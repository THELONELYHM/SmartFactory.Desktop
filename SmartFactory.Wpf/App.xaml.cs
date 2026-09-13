using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmartFactory.Application.Interfaces;
using SmartFactory.Infrastructure;
using SmartFactory.Wpf.Services;
using SmartFactory.Wpf.ViewModels;
using SmartFactory.Wpf.Views;

// WPF 启动类是桌面端组合根：负责 Host、DI、数据库初始化和全局异常兜底。
namespace SmartFactory.Wpf;

/// <summary>应用程序入口及 Generic Host 生命周期管理器。</summary>
public partial class App : System.Windows.Application
{
    private IHost? _host;
    // WPF 生命周期回调必须是 async void；内部业务操作仍使用可等待 Task。
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);RegisterGlobalExceptionHandlers();
        _host=Host.CreateDefaultBuilder().ConfigureAppConfiguration((_,c)=>c.SetBasePath(AppContext.BaseDirectory).AddJsonFile("appsettings.json",false,true))
            .ConfigureLogging(l=>l.ClearProviders().AddDebug()).ConfigureServices((context,services)=>
            {
                services.AddInfrastructure(context.Configuration);services.AddSingleton<IUiDispatcher,WpfUiDispatcher>();services.AddSingleton<IDialogService,DialogService>();services.AddSingleton<INavigationService,NavigationService>();
                services.AddTransient<LoginViewModel>();services.AddTransient<MainViewModel>();services.AddTransient<DashboardViewModel>();services.AddTransient<DeviceListViewModel>();services.AddTransient<AlarmViewModel>();services.AddTransient<WorkOrderViewModel>();services.AddTransient<HistoryViewModel>();services.AddTransient<SettingsViewModel>();
                services.AddTransient<LoginWindow>();services.AddTransient<MainWindow>();
            }).Build();
        try{await _host.StartAsync();await _host.Services.GetRequiredService<IDatabaseInitializer>().InitializeAsync(CancellationToken.None);_host.Services.GetRequiredService<ILogger<App>>().LogInformation("Application started");ShowLoginWindow();}
        catch(Exception ex){MessageBox.Show($"应用初始化失败：{ex.Message}","SmartFactory",MessageBoxButton.OK,MessageBoxImage.Error);Shutdown(-1);}
    }
    public void ShowMainWindow(Window login){if(_host is null)return;var main=_host.Services.GetRequiredService<MainWindow>();MainWindow=main;main.Show();login.Close();}
    public void ShowLoginWindow(Window? closing=null){if(_host is null)return;var login=_host.Services.GetRequiredService<LoginWindow>();MainWindow=login;login.Show();closing?.Close();}
    protected override async void OnExit(ExitEventArgs e){if(_host is not null){_host.Services.GetRequiredService<ILogger<App>>().LogInformation("Application stopped");await _host.StopAsync(TimeSpan.FromSeconds(5));_host.Dispose();}base.OnExit(e);}
    /// <summary>注册最后一道异常日志兜底，不替代业务层 try/catch。</summary>
    private void RegisterGlobalExceptionHandlers()
    {
        DispatcherUnhandledException+=(s,e)=>{_host?.Services.GetService<ILogger<App>>()?.LogCritical(e.Exception,"Unhandled UI exception");MessageBox.Show("发生未处理错误，详细信息已记录。","SmartFactory",MessageBoxButton.OK,MessageBoxImage.Error);e.Handled=true;};
        AppDomain.CurrentDomain.UnhandledException+=(s,e)=>_host?.Services.GetService<ILogger<App>>()?.LogCritical(e.ExceptionObject as Exception,"Unhandled domain exception");
        TaskScheduler.UnobservedTaskException+=(s,e)=>{_host?.Services.GetService<ILogger<App>>()?.LogError(e.Exception,"Unobserved task exception");e.SetObserved();};
    }
}
