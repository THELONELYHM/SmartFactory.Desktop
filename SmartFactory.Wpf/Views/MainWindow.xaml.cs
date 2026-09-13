using System.Windows;
using SmartFactory.Wpf.ViewModels;

// 仅处理注销时的窗口替换和正常关闭，不承载页面业务逻辑。
namespace SmartFactory.Wpf.Views;

/// <summary>主窗口代码后置，负责 Shell 生命周期。</summary>
public partial class MainWindow:Window
{
    private readonly MainViewModel _viewModel;private bool _loggingOut;
    public MainWindow(MainViewModel viewModel){InitializeComponent();DataContext=_viewModel=viewModel;_viewModel.LogoutRequested+=OnLogout;Closed+=OnClosed;}
    private void OnLogout(object? sender,EventArgs e){_loggingOut=true;((App)System.Windows.Application.Current).ShowLoginWindow(this);}
    private void OnClosed(object? sender,EventArgs e){_viewModel.LogoutRequested-=OnLogout;if(!_loggingOut)System.Windows.Application.Current.Shutdown();}
}
