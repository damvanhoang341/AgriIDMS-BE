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

    public async Task<PurchaseOrder?> GetByIdAsync(int id)
    {
        return await _context.PurchaseOrders
            .Include(x => x.Supplier)
            .Include(x => x.Details)
                .ThenInclude(x => x.ProductVariant)
                    .ThenInclude(pv => pv!.Product)
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

    public async Task UpdateAsync(PurchaseOrder purchaseOrder)
    {
        _context.PurchaseOrders.Update(purchaseOrder);
        await _context.SaveChangesAsync();
    }
}