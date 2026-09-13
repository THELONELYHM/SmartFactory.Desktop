// 领域枚举集中放置，避免在业务代码和界面中散落字符串常量。
namespace SmartFactory.Domain.Enums;

/// <summary>设备在产线中的运行状态。</summary>
public enum DeviceStatus { Offline, Online, Warning, Fault, Maintenance }
/// <summary>报警严重级别。</summary>
public enum AlarmLevel { Info, Warning, Critical }
/// <summary>工单生命周期状态。</summary>
public enum WorkOrderStatus { Pending, Processing, Completed, Cancelled }
/// <summary>工单优先级。</summary>
public enum WorkOrderPriority { Low, Normal, High, Emergency }
/// <summary>系统 RBAC 权限清单。</summary>
public enum Permission
{
    DashboardRead, DeviceRead, DeviceEdit, DeviceControl, AlarmRead, AlarmAcknowledge,
    WorkOrderRead, WorkOrderCreate, WorkOrderEdit, HistoryRead, SystemSetting, UserManagement
}
/// <summary>实时数据源连接状态，为未来接入网络协议预留。</summary>
public enum ConnectionState { Disconnected, Connecting, Connected, Reconnecting, Faulted }
