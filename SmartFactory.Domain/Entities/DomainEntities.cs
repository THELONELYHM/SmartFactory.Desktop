using SmartFactory.Domain.Enums;

// 领域实体只保存业务数据，不包含 WPF、EF Core 或网络逻辑。
namespace SmartFactory.Domain.Entities;

/// <summary>生产设备档案及最新运行快照。</summary>
public sealed class Device
{
    // Id 是 SQLite 自增主键；DeviceCode 是面向现场人员的业务编号。
    public int Id { get; set; }
    public string DeviceCode { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    // Status 与遥测值分开保存：状态是业务判断结果，遥测是原始测量快照。
    public DeviceStatus Status { get; set; }
    public double Temperature { get; set; }
    public double Pressure { get; set; }
    public double Speed { get; set; }
    // LastUpdateTime 让界面可以判断数据是否新鲜。
    public DateTime LastUpdateTime { get; set; }
    public string Location { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
}

/// <summary>设备报警记录及确认审计信息。</summary>
public sealed class Alarm
{
    // 报警使用 long 主键，便于长期运行时保存大量历史记录。
    public long Id { get; set; }
    public int DeviceId { get; set; }
    public string DeviceCode { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public AlarmLevel Level { get; set; }
    public DateTime OccurredAt { get; set; }
    // 确认字段保留处理人和时间，形成最小审计链路。
    public bool IsAcknowledged { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public string? AcknowledgedBy { get; set; }
}

/// <summary>设备运维工单。</summary>
public sealed class WorkOrder
{
    // OrderNo 是给运维人员看的工单号，数据库对它建立唯一索引。
    public long Id { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public int DeviceId { get; set; }
    public string DeviceCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    // Status 表示 Pending → Processing → Completed 的业务生命周期。
    public WorkOrderStatus Status { get; set; }
    public WorkOrderPriority Priority { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Assignee { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}

/// <summary>登录用户及角色信息。</summary>
public sealed class User
{
    // PasswordHash 只保存摘要，绝不在日志或数据库中保存明文密码。
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
}

/// <summary>角色及其权限序列化信息。</summary>
public sealed class Role
{
    // 为保持模型简单，演示权限以逗号分隔字符串持久化，运行时展开为 HashSet。
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PermissionsCsv { get; set; } = string.Empty;
}

/// <summary>写入本地数据库的历史遥测采样。</summary>
public sealed class TelemetryRecord
{
    public long Id { get; set; }
    public int DeviceId { get; set; }
    public DateTime Timestamp { get; set; }
    public double Temperature { get; set; }
    public double Pressure { get; set; }
    public double Speed { get; set; }
}
