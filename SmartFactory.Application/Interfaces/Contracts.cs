using SmartFactory.Application.Models;
using SmartFactory.Domain.Entities;
using SmartFactory.Domain.Enums;

// 跨层协作接口集中定义于此，便于替换实现和编写 Fake 单元测试。
namespace SmartFactory.Application.Interfaces;

/// <summary>设备持久化抽象。</summary>
public interface IDeviceRepository
{
    Task<IReadOnlyList<Device>> GetAllAsync(CancellationToken cancellationToken);
    Task<Device?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task AddAsync(Device device, CancellationToken cancellationToken);
    Task UpdateAsync(Device device, CancellationToken cancellationToken);
    Task DeleteAsync(int id, CancellationToken cancellationToken);
}
/// <summary>报警持久化、分页和确认抽象。</summary>
public interface IAlarmRepository
{
    Task<PagedResult<Alarm>> GetPagedAsync(PagedRequest request, AlarmLevel? level, CancellationToken cancellationToken);
    Task AddAsync(Alarm alarm, CancellationToken cancellationToken);
    Task AcknowledgeAsync(long id, string username, CancellationToken cancellationToken);
    Task<IReadOnlyList<Alarm>> GetRecentAsync(int count, CancellationToken cancellationToken);
}
/// <summary>工单持久化抽象。</summary>
public interface IWorkOrderRepository
{
    Task<PagedResult<WorkOrder>> GetPagedAsync(PagedRequest request, WorkOrderStatus? status, CancellationToken cancellationToken);
    Task AddAsync(WorkOrder order, CancellationToken cancellationToken);
    Task UpdateAsync(WorkOrder order, CancellationToken cancellationToken);
    Task DeleteAsync(long id, CancellationToken cancellationToken);
}
/// <summary>设备业务用例。</summary>
public interface IDeviceService
{
    Task<IReadOnlyList<Device>> GetDevicesAsync(CancellationToken cancellationToken);
    Task<Device?> GetDeviceAsync(int id, CancellationToken cancellationToken);
    Task<Device> CreateDeviceAsync(Device device, CancellationToken cancellationToken);
    Task UpdateDeviceAsync(Device device, CancellationToken cancellationToken);
    Task DeleteDeviceAsync(int id, CancellationToken cancellationToken);
}
/// <summary>报警查询、确认和阈值处理用例。</summary>
public interface IAlarmService
{
    Task<PagedResult<Alarm>> GetAlarmsAsync(PagedRequest request, AlarmLevel? level, CancellationToken cancellationToken);
    Task AcknowledgeAsync(long id, CancellationToken cancellationToken);
    Task ProcessTelemetryAsync(Device device, DeviceTelemetry telemetry, CancellationToken cancellationToken);
}
/// <summary>工单查询、保存和状态流转用例。</summary>
public interface IWorkOrderService
{
    Task<PagedResult<WorkOrder>> GetAsync(PagedRequest request, WorkOrderStatus? status, CancellationToken cancellationToken);
    Task SaveAsync(WorkOrder order, CancellationToken cancellationToken);
    Task TransitionAsync(WorkOrder order, WorkOrderStatus status, CancellationToken cancellationToken);
    Task DeleteAsync(long id, CancellationToken cancellationToken);
}
/// <summary>认证服务，避免登录界面硬编码账号判断。</summary>
public interface IAuthenticationService { Task<User> LoginAsync(string username, string password, CancellationToken cancellationToken); }
/// <summary>当前用户上下文和 RBAC 权限判断。</summary>
public interface ICurrentUserService
{
    User? CurrentUser { get; }
    void SetUser(User user);
    void Clear();
    bool HasPermission(Permission permission);
}
/// <summary>历史遥测分页查询。</summary>
public interface IHistoryService
{
    Task<PagedResult<TelemetryRecord>> GetTelemetryAsync(int? deviceId, DateTime from, DateTime to, PagedRequest request, CancellationToken cancellationToken);
}
/// <summary>首页统计查询。</summary>
public interface IDashboardService { Task<DashboardSummary> GetSummaryAsync(CancellationToken cancellationToken); }
/// <summary>实时数据订阅抽象，可替换为 MQTT、SignalR 或 OPC UA。</summary>
public interface IDeviceRealtimeService
{
    ConnectionState State { get; }
    bool IsEnabled { get; set; }
    IAsyncEnumerable<DeviceTelemetry> SubscribeAsync(CancellationToken cancellationToken);
}
/// <summary>UI 线程调度抽象，测试时可以使用同步实现。</summary>
public interface IUiDispatcher { Task InvokeAsync(Action action); }
/// <summary>对话框抽象，隔离 WPF MessageBox。</summary>
public interface IDialogService
{
    Task ShowMessageAsync(string title, string message);
    Task<bool> ConfirmAsync(string title, string message);
}
/// <summary>单窗口内容区域的 ViewModel 导航抽象。</summary>
public interface INavigationService
{
    object? CurrentViewModel { get; }
    event EventHandler? CurrentViewModelChanged;
    void NavigateTo<TViewModel>() where TViewModel : class;
}
/// <summary>页面进入和离开生命周期通知。</summary>
public interface INavigationAware
{
    Task OnNavigatedToAsync(CancellationToken cancellationToken);
    Task OnNavigatedFromAsync();
}
/// <summary>数据库首次启动初始化。</summary>
public interface IDatabaseInitializer { Task InitializeAsync(CancellationToken cancellationToken); }
/// <summary>本地运行参数读写抽象。</summary>
public interface ISettingsService { int UiRefreshIntervalMs { get; set; } bool SimulationEnabled { get; set; } double WarningTemperature { get; set; } Task SaveAsync(CancellationToken cancellationToken); }
