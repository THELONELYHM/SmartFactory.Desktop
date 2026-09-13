using Microsoft.Extensions.Logging.Abstractions;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;
using SmartFactory.Application.Exceptions;
using SmartFactory.Application.Interfaces;
using SmartFactory.Application.Models;
using SmartFactory.Application.Options;
using SmartFactory.Application.Services;
using SmartFactory.Domain.Entities;
using SmartFactory.Domain.Enums;
using SmartFactory.Wpf.Services;
using SmartFactory.Wpf.ViewModels;

// 通过 Fake Repository/Service 隔离外部依赖，验证可测试的业务行为。
namespace SmartFactory.Tests;

/// <summary>SmartFactory 核心业务和 ViewModel 的单元测试集合。</summary>
public sealed class SmartFactoryTests
{
    // 每个测试只验证一个行为，并使用 Fake 替代数据库、实时源和对话框。
    [Fact] public async Task DeviceService_Query_ReturnsRepositoryDevices(){var repo=new FakeDeviceRepository();repo.Items.Add(Device(1,"EQ-001","CNC"));var service=new DeviceService(repo,NullLogger<DeviceService>.Instance);Assert.Single(await service.GetDevicesAsync(default));}
    [Fact] public async Task AlarmService_Acknowledge_UsesCurrentUser(){var repo=new FakeAlarmRepository();var user=Engineer();var service=new AlarmService(repo,user,Options.Create(new AlarmOptions()),NullLogger<AlarmService>.Instance);await service.AcknowledgeAsync(12,default);Assert.Equal((12,"工程师"),repo.Acknowledged);}
    [Fact] public void Rbac_Engineer_CannotOpenSettings(){var user=Engineer();Assert.True(user.HasPermission(Permission.AlarmAcknowledge));Assert.False(user.HasPermission(Permission.SystemSetting));}
    [Fact] public async Task LoginViewModel_Success_RaisesEvent(){var vm=new LoginViewModel(new FakeAuthenticationService(false),NullLogger<LoginViewModel>.Instance);var raised=false;vm.LoginSucceeded+=(s,e)=>raised=true;await vm.LoginCommand.ExecuteAsync(null);Assert.True(raised);Assert.Null(vm.ErrorMessage);}
    [Fact] public async Task LoginViewModel_Failure_ShowsFriendlyError(){var vm=new LoginViewModel(new FakeAuthenticationService(true),NullLogger<LoginViewModel>.Instance);await vm.LoginCommand.ExecuteAsync(null);Assert.Equal("用户名或密码不正确。",vm.ErrorMessage);}
    [Fact] public async Task DeviceList_Refresh_MapsItems(){var repo=new FakeDeviceRepository();repo.Items.Add(Device(1,"EQ-001","CNC"));var vm=CreateDeviceVm(repo);await vm.RefreshAsync();Assert.Single(vm.Devices);Assert.Equal("EQ-001",vm.Devices[0].DeviceCode);}
    [Fact] public async Task DeviceList_Search_UsesCollectionViewWithoutRebuilding(){var repo=new FakeDeviceRepository();repo.Items.Add(Device(1,"EQ-001","CNC加工中心"));repo.Items.Add(Device(2,"EQ-002","装配机"));var vm=CreateDeviceVm(repo);await vm.RefreshAsync();var original=vm.Devices;vm.SearchText="CNC";Assert.Same(original,vm.Devices);Assert.Single(vm.DevicesView.Cast<object>());}
    [Fact] public void WorkOrderValidation_RejectsShortDescription(){var vm=new WorkOrderEditViewModel{DeviceCode="EQ-001",Description="坏了"};Assert.False(vm.Validate());Assert.True(vm.HasErrors);}
    [Fact] public void AlarmThreshold_CriticalTemperatureWins(){var value=new DeviceTelemetry(1,98,1.1,1000,DateTime.Now);var result=AlarmEvaluator.Evaluate(value,new AlarmOptions());Assert.Equal(AlarmLevel.Critical,result.Level);Assert.Equal("temperature",result.Kind);}
    [Fact] public void TelemetryAggregation_KeepsLatestPerDevice(){var agg=new TelemetryAggregator();agg.Add(new(1,70,1,1000,DateTime.Now));agg.Add(new(1,72,1.1,1200,DateTime.Now));agg.Add(new(2,60,1,900,DateTime.Now));var batch=agg.Drain();Assert.Equal(2,batch.Count);Assert.Equal(72,batch.Single(x=>x.DeviceId==1).Temperature);Assert.Empty(agg.Drain());}

