using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartFactory.Application.Exceptions;
using SmartFactory.Application.Interfaces;
using SmartFactory.Application.Models;
using SmartFactory.Application.Options;
using SmartFactory.Domain.Entities;
using SmartFactory.Domain.Enums;

// 应用服务编排业务规则；它们不直接创建数据库、窗口或 HttpClient。
namespace SmartFactory.Application.Services;

/// <summary>设备档案 CRUD 服务。</summary>
public sealed class DeviceService(IDeviceRepository repository, ILogger<DeviceService> logger) : IDeviceService
{
    // 查询由 Repository 完成；服务层保持用例入口，便于以后加入权限、审计或缓存。
    public Task<IReadOnlyList<Device>> GetDevicesAsync(CancellationToken ct) => repository.GetAllAsync(ct);
    // 把 CancellationToken 一直向下传递，用户离开页面时数据库查询可以及时停止。
    public Task<Device?> GetDeviceAsync(int id, CancellationToken ct) => repository.GetByIdAsync(id, ct);
    public async Task<Device> CreateDeviceAsync(Device device, CancellationToken ct)
    {
        // 这是应用级校验；数据库唯一索引仍会作为最后一道约束。
        if (string.IsNullOrWhiteSpace(device.DeviceCode) || string.IsNullOrWhiteSpace(device.DeviceName)) throw new BusinessException("设备编号和名称不能为空。");
        await repository.AddAsync(device, ct); logger.LogInformation("Device {Code} created", device.DeviceCode); return device;
    }
    public async Task UpdateDeviceAsync(Device device, CancellationToken ct) { await repository.UpdateAsync(device, ct); logger.LogInformation("Device {Code} updated", device.DeviceCode); }
    public async Task DeleteDeviceAsync(int id, CancellationToken ct) { await repository.DeleteAsync(id, ct); logger.LogInformation("Device {Id} deleted", id); }
}

/// <summary>保存本次登录用户及其展开后的权限集合。</summary>
public sealed class CurrentUserService : ICurrentUserService
{
    // HashSet 适合频繁的 Contains 权限判断，且不会保存重复权限。
    private readonly HashSet<Permission> _permissions = [];
    public User? CurrentUser { get; private set; }
    public void SetUser(User user)
    {
        // 登录成功时一次性展开角色权限，后续按钮判断不需要重复解析字符串。
        CurrentUser = user; _permissions.Clear();
        foreach (var value in RolePermissions.For(user.RoleName)) _permissions.Add(value);
    }
    public void Clear() { CurrentUser = null; _permissions.Clear(); }
    public bool HasPermission(Permission permission) => _permissions.Contains(permission);
}

/// <summary>演示角色到权限的静态映射。</summary>
public static class RolePermissions
{
    // switch 表达式让每个角色的权限集合一目了然；未知角色按最小权限处理。
    public static IReadOnlyCollection<Permission> For(string role) => role switch
    {
        "Administrator" => Enum.GetValues<Permission>(),
        "Engineer" => [Permission.DashboardRead, Permission.DeviceRead, Permission.DeviceEdit, Permission.AlarmRead,
            Permission.AlarmAcknowledge, Permission.WorkOrderRead, Permission.WorkOrderCreate, Permission.WorkOrderEdit, Permission.HistoryRead],
        _ => [Permission.DashboardRead, Permission.DeviceRead, Permission.AlarmRead, Permission.WorkOrderRead, Permission.HistoryRead]
    };
}

/// <summary>离线演示用 SHA-256 摘要；生产系统应采用加盐慢哈希。</summary>
public static class PasswordHasher
{
    public static string Hash(string password) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(password)));
}

/// <summary>报警查询、确认、阈值判断和冷却去重服务。</summary>
public sealed class AlarmService(IAlarmRepository repository, ICurrentUserService currentUser,
    IOptions<AlarmOptions> options, ILogger<AlarmService> logger) : IAlarmService
{
    private readonly ConcurrentDictionary<string, DateTime> _cooldowns = new();
    // 报警列表使用 Repository 分页，避免把所有历史报警一次性加载到 UI。
    public Task<PagedResult<Alarm>> GetAlarmsAsync(PagedRequest request, AlarmLevel? level, CancellationToken ct) => repository.GetPagedAsync(request, level, ct);
    public async Task AcknowledgeAsync(long id, CancellationToken ct)
    {
        if (!currentUser.HasPermission(Permission.AlarmAcknowledge)) throw new AuthorizationException("当前角色没有报警确认权限。");
        var user = currentUser.CurrentUser?.DisplayName ?? "system";
        await repository.AcknowledgeAsync(id, user, ct); logger.LogInformation("Alarm {AlarmId} acknowledged by {User}", id, user);
    }
    public async Task ProcessTelemetryAsync(Device device, DeviceTelemetry telemetry, CancellationToken ct)
    {
        // IOptions 让阈值来自配置，而不是散落在 ViewModel 的 magic number。
        var config = options.Value;
        var (level, kind, message) = AlarmEvaluator.Evaluate(telemetry, config);
        if (level is null) return;
        // 同一设备、同一类型报警共用冷却键，避免 50ms 采样产生大量重复记录。
        var key = $"{device.Id}:{kind}"; var now = DateTime.UtcNow;
        if (_cooldowns.TryGetValue(key, out var last) && now - last < TimeSpan.FromSeconds(config.CooldownSeconds)) return;
        _cooldowns[key] = now;
        await repository.AddAsync(new Alarm { DeviceId = device.Id, DeviceCode = device.DeviceCode, DeviceName = device.DeviceName,
            Level = level.Value, Message = message, OccurredAt = telemetry.Timestamp }, ct);
        logger.LogWarning("Alarm generated for {Device}: {Message}", device.DeviceCode, message);
    }
}

/// <summary>根据实时遥测和配置阈值生成报警判定。</summary>
public static class AlarmEvaluator
{
    // 返回元组同时携带级别、去重类型和用户可读消息；level 为 null 表示正常。
    public static (AlarmLevel? Level, string Kind, string Message) Evaluate(DeviceTelemetry value, AlarmOptions options)
    {
        if (value.Temperature > options.CriticalTemperature) return (AlarmLevel.Critical, "temperature", $"温度严重超限：{value.Temperature:F1}℃");
        if (value.Pressure > options.CriticalPressure) return (AlarmLevel.Critical, "pressure", $"压力严重超限：{value.Pressure:F2} MPa");
        if (value.Temperature > options.WarningTemperature) return (AlarmLevel.Warning, "temperature", $"温度偏高：{value.Temperature:F1}℃");
        return (null, string.Empty, string.Empty);
    }
}

/// <summary>按设备 ID 保留最新采样，解耦采集频率和 UI 刷新频率。</summary>
public sealed class TelemetryAggregator
{
    private readonly ConcurrentDictionary<int, DeviceTelemetry> _latest = new();
    // 字典赋值会覆盖旧值，因此每个设备只保留刷新窗口内最新的一条采样。
    public void Add(DeviceTelemetry value) => _latest[value.DeviceId] = value;
    public IReadOnlyList<DeviceTelemetry> Drain()
    {
        var values = new List<DeviceTelemetry>();
        // TryRemove 让取出和删除成为一个原子动作，避免同一快照被重复绘制。
        foreach (var key in _latest.Keys) if (_latest.TryRemove(key, out var value)) values.Add(value);
        return values;
    }
}
