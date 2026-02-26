using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Infrastructure.Repositories
{
    public class ProductVariantRepository : IProductVariantRepository
    {
        private readonly AppDbContext _context;
        public ProductVariantRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task<ProductVariant> GetProductVariantByIdAsync(int productVariantId)
        {
            var productVariant = await _context.ProductVariants.FindAsync(productVariantId);

            if (productVariant == null)
                throw new KeyNotFoundException($"ProductVariant with id {productVariantId} not found.");

            return productVariant;
        }
    }
}
