using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartFactory.Application.Interfaces;
using SmartFactory.Application.Services;
using SmartFactory.Domain.Entities;
using SmartFactory.Domain.Enums;

// 首次启动种子数据用于离线演示；检测到设备后直接返回，保证重复启动幂等。
namespace SmartFactory.Infrastructure.Persistence;

/// <summary>创建数据库并填充演示设备、报警、工单和历史遥测。</summary>
public sealed class DatabaseInitializer(IDbContextFactory<AppDbContext> factory, ILogger<DatabaseInitializer> logger) : IDatabaseInitializer
{
    public async Task InitializeAsync(CancellationToken ct)
    {
        // EnsureCreated 适合零配置 Demo；生产持续演进时应切换为 EF Core Migrations。
        await using var db = await factory.CreateDbContextAsync(ct); await db.Database.EnsureCreatedAsync(ct);
        if (await db.Devices.AnyAsync(ct)) return;
        // 设备对象先加入数据库，SaveChanges 后 EF 会回填自增 Id，后续报警和遥测引用这些 Id。
        var devices = new[]
        {
            D("EQ-001","CNC一号加工中心","数控机床",DeviceStatus.Online,"A区 · 01"), D("EQ-002","CNC二号加工中心","数控机床",DeviceStatus.Warning,"A区 · 02"),
            D("EQ-003","六轴机器人A","工业机器人",DeviceStatus.Online,"B区 · 01"), D("EQ-004","自动装配机","装配设备",DeviceStatus.Online,"B区 · 03"),
            D("EQ-005","AOI检测设备","视觉检测",DeviceStatus.Fault,"C区 · 02"), D("EQ-006","空压机组","动力设备",DeviceStatus.Online,"动力站"),
            D("EQ-007","传送带A","输送设备",DeviceStatus.Offline,"A-B联线"), D("EQ-008","AGV-01","物流设备",DeviceStatus.Online,"仓储区")
        };
        db.Devices.AddRange(devices);
        var roles = new[] { new Role { Id=1, Name="Administrator", PermissionsCsv=string.Join(',', RolePermissions.For("Administrator")) }, new Role { Id=2, Name="Engineer", PermissionsCsv=string.Join(',', RolePermissions.For("Engineer")) }, new Role { Id=3, Name="Operator", PermissionsCsv=string.Join(',', RolePermissions.For("Operator")) } };
        db.Roles.AddRange(roles);
        db.Users.AddRange(U(1,"admin","系统管理员","admin123",1,"Administrator"), U(2,"engineer","设备工程师","engineer123",2,"Engineer"), U(3,"operator","产线操作员","operator123",3,"Operator"));
        await db.SaveChangesAsync(ct);
        // 固定随机种子让每次全新安装的演示数据分布稳定，便于复现和讲解。
        var now = DateTime.Now; var random = new Random(2025);
        for (var i = 0; i < 36; i++)
        {
            var device = devices[i % devices.Length]; var critical = i % 7 == 0;
            db.Alarms.Add(new Alarm { DeviceId=device.Id, DeviceCode=device.DeviceCode, DeviceName=device.DeviceName, Level=critical?AlarmLevel.Critical:AlarmLevel.Warning,
                Message=critical?"温度严重超限":"设备运行参数偏高", OccurredAt=now.AddHours(-i * 3), IsAcknowledged=i%3==0, AcknowledgedBy=i%3==0?"系统管理员":null, AcknowledgedAt=i%3==0?now.AddHours(-i*3).AddMinutes(8):null });
        }
        db.WorkOrders.AddRange(
            W("WO-202509-001",devices[4],"检查 AOI 相机通信与光源控制模块",WorkOrderPriority.Emergency,WorkOrderStatus.Processing,"设备工程师"),
            W("WO-202509-002",devices[1],"执行主轴温升趋势诊断并检查冷却液",WorkOrderPriority.High,WorkOrderStatus.Pending,null),
            W("WO-202509-003",devices[6],"更换传送带张紧轮并进行校准",WorkOrderPriority.Normal,WorkOrderStatus.Pending,null));
        foreach (var device in devices) for (var i = 0; i < 180; i++) db.TelemetryRecords.Add(new TelemetryRecord { DeviceId=device.Id, Timestamp=now.AddMinutes(-i*5), Temperature=45+random.NextDouble()*45, Pressure=.9+random.NextDouble()*.65, Speed=800+random.NextDouble()*3600 });
        await db.SaveChangesAsync(ct); logger.LogInformation("Demo database initialized at first startup");
    }
    private static Device D(string code,string name,string type,DeviceStatus status,string location)=>new(){DeviceCode=code,DeviceName=name,DeviceType=type,Status=status,Temperature=62,Pressure=1.15,Speed=2450,LastUpdateTime=DateTime.Now,Location=location,IsEnabled=true};
    private static User U(int id,string name,string display,string password,int roleId,string role)=>new(){Id=id,Username=name,DisplayName=display,PasswordHash=PasswordHasher.Hash(password),RoleId=roleId,RoleName=role,IsEnabled=true};
    private static WorkOrder W(string no,Device device,string description,WorkOrderPriority priority,WorkOrderStatus status,string? assignee)=>new(){OrderNo=no,DeviceId=device.Id,DeviceCode=device.DeviceCode,Description=description,Priority=priority,Status=status,Assignee=assignee,CreatedAt=DateTime.Now.AddDays(-2),CreatedBy="admin"};
}
