using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using SmartFactory.Application.Interfaces;

// WPF 特有能力集中在 Services，保证 ViewModel 可以脱离窗口进行测试。
namespace SmartFactory.Wpf.Services;

/// <summary>把批量更新切回创建控件的 UI Dispatcher。</summary>
public sealed class WpfUiDispatcher : IUiDispatcher
{
    public Task InvokeAsync(Action action)=>System.Windows.Application.Current.Dispatcher.InvokeAsync(action).Task;
}

/// <summary>MessageBox 的异步接口适配器。</summary>
public sealed class DialogService : IDialogService
{
    public Task ShowMessageAsync(string title,string message){MessageBox.Show(message,title,MessageBoxButton.OK,MessageBoxImage.Information);return Task.CompletedTask;}
    public Task<bool> ConfirmAsync(string title,string message)=>Task.FromResult(MessageBox.Show(message,title,MessageBoxButton.YesNo,MessageBoxImage.Question)==MessageBoxResult.Yes);
}

/// <summary>在单个 ContentControl 中切换页面，并管理页面取消令牌。</summary>
public sealed class NavigationService(IServiceProvider provider) : INavigationService
{
    private CancellationTokenSource? _cts;
    public object? CurrentViewModel{get;private set;}public event EventHandler? CurrentViewModelChanged;
    public async void NavigateTo<TViewModel>() where TViewModel:class
    {
        try
        {
            // 先通知旧页面停止任务，再创建新页面，防止实时循环引用已离开的 View。
            if(CurrentViewModel is INavigationAware old)await old.OnNavigatedFromAsync();_cts?.Cancel();_cts?.Dispose();_cts=new();
            CurrentViewModel=provider.GetRequiredService<TViewModel>();CurrentViewModelChanged?.Invoke(this,EventArgs.Empty);
            if(CurrentViewModel is INavigationAware current)await current.OnNavigatedToAsync(_cts.Token);
        }
        catch(OperationCanceledException){}
    }
}

/// <summary>测试用同步 Dispatcher，避免测试启动真实 WPF 窗口。</summary>
public sealed class ImmediateUiDispatcher : IUiDispatcher{public Task InvokeAsync(Action action){action();return Task.CompletedTask;}}
