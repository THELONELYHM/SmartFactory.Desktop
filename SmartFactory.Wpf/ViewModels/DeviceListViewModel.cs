using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartFactory.Application.Interfaces;
using SmartFactory.Application.Models;
using SmartFactory.Application.Options;
using SmartFactory.Application.Services;
using SmartFactory.Domain.Entities;
using SmartFactory.Domain.Enums;

// 设备页面同时承担列表筛选和实时管线生命周期管理，离开页面时会取消订阅。
namespace SmartFactory.Wpf.ViewModels;

public sealed partial class DeviceItemViewModel : ObservableObject
{
    // 列表项单独实现通知：ObservableCollection 只能通知“行增删”，不能通知行内温度变化。
    public int Id{get;} [ObservableProperty] private string _deviceCode;[ObservableProperty] private string _deviceName;[ObservableProperty] private string _deviceType;
    [ObservableProperty] private DeviceStatus _status;[ObservableProperty] private double _temperature;[ObservableProperty] private double _pressure;[ObservableProperty] private double _speed;
    [ObservableProperty] private DateTime _lastUpdateTime;[ObservableProperty] private string _location;public bool IsEnabled{get;set;}
    // 构造时复制领域实体，避免把 EF 跟踪实体直接暴露给界面。
    public DeviceItemViewModel(Device d){Id=d.Id;_deviceCode=d.DeviceCode;_deviceName=d.DeviceName;_deviceType=d.DeviceType;_status=d.Status;_temperature=d.Temperature;_pressure=d.Pressure;_speed=d.Speed;_lastUpdateTime=d.LastUpdateTime;_location=d.Location;IsEnabled=d.IsEnabled;}
    // 保存前把用户在详情面板编辑的值转换回应用层实体。
    public Device ToEntity()=>new(){Id=Id,DeviceCode=DeviceCode,DeviceName=DeviceName,DeviceType=DeviceType,Status=Status,Temperature=Temperature,Pressure=Pressure,Speed=Speed,LastUpdateTime=LastUpdateTime,Location=Location,IsEnabled=IsEnabled};
}

