using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using SmartFactory.Domain.Enums;

// 转换器只负责把领域状态映射成视觉值，ViewModel 不依赖 Brush。
namespace SmartFactory.Wpf.Converters;

/// <summary>将设备状态映射为统一状态色。</summary>
public sealed class DeviceStatusBrushConverter:IValueConverter
{
    public object Convert(object value,Type targetType,object parameter,CultureInfo culture)=>value is DeviceStatus status?status switch{DeviceStatus.Online=>new SolidColorBrush(Color.FromRgb(31,201,139)),DeviceStatus.Warning=>new SolidColorBrush(Color.FromRgb(245,158,11)),DeviceStatus.Fault=>new SolidColorBrush(Color.FromRgb(239,68,68)),DeviceStatus.Maintenance=>new SolidColorBrush(Color.FromRgb(139,92,246)),_=>new SolidColorBrush(Color.FromRgb(100,116,139))}:Brushes.Gray;
    public object ConvertBack(object value,Type targetType,object parameter,CultureInfo culture)=>Binding.DoNothing;
}
/// <summary>将报警级别映射为蓝、橙、红色。</summary>
public sealed class AlarmLevelBrushConverter:IValueConverter
{
    public object Convert(object value,Type targetType,object parameter,CultureInfo culture)=>value is AlarmLevel level?level switch{AlarmLevel.Critical=>new SolidColorBrush(Color.FromRgb(239,68,68)),AlarmLevel.Warning=>new SolidColorBrush(Color.FromRgb(245,158,11)),_=>new SolidColorBrush(Color.FromRgb(14,165,233))}:Brushes.Gray;
    public object ConvertBack(object value,Type targetType,object parameter,CultureInfo culture)=>Binding.DoNothing;
}
/// <summary>详情面板无选中项时隐藏控件。</summary>
public sealed class NullToVisibilityConverter:IValueConverter
{
    public object Convert(object value,Type targetType,object parameter,CultureInfo culture)=>value is null?Visibility.Collapsed:Visibility.Visible;
    public object ConvertBack(object value,Type targetType,object parameter,CultureInfo culture)=>Binding.DoNothing;
}
