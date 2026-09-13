using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartFactory.Application.Interfaces;
using SmartFactory.Domain.Entities;

// 首页只消费聚合后的 DashboardSummary，避免直接查询多张表。
namespace SmartFactory.Wpf.ViewModels;

/// <summary>生产总览页面 ViewModel。</summary>
public sealed partial class DashboardViewModel(IDashboardService service) : ViewModelBase, INavigationAware
{
    [ObservableProperty] private int _totalDevices;[ObservableProperty] private int _onlineDevices;[ObservableProperty] private int _offlineDevices;
    [ObservableProperty] private int _faultDevices;[ObservableProperty] private int _todayAlarms;[ObservableProperty] private int _criticalAlarms;[ObservableProperty] private int _pendingWorkOrders;
    public ObservableCollection<Alarm> RecentAlarms { get; }=[];
    [RelayCommand] public async Task RefreshAsync(CancellationToken ct=default)
    {
        if(IsBusy)return;IsBusy=true;ErrorMessage=null;
        try{var x=await service.GetSummaryAsync(ct);TotalDevices=x.TotalDevices;OnlineDevices=x.OnlineDevices;OfflineDevices=x.OfflineDevices;FaultDevices=x.FaultDevices;TodayAlarms=x.TodayAlarms;CriticalAlarms=x.CriticalAlarms;PendingWorkOrders=x.PendingWorkOrders;RecentAlarms.Clear();foreach(var alarm in x.RecentAlarms)RecentAlarms.Add(alarm);}
        catch(OperationCanceledException){}catch(Exception ex){ErrorMessage=ex.Message;}finally{IsBusy=false;}
    }
    public Task OnNavigatedToAsync(CancellationToken ct)=>RefreshAsync(ct);
    public Task OnNavigatedFromAsync()=>Task.CompletedTask;
}
