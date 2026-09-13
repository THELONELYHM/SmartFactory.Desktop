using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartFactory.Application.Interfaces;
using SmartFactory.Application.Models;
using SmartFactory.Domain.Entities;

// 历史页面始终通过分页查询，避免一次性把大量遥测记录加载到内存。
namespace SmartFactory.Wpf.ViewModels;

/// <summary>按设备和时间范围查询历史遥测。</summary>
public sealed partial class HistoryViewModel(IHistoryService history,IDeviceService devices) : ViewModelBase, INavigationAware
{
    public ObservableCollection<Device> Devices{get;}=[];public ObservableCollection<TelemetryRecord> Records{get;}=[];
    [ObservableProperty] private Device? _selectedDevice;[ObservableProperty] private DateTime _from=DateTime.Today.AddDays(-1);[ObservableProperty] private DateTime _to=DateTime.Now;
    [ObservableProperty] private int _pageIndex=1;[ObservableProperty] private int _totalPages=1;[ObservableProperty] private int _totalCount;
    // 日期和设备条件交给应用层组合 IQueryable，界面只展示当前页结果。
    [RelayCommand] public async Task QueryAsync(CancellationToken ct=default){if(IsBusy)return;IsBusy=true;try{var page=await history.GetTelemetryAsync(SelectedDevice?.Id,From,To,new(PageIndex,50),ct);Records.Clear();foreach(var item in page.Items)Records.Add(item);TotalPages=page.TotalPages;TotalCount=page.TotalCount;}catch(OperationCanceledException){}catch(Exception ex){ErrorMessage=ex.Message;}finally{IsBusy=false;}}
    [RelayCommand] private async Task PreviousAsync(CancellationToken ct){if(PageIndex>1){PageIndex--;await QueryAsync(ct);}}
    [RelayCommand] private async Task NextAsync(CancellationToken ct){if(PageIndex<TotalPages){PageIndex++;await QueryAsync(ct);}}
    public async Task OnNavigatedToAsync(CancellationToken ct){if(Devices.Count==0){foreach(var item in await devices.GetDevicesAsync(ct))Devices.Add(item);SelectedDevice=Devices.FirstOrDefault();}await QueryAsync(ct);}public Task OnNavigatedFromAsync()=>Task.CompletedTask;
}
