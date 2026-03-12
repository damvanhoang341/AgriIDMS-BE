using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

public class PurchaseOrderRepository : IPurchaseOrderRepository
{
    private readonly AppDbContext _context;

    public PurchaseOrderRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(PurchaseOrder order)
    {
        await _context.PurchaseOrders.AddAsync(order);
    }

    public async Task<IEnumerable<PurchaseOrder>> GetAllAsync()
    {
        return await _context.PurchaseOrders
            .Include(x => x.Supplier)
            .Include(x => x.Details)
                .ThenInclude(x => x.ProductVariant)
                    .ThenInclude(pv => pv!.Product)
            .ToListAsync();
    }

    public async Task<PurchaseOrder?> GetByIdAsync(int id)
    {
        return await _context.PurchaseOrders
            .Include(x => x.Supplier)
            .Include(x => x.Details)
                .ThenInclude(x => x.ProductVariant)
                    .ThenInclude(pv => pv!.Product)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<PurchaseOrder?> GetByIdWithGoodsReceiptsAsync(int id)
    {
        return await _context.PurchaseOrders
            .Include(x => x.Supplier)
            .Include(x => x.Details)
                .ThenInclude(x => x.ProductVariant)
                    .ThenInclude(pv => pv!.Product)
            .Include(x => x.GoodsReceipts)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<PurchaseOrderDetail?> GetDetailByIdAsync(int purchaseOrderDetailId)
    {
        return await _context.PurchaseOrderDetails
            .Include(d => d.PurchaseOrder)
            .Include(d => d.ProductVariant)
            .FirstOrDefaultAsync(d => d.Id == purchaseOrderDetailId);
    }

    public async Task<string> GenerateOrderCodeAsync()
    {
        var count = await _context.PurchaseOrders.CountAsync() + 1;

        return $"PO-{DateTime.UtcNow:yyyyMMdd}-{count:D4}";
    }

    public Task UpdateAsync(PurchaseOrder purchaseOrder)
    {
        _context.PurchaseOrders.Update(purchaseOrder);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(PurchaseOrder purchaseOrder)
    {
        _context.PurchaseOrders.Remove(purchaseOrder);
        return Task.CompletedTask;
    }

    public void RemoveDetails(IEnumerable<PurchaseOrderDetail> details)
    {
        foreach (var d in details)
            _context.PurchaseOrderDetails.Remove(d);
    }
}