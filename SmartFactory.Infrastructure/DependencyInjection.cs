using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartFactory.Application.Interfaces;
using SmartFactory.Application.Options;
using SmartFactory.Application.Services;
using SmartFactory.Infrastructure.Api;
using SmartFactory.Infrastructure.Persistence;
using SmartFactory.Infrastructure.Persistence.Repositories;
using SmartFactory.Infrastructure.Realtime;
using SmartFactory.Infrastructure.Services;

// 基础设施层的唯一组合根：在这里把具体实现绑定到应用层接口。
namespace SmartFactory.Infrastructure;

/// <summary>注册数据库、仓储、业务服务和实时数据源。</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services,IConfiguration configuration)
    {
        // 将 JSON 分节绑定到强类型 Options，业务代码只读取 options.Value。
        services.Configure<RealtimeOptions>(configuration.GetSection(RealtimeOptions.SectionName));services.Configure<AlarmOptions>(configuration.GetSection(AlarmOptions.SectionName));services.Configure<HistoryOptions>(configuration.GetSection(HistoryOptions.SectionName));
        // 数据库放在 LocalApplicationData，避免把运行时数据库写进程序安装目录。
        var folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"SmartFactory");Directory.CreateDirectory(folder);var connection=$"Data Source={Path.Combine(folder,"smartfactory.db")}";
        services.AddPooledDbContextFactory<AppDbContext>(o=>o.UseSqlite(connection));
        // Repository 和页面服务使用 Transient；当前用户、实时源和聚合器需要跨页面共享。
        services.AddTransient<IDeviceRepository,DeviceRepository>();services.AddTransient<IAlarmRepository,AlarmRepository>();services.AddTransient<IWorkOrderRepository,WorkOrderRepository>();
        services.AddTransient<IDeviceService,DeviceService>();services.AddTransient<IAlarmService,AlarmService>();services.AddTransient<IWorkOrderService,WorkOrderService>();
        services.AddTransient<IAuthenticationService,AuthenticationService>();services.AddTransient<IHistoryService,HistoryService>();services.AddTransient<IDashboardService,DashboardService>();
        services.AddSingleton<ICurrentUserService,CurrentUserService>();services.AddSingleton<IDeviceRealtimeService,SimulatedDeviceRealtimeService>();services.AddSingleton<ISettingsService,SettingsService>();services.AddSingleton<IDatabaseInitializer,DatabaseInitializer>();services.AddSingleton<TelemetryAggregator>();
        // HttpClient 由工厂管理连接池；默认地址只是未来接入真实服务端的扩展点。
        services.AddHttpClient<DeviceApiClient>(client=>client.BaseAddress=new Uri(configuration["Api:BaseUrl"]??"https://localhost:7043/"));return services;
    }
}
