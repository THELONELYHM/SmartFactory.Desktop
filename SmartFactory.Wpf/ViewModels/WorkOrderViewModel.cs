using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartFactory.Application.Interfaces;
using SmartFactory.Application.Models;
using SmartFactory.Domain.Entities;
using SmartFactory.Domain.Enums;

// 工单列表与编辑器放在同一页面，验证由 ObservableValidator 提供字段级错误。
namespace SmartFactory.Wpf.ViewModels;

/// <summary>工单编辑字段和 DataAnnotations 校验。</summary>
public sealed partial class WorkOrderEditViewModel : ObservableValidator
{
    private WorkOrder? _source;
    [ObservableProperty] [Required(ErrorMessage="设备编号不能为空")] [NotifyDataErrorInfo] private string _deviceCode=string.Empty;
    [ObservableProperty] [Required(ErrorMessage="描述不能为空")] [MinLength(5,ErrorMessage="描述至少需要 5 个字符")] [NotifyDataErrorInfo] private string _description=string.Empty;
    [ObservableProperty] private WorkOrderPriority _priority=WorkOrderPriority.Normal;
    [ObservableProperty] private string? _assignee;
    public long Id{get;set;}
    public Array Priorities=>Enum.GetValues<WorkOrderPriority>();
    public bool Validate(){ValidateAllProperties();return !HasErrors;}
    // 编辑已有工单时保留原始 OrderNo、状态和审计字段，避免更新覆盖数据库元数据。
    public WorkOrder ToEntity()=>new(){Id=Id,OrderNo=_source?.OrderNo??string.Empty,DeviceCode=DeviceCode,DeviceId=_source?.DeviceId??0,Description=Description,Priority=Priority,Assignee=Assignee,Status=_source?.Status??WorkOrderStatus.Pending,CreatedAt=_source?.CreatedAt??default,CreatedBy=_source?.CreatedBy??string.Empty,CompletedAt=_source?.CompletedAt};
    public void Load(WorkOrder? value){_source=value;Id=value?.Id??0;DeviceCode=value?.DeviceCode??string.Empty;Description=value?.Description??string.Empty;Priority=value?.Priority??WorkOrderPriority.Normal;Assignee=value?.Assignee;ClearErrors();}
}

/// <summary>工单分页、保存和 Pending/Processing/Completed 状态流转。</summary>
public sealed partial class WorkOrderViewModel(IWorkOrderService service,IDialogService dialogs,ICurrentUserService user) : ViewModelBase, INavigationAware
{
    public ObservableCollection<WorkOrder> WorkOrders{get;}=[];public WorkOrderEditViewModel Editor{get;}=new();public Array StatusOptions=>Enum.GetValues<WorkOrderStatus>();
    [ObservableProperty] private WorkOrder? _selectedOrder;[ObservableProperty] private WorkOrderStatus? _selectedStatus;[ObservableProperty] private string _searchText=string.Empty;
    [ObservableProperty] private int _pageIndex=1;[ObservableProperty] private int _totalPages=1;
    public bool CanEdit=>user.HasPermission(Permission.WorkOrderCreate);
    partial void OnSelectedOrderChanged(WorkOrder? value)=>Editor.Load(value);
    // 刷新列表会重新请求当前页，并由 ObservableCollection 通知 DataGrid 更新。
    [RelayCommand] public async Task RefreshAsync(CancellationToken ct=default){if(IsBusy)return;IsBusy=true;try{var page=await service.GetAsync(new(PageIndex,15,SearchText),SelectedStatus,ct);WorkOrders.Clear();foreach(var item in page.Items)WorkOrders.Add(item);TotalPages=page.TotalPages;}catch(OperationCanceledException){}catch(Exception ex){ErrorMessage=ex.Message;}finally{IsBusy=false;}}
    [RelayCommand(CanExecute=nameof(CanEdit))] private void New(){SelectedOrder=null;Editor.Load(null);}
    [RelayCommand(CanExecute=nameof(CanEdit))] private async Task SaveAsync(CancellationToken ct){if(!Editor.Validate())return;await service.SaveAsync(Editor.ToEntity(),ct);await RefreshAsync(ct);await dialogs.ShowMessageAsync("工单已保存","运维工单已进入待处理队列。");}
    [RelayCommand(CanExecute=nameof(CanEdit))] private async Task StartAsync(CancellationToken ct){if(SelectedOrder is null)return;await service.TransitionAsync(SelectedOrder,WorkOrderStatus.Processing,ct);await RefreshAsync(ct);}
    [RelayCommand(CanExecute=nameof(CanEdit))] private async Task CompleteAsync(CancellationToken ct){if(SelectedOrder is null)return;await service.TransitionAsync(SelectedOrder,WorkOrderStatus.Completed,ct);await RefreshAsync(ct);}
    [RelayCommand(CanExecute=nameof(CanEdit))] private async Task DeleteAsync(CancellationToken ct){if(SelectedOrder is null||!await dialogs.ConfirmAsync("删除工单","确认删除所选工单？"))return;await service.DeleteAsync(SelectedOrder.Id,ct);await RefreshAsync(ct);}
    [RelayCommand] private async Task PreviousAsync(CancellationToken ct){if(PageIndex>1){PageIndex--;await RefreshAsync(ct);}}
    [RelayCommand] private async Task NextAsync(CancellationToken ct){if(PageIndex<TotalPages){PageIndex++;await RefreshAsync(ct);}}
    public Task OnNavigatedToAsync(CancellationToken ct)=>RefreshAsync(ct);public Task OnNavigatedFromAsync()=>Task.CompletedTask;
}
