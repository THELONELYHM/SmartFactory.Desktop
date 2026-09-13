using System.Windows.Controls;

// 页面代码后置统一保持最小化，只负责初始化由 XAML 声明的 UserControl。
namespace SmartFactory.Wpf.Views;

public partial class DashboardView:UserControl{public DashboardView()=>InitializeComponent();}
public partial class DeviceListView:UserControl{public DeviceListView()=>InitializeComponent();}
public partial class AlarmView:UserControl{public AlarmView()=>InitializeComponent();}
public partial class WorkOrderView:UserControl{public WorkOrderView()=>InitializeComponent();}
public partial class HistoryView:UserControl{public HistoryView()=>InitializeComponent();}
public partial class SettingsView:UserControl{public SettingsView()=>InitializeComponent();}
