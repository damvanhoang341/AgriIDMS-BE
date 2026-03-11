using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace AgriIDMS.Infrastructure.Repositories
{
    public class ExportReceiptRepository : IExportReceiptRepository
    {
        private readonly AppDbContext _context;

        public ExportReceiptRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(ExportReceipt receipt)
        {
            await _context.ExportReceipts.AddAsync(receipt);
        }

        public async Task<ExportReceipt?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.ExportReceipts
                .Include(e => e.Order)
                .Include(e => e.Details)
                    .ThenInclude(d => d.Box)
                        .ThenInclude(b => b.Slot)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<bool> ExistsForOrderAsync(int orderId)
        {
            return await _context.ExportReceipts
                .AnyAsync(e => e.OrderId == orderId
                    && e.Status != Domain.Enums.ExportStatus.Cancelled);
        }
    }
}
