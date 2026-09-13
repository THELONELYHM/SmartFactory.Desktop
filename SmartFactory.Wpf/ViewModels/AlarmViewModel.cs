using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartFactory.Application.Interfaces;
using SmartFactory.Application.Models;
using SmartFactory.Domain.Entities;
using SmartFactory.Domain.Enums;

// 报警页面使用应用层分页结果，确认命令通过权限 CanExecute 控制。
namespace SmartFactory.Wpf.ViewModels;

/// <summary>报警中心查询、过滤、分页和确认。</summary>
public sealed partial class AlarmViewModel(IAlarmService service,ICurrentUserService user) : ViewModelBase, INavigationAware
{
    public ObservableCollection<Alarm> Alarms{get;}=[];public Array LevelOptions=>Enum.GetValues<AlarmLevel>();
    [ObservableProperty] private Alarm? _selectedAlarm;[ObservableProperty] private AlarmLevel? _selectedLevel;[ObservableProperty] private string _searchText=string.Empty;
    [ObservableProperty] private int _pageIndex=1;[ObservableProperty] private int _totalPages=1;[ObservableProperty] private int _totalCount;
    public bool CanAcknowledge=>user.HasPermission(Permission.AlarmAcknowledge);
    // 查询参数只传给应用服务，ViewModel 不关心 SQL 如何实现。
    [RelayCommand] public async Task RefreshAsync(CancellationToken ct=default){if(IsBusy)return;IsBusy=true;try{var page=await service.GetAlarmsAsync(new(PageIndex,20,SearchText),SelectedLevel,ct);Alarms.Clear();foreach(var item in page.Items)Alarms.Add(item);TotalPages=page.TotalPages;TotalCount=page.TotalCount;}catch(OperationCanceledException){}catch(Exception ex){ErrorMessage=ex.Message;}finally{IsBusy=false;}}
    [RelayCommand(CanExecute=nameof(CanAcknowledge))] private async Task AcknowledgeAsync(CancellationToken ct){if(SelectedAlarm is null||SelectedAlarm.IsAcknowledged)return;await service.AcknowledgeAsync(SelectedAlarm.Id,ct);await RefreshAsync(ct);}
    [RelayCommand] private async Task PreviousAsync(CancellationToken ct){if(PageIndex<=1)return;PageIndex--;await RefreshAsync(ct);}
    [RelayCommand] private async Task NextAsync(CancellationToken ct){if(PageIndex>=TotalPages)return;PageIndex++;await RefreshAsync(ct);}
    partial void OnSelectedLevelChanged(AlarmLevel? value){PageIndex=1;_ = RefreshAsync();}
    public Task OnNavigatedToAsync(CancellationToken ct)=>RefreshAsync(ct);public Task OnNavigatedFromAsync()=>Task.CompletedTask;
}
