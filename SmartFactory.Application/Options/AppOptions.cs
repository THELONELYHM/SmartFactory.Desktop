// 配置模型与 appsettings.json 分节对应，避免阈值散落在业务代码中。
namespace SmartFactory.Application.Options;

/// <summary>实时采集和界面刷新参数。</summary>
public sealed class RealtimeOptions
{
    public const string SectionName = "Realtime";
    public int UiRefreshIntervalMs { get; set; } = 200;
    public int SimulationIntervalMs { get; set; } = 50;
    public int ReconnectSeconds { get; set; } = 5;
    public bool UseSimulation { get; set; } = true;
}
/// <summary>报警阈值和冷却去重参数。</summary>
public sealed class AlarmOptions
{
    public const string SectionName = "Alarm";
    public double WarningTemperature { get; set; } = 85;
    public double CriticalTemperature { get; set; } = 95;
    public double CriticalPressure { get; set; } = 1.6;
    public int CooldownSeconds { get; set; } = 30;
}
/// <summary>历史查询默认分页参数。</summary>
public sealed class HistoryOptions
{
    public const string SectionName = "History";
    public int DefaultPageSize { get; set; } = 50;
}
