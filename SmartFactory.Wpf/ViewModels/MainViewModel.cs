using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartFactory.Application.Interfaces;
using SmartFactory.Application.Models;
using SmartFactory.Domain.Enums;

// 主 Shell 的职责是组织导航和当前用户展示，不承载具体业务查询。
namespace SmartFactory.Wpf.ViewModels;

/// <summary>主窗口 Shell ViewModel。</summary>
public sealed partial class MainViewModel : ViewModelBase
{
    private readonly INavigationService _navigation;
    private readonly ICurrentUserService _currentUser;
    // ObservableCollection 会把菜单项增删通知给 ItemsControl。
    public ObservableCollection<NavigationItem> NavigationItems { get; }=[];
    [ObservableProperty] private object? _currentViewModel;
    [ObservableProperty] private NavigationItem? _selectedNavigation;
    public string DisplayName=>_currentUser.CurrentUser?.DisplayName??"访客";
    public string RoleName=>_currentUser.CurrentUser?.RoleName??"Unknown";
    public string ConnectionText=>"本地数据源 · 实时服务在线";
    public event EventHandler? LogoutRequested;

    public MainViewModel(INavigationService navigation,ICurrentUserService currentUser)
    {
        // MainViewModel 通过构造函数注入依赖，避免在业务代码中使用 Service Locator。
        _navigation=navigation;_currentUser=currentUser;_navigation.CurrentViewModelChanged+=OnCurrentChanged;
        Add("生产总览","◈",typeof(DashboardViewModel),Permission.DashboardRead);Add("设备监控","▦",typeof(DeviceListViewModel),Permission.DeviceRead);
        Add("报警中心","△",typeof(AlarmViewModel),Permission.AlarmRead);Add("运维工单","◇",typeof(WorkOrderViewModel),Permission.WorkOrderRead);
        Add("历史数据","⌁",typeof(HistoryViewModel),Permission.HistoryRead);Add("系统设置","⚙",typeof(SettingsViewModel),Permission.SystemSetting);
        SelectedNavigation=NavigationItems.FirstOrDefault();
    }
    // 根据当前用户权限生成菜单；隐藏菜单只是体验优化，Service 仍会再次鉴权。
    private void Add(string label,string icon,Type vm,Permission permission){if(_currentUser.HasPermission(permission))NavigationItems.Add(new(label,icon,vm,permission));}
    private void OnCurrentChanged(object? sender,EventArgs e)=>CurrentViewModel=_navigation.CurrentViewModel;
    partial void OnSelectedNavigationChanged(NavigationItem? value){if(value is null)return;foreach(var item in NavigationItems)item.IsSelected=item==value;Navigate(value);}
    [RelayCommand] private void Navigate(NavigationItem item)
    {
        // 接口使用泛型 NavigateTo<T>，这里仅把运行时菜单类型转成反射调用。
        var method=typeof(INavigationService).GetMethod(nameof(INavigationService.NavigateTo))!.MakeGenericMethod(item.ViewModelType);method.Invoke(_navigation,null);
    }
    [RelayCommand] private void Logout(){_currentUser.Clear();LogoutRequested?.Invoke(this,EventArgs.Empty);}
}
