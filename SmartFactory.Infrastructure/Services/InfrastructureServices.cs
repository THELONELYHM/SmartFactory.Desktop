using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartFactory.Application.Exceptions;
using SmartFactory.Application.Interfaces;
using SmartFactory.Application.Models;
using SmartFactory.Application.Options;
using SmartFactory.Application.Services;
using SmartFactory.Domain.Entities;
using SmartFactory.Domain.Enums;
using SmartFactory.Infrastructure.Persistence;

// 这些服务把应用接口连接到 SQLite、本地文件和当前用户上下文。
namespace SmartFactory.Infrastructure.Services;

/// <summary>基于本地 Users 表的离线认证服务。</summary>
public sealed class AuthenticationService(IDbContextFactory<AppDbContext> factory, ICurrentUserService currentUser, ILogger<AuthenticationService> logger) : IAuthenticationService
{
    public async Task<User> LoginAsync(string username, string password, CancellationToken ct)
    {
        // 模拟网络延迟，让登录按钮的 IsBusy/禁用状态在演示中可观察。
        await Task.Delay(350, ct); await using var db = await factory.CreateDbContextAsync(ct);
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Username == username, ct);
        if (user is null || !user.IsEnabled || user.PasswordHash != PasswordHasher.Hash(password)) { logger.LogWarning("Login failed for user {User}", username); throw new AuthenticationException("用户名或密码不正确。"); }
        currentUser.SetUser(user); logger.LogInformation("Login success for user {User}", username); return user;
    }
}

/// <summary>历史遥测的分页查询服务。</summary>
public sealed class HistoryService(IDbContextFactory<AppDbContext> factory) : IHistoryService
{
    public async Task<PagedResult<TelemetryRecord>> GetTelemetryAsync(int? deviceId, DateTime from, DateTime to, PagedRequest request, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct); var query = db.TelemetryRecords.AsNoTracking().Where(x => x.Timestamp >= from && x.Timestamp <= to);
        if (deviceId is not null) query = query.Where(x => x.DeviceId == deviceId);
        // 只取当前页记录；Count 用于分页器展示总条数。
        var total = await query.CountAsync(ct); var items = await query.OrderByDescending(x => x.Timestamp).Skip((request.PageIndex-1)*request.PageSize).Take(request.PageSize).ToListAsync(ct);
        return new(items,total,request.PageIndex,request.PageSize);
    }
}

/// <summary>聚合首页设备、报警和工单统计。</summary>
public sealed class DashboardService(IDbContextFactory<AppDbContext> factory, IAlarmRepository alarmRepository) : IDashboardService
{
    public async Task<DashboardSummary> GetSummaryAsync(CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct); var today = DateTime.Today;
        return new(await db.Devices.CountAsync(ct), await db.Devices.CountAsync(x=>x.Status==DeviceStatus.Online,ct), await db.Devices.CountAsync(x=>x.Status==DeviceStatus.Offline,ct),
            await db.Devices.CountAsync(x=>x.Status==DeviceStatus.Fault,ct), await db.Alarms.CountAsync(x=>x.OccurredAt>=today,ct), await db.Alarms.CountAsync(x=>x.Level==AlarmLevel.Critical&&!x.IsAcknowledged,ct),
            await db.WorkOrders.CountAsync(x=>x.Status==WorkOrderStatus.Pending,ct), await alarmRepository.GetRecentAsync(6,ct));
    }
}

/// <summary>工单保存、权限校验和状态流转服务。</summary>
public sealed class WorkOrderService(IWorkOrderRepository repository, ICurrentUserService currentUser, ILogger<WorkOrderService> logger) : IWorkOrderService
{
    public Task<PagedResult<WorkOrder>> GetAsync(PagedRequest request, WorkOrderStatus? status, CancellationToken ct)=>repository.GetPagedAsync(request,status,ct);
    public async Task SaveAsync(WorkOrder order,CancellationToken ct)
    {
        // 权限和业务校验放在 Service 层，即使绕过 WPF 按钮也不能直接提交非法工单。
        if (!currentUser.HasPermission(Permission.WorkOrderCreate)) throw new AuthorizationException("当前角色没有编辑工单权限。");
        if (string.IsNullOrWhiteSpace(order.DeviceCode) || order.Description.Trim().Length<5) throw new BusinessException("请选择设备，且描述至少 5 个字符。");
        // Id 为 0 表示新增，否则更新已有记录；编辑时保留原工单号和创建信息。
        if(order.Id==0){order.OrderNo=$"WO-{DateTime.Now:yyyyMMdd-HHmmss}";order.CreatedAt=DateTime.Now;order.CreatedBy=currentUser.CurrentUser?.Username??"system";await repository.AddAsync(order,ct);}else await repository.UpdateAsync(order,ct);
        logger.LogInformation("Work order {OrderNo} saved",order.OrderNo);
    }
    public async Task TransitionAsync(WorkOrder order,WorkOrderStatus status,CancellationToken ct){order.Status=status;order.CompletedAt=status==WorkOrderStatus.Completed?DateTime.Now:null;await repository.UpdateAsync(order,ct);}
    public Task DeleteAsync(long id,CancellationToken ct)=>repository.DeleteAsync(id,ct);
}

/// <summary>把用户可调参数写入本地应用数据目录。</summary>
public sealed class SettingsService(IOptions<RealtimeOptions> realtime, IOptions<AlarmOptions> alarm) : ISettingsService
{
    public int UiRefreshIntervalMs { get; set; }=realtime.Value.UiRefreshIntervalMs;
    public bool SimulationEnabled { get; set; }=realtime.Value.UseSimulation;
    public double WarningTemperature { get; set; }=alarm.Value.WarningTemperature;
    public async Task SaveAsync(CancellationToken ct)
    {
        var folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"SmartFactory");Directory.CreateDirectory(folder);
        await File.WriteAllTextAsync(Path.Combine(folder,"user-settings.json"),JsonSerializer.Serialize(new{UiRefreshIntervalMs,SimulationEnabled,WarningTemperature},new JsonSerializerOptions{WriteIndented=true}),ct);
    }
}
