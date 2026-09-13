using System.Windows;
using System.Windows.Controls;
using SmartFactory.Domain.Enums;

// 自定义控件示例：使用 DependencyProperty 接入 WPF 属性系统和样式机制。
namespace SmartFactory.Wpf.Controls;

/// <summary>首页统计卡片，展示标题、数值和状态色。</summary>
public partial class DeviceStatusCard:UserControl
{
    public static readonly DependencyProperty TitleProperty=DependencyProperty.Register(nameof(Title),typeof(string),typeof(DeviceStatusCard),new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty ValueProperty=DependencyProperty.Register(nameof(Value),typeof(object),typeof(DeviceStatusCard),new PropertyMetadata(null));
    public static readonly DependencyProperty StatusProperty=DependencyProperty.Register(nameof(Status),typeof(DeviceStatus),typeof(DeviceStatusCard),new PropertyMetadata(DeviceStatus.Offline));
    public string Title{get=>(string)GetValue(TitleProperty);set=>SetValue(TitleProperty,value);}public object Value{get=>GetValue(ValueProperty);set=>SetValue(ValueProperty,value);}public DeviceStatus Status{get=>(DeviceStatus)GetValue(StatusProperty);set=>SetValue(StatusProperty,value);}
    public DeviceStatusCard()=>InitializeComponent();
}
