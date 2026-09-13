using System.Windows;
using System.Windows.Controls;
using SmartFactory.Wpf.ViewModels;

// 仅处理 PasswordBox 这一 WPF 特有的不可直接绑定行为和窗口切换。
namespace SmartFactory.Wpf.Views;

/// <summary>登录窗口代码后置，保持无业务逻辑。</summary>
public partial class LoginWindow:Window
{
    private readonly LoginViewModel _viewModel;private bool _authenticated;
    public LoginWindow(LoginViewModel viewModel){InitializeComponent();DataContext=_viewModel=viewModel;PasswordInput.Password=viewModel.Password;_viewModel.LoginSucceeded+=OnLoginSucceeded;Closed+=OnClosed;}
    private void PasswordChanged(object sender,RoutedEventArgs e)=>_viewModel.Password=((PasswordBox)sender).Password;
    private void OnLoginSucceeded(object? sender,EventArgs e){_authenticated=true;((App)System.Windows.Application.Current).ShowMainWindow(this);}
    private void OnClosed(object? sender,EventArgs e){_viewModel.LoginSucceeded-=OnLoginSucceeded;if(!_authenticated&&System.Windows.Application.Current.MainWindow==this)System.Windows.Application.Current.Shutdown();}

    private void TextBox_TextChanged()
    {

    }
}