    // 创建 ViewModel 时注入测试替身，证明页面逻辑不需要启动完整 Window。
    private static DeviceListViewModel CreateDeviceVm(FakeDeviceRepository repo)=>new(new DeviceService(repo,NullLogger<DeviceService>.Instance),new EmptyRealtime(),new NoopAlarmService(),new ImmediateUiDispatcher(),new NoopDialog(),Admin(),new TelemetryAggregator(),Options.Create(new RealtimeOptions()),NullLogger<DeviceListViewModel>.Instance);
    private static Device Device(int id,string code,string name)=>new(){Id=id,DeviceCode=code,DeviceName=name,DeviceType="测试",Status=DeviceStatus.Online,LastUpdateTime=DateTime.Now,Location="A"};
    private static CurrentUserService Engineer(){var service=new CurrentUserService();service.SetUser(new User{Username="engineer",DisplayName="工程师",RoleName="Engineer"});return service;}
    private static CurrentUserService Admin(){var service=new CurrentUserService();service.SetUser(new User{Username="admin",DisplayName="管理员",RoleName="Administrator"});return service;}

    // Fake 只保留测试需要的内存集合，不连接真实 SQLite。
    private sealed class FakeDeviceRepository:IDeviceRepository
    {public List<Device> Items{get;}=[];public Task<IReadOnlyList<Device>> GetAllAsync(CancellationToken ct)=>Task.FromResult<IReadOnlyList<Device>>(Items);public Task<Device?> GetByIdAsync(int id,CancellationToken ct)=>Task.FromResult(Items.FirstOrDefault(x=>x.Id==id));public Task AddAsync(Device d,CancellationToken ct){d.Id=Items.Count+1;Items.Add(d);return Task.CompletedTask;}public Task UpdateAsync(Device d,CancellationToken ct)=>Task.CompletedTask;public Task DeleteAsync(int id,CancellationToken ct){Items.RemoveAll(x=>x.Id==id);return Task.CompletedTask;}}
    private sealed class FakeAlarmRepository:IAlarmRepository
    {public (long,string) Acknowledged{get;private set;}public Task<PagedResult<Alarm>> GetPagedAsync(PagedRequest r,AlarmLevel? l,CancellationToken ct)=>Task.FromResult(new PagedResult<Alarm>([],0,1,20));public Task AddAsync(Alarm a,CancellationToken ct)=>Task.CompletedTask;public Task AcknowledgeAsync(long id,string user,CancellationToken ct){Acknowledged=(id,user);return Task.CompletedTask;}public Task<IReadOnlyList<Alarm>> GetRecentAsync(int count,CancellationToken ct)=>Task.FromResult<IReadOnlyList<Alarm>>([]);}
    private sealed class FakeAuthenticationService(bool fail):IAuthenticationService{public Task<User> LoginAsync(string u,string p,CancellationToken ct)=>fail?Task.FromException<User>(new AuthenticationException("用户名或密码不正确。")):Task.FromResult(new User{Username=u,DisplayName=u,RoleName="Administrator"});}
    private sealed class EmptyRealtime:IDeviceRealtimeService{public ConnectionState State=>ConnectionState.Connected;public bool IsEnabled{get;set;}=true;public async IAsyncEnumerable<DeviceTelemetry> SubscribeAsync([EnumeratorCancellation] CancellationToken ct){await Task.CompletedTask;yield break;}}
    private sealed class NoopAlarmService:IAlarmService{public Task<PagedResult<Alarm>> GetAlarmsAsync(PagedRequest r,AlarmLevel? l,CancellationToken ct)=>Task.FromResult(new PagedResult<Alarm>([],0,1,20));public Task AcknowledgeAsync(long id,CancellationToken ct)=>Task.CompletedTask;public Task ProcessTelemetryAsync(Device d,DeviceTelemetry t,CancellationToken ct)=>Task.CompletedTask;}
    private sealed class NoopDialog:IDialogService{public Task ShowMessageAsync(string t,string m)=>Task.CompletedTask;public Task<bool> ConfirmAsync(string t,string m)=>Task.FromResult(true);}
}
