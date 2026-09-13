using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartFactory.Application.Interfaces;

// 设置页只修改 ISettingsService 和实时服务状态，不直接写文件或配置系统。
namespace SmartFactory.Wpf.ViewModels;

/// <summary>实时刷新、模拟器和报警阈值设置。</summary>
public sealed partial class SettingsViewModel : ViewModelBase, INavigationAware
{
    private readonly ISettingsService _settings;private readonly IDeviceRealtimeService _realtime;private readonly IDialogService _dialogs;
    [ObservableProperty] private int _uiRefreshIntervalMs;[ObservableProperty] private bool _simulationEnabled;[ObservableProperty] private double _warningTemperature;[ObservableProperty] private bool _darkTheme;
    public SettingsViewModel(ISettingsService settings,IDeviceRealtimeService realtime,IDialogService dialogs){_settings=settings;_realtime=realtime;_dialogs=dialogs;_uiRefreshIntervalMs=settings.UiRefreshIntervalMs;_simulationEnabled=settings.SimulationEnabled;_warningTemperature=settings.WarningTemperature;}
    [RelayCommand] private async Task SaveAsync(CancellationToken ct){_settings.UiRefreshIntervalMs=Math.Clamp(UiRefreshIntervalMs,100,1000);_settings.SimulationEnabled=SimulationEnabled;_settings.WarningTemperature=Math.Clamp(WarningTemperature,60,110);_realtime.IsEnabled=SimulationEnabled;await _settings.SaveAsync(ct);await _dialogs.ShowMessageAsync("设置已保存","运行参数已写入本地配置。部分参数将在下次订阅时生效。");}
    public Task OnNavigatedToAsync(CancellationToken ct)=>Task.CompletedTask;public Task OnNavigatedFromAsync()=>Task.CompletedTask;
}
