using Microsoft.EntityFrameworkCore;
using SmartFactory.Domain.Entities;

// EF Core 上下文只负责数据库映射；生命周期由 IDbContextFactory 管理。
namespace SmartFactory.Infrastructure.Persistence;

/// <summary>SmartFactory SQLite 数据库上下文。</summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<Alarm> Alarms => Set<Alarm>();
    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
    public DbSet<TelemetryRecord> TelemetryRecords => Set<TelemetryRecord>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Device>().HasIndex(x => x.DeviceCode).IsUnique();
        modelBuilder.Entity<User>().HasIndex(x => x.Username).IsUnique();
        modelBuilder.Entity<Alarm>().HasIndex(x => new { x.OccurredAt, x.DeviceId });
        modelBuilder.Entity<TelemetryRecord>().HasIndex(x => new { x.DeviceId, x.Timestamp });
        modelBuilder.Entity<WorkOrder>().HasIndex(x => x.OrderNo).IsUnique();
    }
}
