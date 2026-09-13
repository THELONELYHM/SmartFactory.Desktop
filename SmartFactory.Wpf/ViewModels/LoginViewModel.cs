using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SmartFactory.Application.Exceptions;
using SmartFactory.Application.Interfaces;

// 登录页面只负责输入状态和调用认证接口，不硬编码账号判断。
namespace SmartFactory.Wpf.ViewModels;

/// <summary>登录状态、异步命令和错误提示。</summary>
public sealed partial class LoginViewModel(IAuthenticationService authentication, ILogger<LoginViewModel> logger) : ViewModelBase
{
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(LoginCommand))] private string _username = "admin";
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(LoginCommand))] private string _password = "admin123";
    public event EventHandler? LoginSucceeded;
    private bool CanLogin() => !IsBusy && !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);

    [RelayCommand(CanExecute=nameof(CanLogin))]
    private async Task LoginAsync(CancellationToken ct)
    {
        // AsyncRelayCommand 会把 CancellationToken 传入这里；IsBusy 防止重复提交。
        if(IsBusy)return; IsBusy=true;ErrorMessage=null;LoginCommand.NotifyCanExecuteChanged();
        try { await authentication.LoginAsync(Username,Password,ct); Password=string.Empty; LoginSucceeded?.Invoke(this,EventArgs.Empty); }
        catch(AuthenticationException ex){ErrorMessage=ex.Message;}
        catch(OperationCanceledException){ }
        catch(Exception ex){logger.LogError(ex,"Unexpected login error");ErrorMessage="登录服务暂时不可用，请稍后重试。";}
        finally{IsBusy=false;LoginCommand.NotifyCanExecuteChanged();}
    }
}
