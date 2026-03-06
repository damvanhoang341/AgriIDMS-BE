using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
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
        public async Task<IEnumerable<ProductVariant>> GetAllAsync()
        {
            return await _context.ProductVariants
                .Include(x => x.Product)
                .ToListAsync();
        }

        public async Task AddAsync(ProductVariant variant)
        {
            await _context.ProductVariants.AddAsync(variant);
        }

        public void Update(ProductVariant variant)
        {
            _context.ProductVariants.Update(variant);
        }

        public void Delete(ProductVariant variant)
        {
            _context.ProductVariants.Remove(variant);
        }
    }
}
