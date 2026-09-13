// 所有页面共享的状态基类，统一承载忙碌和错误提示。
using CommunityToolkit.Mvvm.ComponentModel;

namespace SmartFactory.Wpf.ViewModels;

/// <summary>CommunityToolkit 可观察对象的项目基类。</summary>
public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _errorMessage;
}
