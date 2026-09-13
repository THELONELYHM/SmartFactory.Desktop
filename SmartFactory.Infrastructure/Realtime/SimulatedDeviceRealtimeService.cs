using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartFactory.Application.Interfaces;
using SmartFactory.Application.Models;
using SmartFactory.Application.Options;
using SmartFactory.Domain.Enums;

// 模拟器模拟真实设备网关；上层只依赖 IDeviceRealtimeService，不感知数据来源。
namespace SmartFactory.Infrastructure.Realtime;

/// <summary>离线运行的设备遥测生成器，支持取消和连接状态切换。</summary>
public sealed class SimulatedDeviceRealtimeService(IDeviceRepository repository, IOptions<RealtimeOptions> options, ILogger<SimulatedDeviceRealtimeService> logger) : IDeviceRealtimeService
{
    public ConnectionState State { get; private set; } = ConnectionState.Disconnected;
    public bool IsEnabled { get; set; } = true;
    public async IAsyncEnumerable<DeviceTelemetry> SubscribeAsync([EnumeratorCancellation] CancellationToken ct)
    {
        // 连接阶段只读取一次设备快照；真实实现可在这里建立 WebSocket/MQTT 连接。
        State=ConnectionState.Connecting;var devices=await repository.GetAllAsync(ct);State=ConnectionState.Connected;logger.LogInformation("Realtime service connected");
        var random=new Random();
        try
        {
            while(!ct.IsCancellationRequested)
            {
                // 关闭模拟器时保留协程，但降低轮询频率；设置页可再次打开它。
                if(!IsEnabled){await Task.Delay(250,ct);continue;}
                foreach(var device in devices.Where(x=>x.IsEnabled&&x.Status!=DeviceStatus.Offline))
                {
                    // 少量随机尖峰用于演示报警阈值和故障状态变化。
                    var spike=random.NextDouble()<.012?35:0;
                    yield return new(device.Id,45+random.NextDouble()*40+spike,.85+random.NextDouble()*.7+(random.NextDouble()<.008?.35:0),600+random.NextDouble()*3900,DateTime.Now);
                }
                await Task.Delay(Math.Max(20,options.Value.SimulationIntervalMs),ct);
            }
        }
        // finally 保证取消订阅或异常时都能恢复连接状态并记录日志。
        finally{State=ConnectionState.Disconnected;logger.LogInformation("Realtime service disconnected");}
    }
}
