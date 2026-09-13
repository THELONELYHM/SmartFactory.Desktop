using System.Net.Http.Json;
using SmartFactory.Domain.Entities;

// REST 客户端是未来接入真实服务端的替换点，默认演示模式不会调用它。
namespace SmartFactory.Infrastructure.Api;

/// <summary>通过 HttpClientFactory 访问设备 API 的客户端。</summary>
public sealed class DeviceApiClient(HttpClient httpClient)
{
    public async Task<IReadOnlyList<Device>> GetDevicesAsync(CancellationToken ct) => await httpClient.GetFromJsonAsync<List<Device>>("api/devices",ct) ?? [];
}
