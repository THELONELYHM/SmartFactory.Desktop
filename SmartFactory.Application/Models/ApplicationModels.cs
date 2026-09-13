using SmartFactory.Domain.Entities;
using SmartFactory.Domain.Enums;

// DTO、分页和实时数据模型位于应用层，避免界面直接依赖基础设施类型。
namespace SmartFactory.Application.Models;

/// <summary>分页查询请求，页码从 1 开始。</summary>
public sealed record PagedRequest(int PageIndex = 1, int PageSize = 50, string? SearchText = null);
/// <summary>通用分页结果。</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int PageIndex, int PageSize)
{
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
}
/// <summary>实时设备采样值。</summary>
public sealed record DeviceTelemetry(int DeviceId, double Temperature, double Pressure, double Speed, DateTime Timestamp);
/// <summary>首页统计指标的聚合结果。</summary>
public sealed record DashboardSummary(int TotalDevices, int OnlineDevices, int OfflineDevices, int FaultDevices,
    int TodayAlarms, int CriticalAlarms, int PendingWorkOrders, IReadOnlyList<Alarm> RecentAlarms);
/// <summary>带权限要求的导航菜单项。</summary>
public sealed record NavigationItem(string Label, string Icon, Type ViewModelType, Permission Permission)
{
    public bool IsSelected { get; set; }
}
