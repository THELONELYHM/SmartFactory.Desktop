using Microsoft.EntityFrameworkCore;
using SmartFactory.Application.Interfaces;
using SmartFactory.Application.Models;
using SmartFactory.Domain.Entities;
using SmartFactory.Domain.Enums;

// Repository 每次操作创建短生命周期 DbContext，查询默认关闭跟踪以降低开销。
namespace SmartFactory.Infrastructure.Persistence.Repositories;

/// <summary>设备数据访问实现。</summary>
public sealed class DeviceRepository(IDbContextFactory<AppDbContext> factory) : IDeviceRepository
{
    // 每个方法独立创建上下文，避免长生命周期 DbContext 的跟踪器持续增长。
    public async Task<Device?> GetByIdAsync(int id, CancellationToken ct) { await using var db = await factory.CreateDbContextAsync(ct); return await db.Devices.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct); }
    // AsNoTracking 用于只读列表，减少 ChangeTracker 开销。
    public async Task<IReadOnlyList<Device>> GetAllAsync(CancellationToken ct) { await using var db = await factory.CreateDbContextAsync(ct); return await db.Devices.AsNoTracking().OrderBy(x => x.DeviceCode).ToListAsync(ct); }
    public async Task AddAsync(Device device, CancellationToken ct) { await using var db = await factory.CreateDbContextAsync(ct); db.Devices.Add(device); await db.SaveChangesAsync(ct); }
    public async Task UpdateAsync(Device device, CancellationToken ct) { await using var db = await factory.CreateDbContextAsync(ct); db.Devices.Update(device); await db.SaveChangesAsync(ct); }
    // ExecuteDeleteAsync 直接生成 DELETE SQL，不需要先把实体加载到内存。
    public async Task DeleteAsync(int id, CancellationToken ct) { await using var db = await factory.CreateDbContextAsync(ct); await db.Devices.Where(x => x.Id == id).ExecuteDeleteAsync(ct); }
}

/// <summary>报警分页、写入和确认数据访问实现。</summary>
public sealed class AlarmRepository(IDbContextFactory<AppDbContext> factory) : IAlarmRepository
{
    public async Task<PagedResult<Alarm>> GetPagedAsync(PagedRequest request, AlarmLevel? level, CancellationToken ct)
    {
        // IQueryable 先逐步拼接过滤条件，Count 和分页查询最终由 SQLite 执行。
        await using var db = await factory.CreateDbContextAsync(ct); var query = db.Alarms.AsNoTracking();
        if (level is not null) query = query.Where(x => x.Level == level);
        if (!string.IsNullOrWhiteSpace(request.SearchText)) query = query.Where(x => x.DeviceCode.Contains(request.SearchText) || x.Message.Contains(request.SearchText));
        // 先 Count 再 Skip/Take，PagedResult 才能显示准确的总页数。
        var total = await query.CountAsync(ct); var items = await query.OrderByDescending(x => x.OccurredAt).Skip((request.PageIndex - 1) * request.PageSize).Take(request.PageSize).ToListAsync(ct);
        return new(items, total, request.PageIndex, request.PageSize);
    }
    public async Task AddAsync(Alarm alarm, CancellationToken ct) { await using var db = await factory.CreateDbContextAsync(ct); db.Alarms.Add(alarm); await db.SaveChangesAsync(ct); }
    public async Task AcknowledgeAsync(long id, string username, CancellationToken ct)
    {
        // 重新读取实体而不是使用列表对象，确保确认操作基于数据库当前状态。
        await using var db = await factory.CreateDbContextAsync(ct); var alarm = await db.Alarms.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (alarm is null || alarm.IsAcknowledged) return; alarm.IsAcknowledged = true; alarm.AcknowledgedAt = DateTime.Now; alarm.AcknowledgedBy = username; await db.SaveChangesAsync(ct);
    }
    public async Task<IReadOnlyList<Alarm>> GetRecentAsync(int count, CancellationToken ct) { await using var db = await factory.CreateDbContextAsync(ct); return await db.Alarms.AsNoTracking().OrderByDescending(x => x.OccurredAt).Take(count).ToListAsync(ct); }
}

/// <summary>工单分页、写入和删除数据访问实现。</summary>
public sealed class WorkOrderRepository(IDbContextFactory<AppDbContext> factory) : IWorkOrderRepository
{
    public async Task<PagedResult<WorkOrder>> GetPagedAsync(PagedRequest request, WorkOrderStatus? status, CancellationToken ct)
    {
        // 工单同样采用服务端分页思想，数据量增大时不会拖慢 DataGrid。
        await using var db = await factory.CreateDbContextAsync(ct); var query = db.WorkOrders.AsNoTracking();
        if (status is not null) query = query.Where(x => x.Status == status);
        if (!string.IsNullOrWhiteSpace(request.SearchText)) query = query.Where(x => x.OrderNo.Contains(request.SearchText) || x.Description.Contains(request.SearchText));
        var total = await query.CountAsync(ct); var items = await query.OrderByDescending(x => x.CreatedAt).Skip((request.PageIndex - 1) * request.PageSize).Take(request.PageSize).ToListAsync(ct);
        return new(items, total, request.PageIndex, request.PageSize);
    }
    public async Task AddAsync(WorkOrder order, CancellationToken ct) { await using var db = await factory.CreateDbContextAsync(ct); db.WorkOrders.Add(order); await db.SaveChangesAsync(ct); }
    public async Task UpdateAsync(WorkOrder order, CancellationToken ct) { await using var db = await factory.CreateDbContextAsync(ct); db.WorkOrders.Update(order); await db.SaveChangesAsync(ct); }
    public async Task DeleteAsync(long id, CancellationToken ct) { await using var db = await factory.CreateDbContextAsync(ct); await db.WorkOrders.Where(x => x.Id == id).ExecuteDeleteAsync(ct); }
}