/// <summary>设备档案、筛选、CRUD 和批量实时刷新页面。</summary>
public sealed partial class DeviceListViewModel : ViewModelBase, INavigationAware
{
    private readonly IDeviceService _service;private readonly IDeviceRealtimeService _realtime;private readonly IAlarmService _alarms;private readonly IUiDispatcher _dispatcher;private readonly IDialogService _dialogs;private readonly ICurrentUserService _user;private readonly TelemetryAggregator _aggregator;private readonly RealtimeOptions _options;private readonly ILogger<DeviceListViewModel> _logger;
    private CancellationTokenSource? _realtimeCts;private Task? _collector;private Task? _pump;
    public ObservableCollection<DeviceItemViewModel> Devices{get;}=[];public ICollectionView DevicesView{get;}
    public Array StatusOptions=>Enum.GetValues<DeviceStatus>();
    [ObservableProperty] private string _searchText=string.Empty;[ObservableProperty] private DeviceStatus? _selectedStatus;[ObservableProperty] private DeviceItemViewModel? _selectedDevice;
    // CanExecute 只影响界面按钮是否可点，Service 层仍会再次校验权限。
    public bool CanEdit=>_user.HasPermission(Permission.DeviceEdit);
    public DeviceListViewModel(IDeviceService service,IDeviceRealtimeService realtime,IAlarmService alarms,IUiDispatcher dispatcher,IDialogService dialogs,ICurrentUserService user,TelemetryAggregator aggregator,IOptions<RealtimeOptions> options,ILogger<DeviceListViewModel> logger)
    {_service=service;_realtime=realtime;_alarms=alarms;_dispatcher=dispatcher;_dialogs=dialogs;_user=user;_aggregator=aggregator;_options=options.Value;_logger=logger;DevicesView=CollectionViewSource.GetDefaultView(Devices);DevicesView.Filter=Filter;}
    // 过滤在原 ICollectionView 上刷新，不创建新的 ObservableCollection。
    partial void OnSearchTextChanged(string value)=>DevicesView.Refresh();partial void OnSelectedStatusChanged(DeviceStatus? value)=>DevicesView.Refresh();
    private bool Filter(object item)=>item is DeviceItemViewModel d&&(string.IsNullOrWhiteSpace(SearchText)||d.DeviceCode.Contains(SearchText,StringComparison.OrdinalIgnoreCase)||d.DeviceName.Contains(SearchText,StringComparison.OrdinalIgnoreCase))&&(SelectedStatus is null||d.Status==SelectedStatus);
    // 页面刷新使用 IsBusy 防止用户连续点击，取消异常不显示错误弹窗。
    [RelayCommand] public async Task RefreshAsync(CancellationToken ct=default){if(IsBusy)return;IsBusy=true;try{var items=await _service.GetDevicesAsync(ct);Devices.Clear();foreach(var item in items)Devices.Add(new(item));}catch(OperationCanceledException){}catch(Exception ex){ErrorMessage=ex.Message;}finally{IsBusy=false;}}
    [RelayCommand(CanExecute=nameof(CanEdit))] private async Task AddAsync(CancellationToken ct){var index=Devices.Count+1;var device=new Device{DeviceCode=$"EQ-{100+index}",DeviceName="新建设备",DeviceType="待配置",Status=DeviceStatus.Offline,Location="未分配",LastUpdateTime=DateTime.Now};await _service.CreateDeviceAsync(device,ct);Devices.Add(new(device));SelectedDevice=Devices[^1];}
    [RelayCommand(CanExecute=nameof(CanEdit))] private async Task SaveSelectedAsync(CancellationToken ct){if(SelectedDevice is null)return;await _service.UpdateDeviceAsync(SelectedDevice.ToEntity(),ct);await _dialogs.ShowMessageAsync("保存成功","设备档案已更新。");}
    [RelayCommand(CanExecute=nameof(CanEdit))] private async Task DeleteAsync(CancellationToken ct){if(SelectedDevice is null)return;if(!await _dialogs.ConfirmAsync("删除设备",$"确认删除 {SelectedDevice.DeviceName}？"))return;await _service.DeleteDeviceAsync(SelectedDevice.Id,ct);Devices.Remove(SelectedDevice);}
    // 进入页面时启动两个后台任务：采集任务写入聚合器，泵任务按固定周期刷新界面。
    public async Task OnNavigatedToAsync(CancellationToken ct){await RefreshAsync(ct);if(_realtimeCts is not null)return;_realtimeCts=new();_collector=CollectAsync(_realtimeCts.Token);_pump=PumpAsync(_realtimeCts.Token);}
    // 离开页面必须先 Cancel，再等待两个任务结束，避免页面销毁后仍访问控件。
    public async Task OnNavigatedFromAsync(){if(_realtimeCts is null)return;_realtimeCts.Cancel();try{await Task.WhenAll(_collector??Task.CompletedTask,_pump??Task.CompletedTask);}catch(OperationCanceledException){}finally{_realtimeCts.Dispose();_realtimeCts=null;}}
    private async Task CollectAsync(CancellationToken ct)
    {
        try{await foreach(var value in _realtime.SubscribeAsync(ct))_aggregator.Add(value);}catch(OperationCanceledException){}catch(Exception ex){_logger.LogError(ex,"Realtime collection failed");}
    }
    private async Task PumpAsync(CancellationToken ct)
    {
        // PeriodicTimer 只负责节拍，不创建额外线程；每次 tick 批量拿走最新快照。
        using var timer=new PeriodicTimer(TimeSpan.FromMilliseconds(Math.Clamp(_options.UiRefreshIntervalMs,100,1000)));
        try{while(await timer.WaitForNextTickAsync(ct)){var batch=_aggregator.Drain();if(batch.Count==0)continue;var alarmCandidates=new List<(Device Device,DeviceTelemetry Telemetry)>();await _dispatcher.InvokeAsync(()=>{foreach(var value in batch){var item=Devices.FirstOrDefault(x=>x.Id==value.DeviceId);if(item is null)continue;item.Temperature=value.Temperature;item.Pressure=value.Pressure;item.Speed=value.Speed;item.LastUpdateTime=value.Timestamp;item.Status=value.Temperature>95||value.Pressure>1.6?DeviceStatus.Fault:value.Temperature>85?DeviceStatus.Warning:DeviceStatus.Online;alarmCandidates.Add((item.ToEntity(),value));}});foreach(var candidate in alarmCandidates)await _alarms.ProcessTelemetryAsync(candidate.Device,candidate.Telemetry,ct);}}
        catch(OperationCanceledException){}
    }
}
