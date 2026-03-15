using AgriIDMS.Application.DTOs.ProductVariant;
using AgriIDMS.Application.Exceptions;
using AgriIDMS.Application.Interfaces;
using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Exceptions;
using AgriIDMS.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Services
{
    public class ProductVariantService : IProductVariantService
    {
        private readonly IProductVariantRepository _repo;
        private readonly IBoxRepository _boxRepo;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<ProductVariantService> _logger;
        private readonly IProductRepository _productRepo;

        public ProductVariantService(
            IProductVariantRepository repo,
            IBoxRepository boxRepo,
            IUnitOfWork uow,
            ILogger<ProductVariantService> logger,
            IProductRepository product)
        {
            _repo = repo;
            _boxRepo = boxRepo;
            _uow = uow;
            _logger = logger;
            _productRepo = product;
        }

        public async Task<int> CreateAsync(CreateProductVariantDto dto)
        {
            _logger.LogInformation("Creating ProductVariant for product {ProductId}", dto.ProductId);
            var product = await _productRepo.GetProductByIdAsync(dto.ProductId);
            if (product == null)
                throw new NotFoundException("Product không tồn tại");
            var exists = await _repo.ExistsAsync(dto.ProductId, dto.Grade);
            if (exists)
                throw new InvalidBusinessRuleException("Đã tồn tại biến thể với grade này cho sản phẩm");
            var productCr = _productRepo.GetProductByIdAsync(dto.ProductId);
            var variant = new ProductVariant
            {
                ProductId = dto.ProductId,
                Name = $"{(await productCr)?.Name} {dto.Grade}",
                Grade = dto.Grade,
                Price = dto.Price,
                IsActive = true,
                ShelfLifeDays = dto.ShelfLifeDays,
                ImageUrl = dto.ImageUrl,
                MinReceiptWeight = dto.MinReceiptWeight
            };

            await _repo.AddAsync(variant);

            try
            {
                await _uow.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.InnerException?.Message);
                throw;
            }

            _logger.LogInformation("ProductVariant created with id {Id}", variant.Id);

            return variant.Id;
        }

        public async Task<IEnumerable<ProductVariantResponseDto>> GetAllAsync()
        {
            _logger.LogInformation("Getting all product variants");

            var variants = await _repo.GetAllAsync();

            var result = new List<ProductVariantResponseDto>();
            foreach (var x in variants)
            {
                var boxCount = await _boxRepo.GetAvailableBoxCountByVariantIdAsync(x.Id);
                result.Add(new ProductVariantResponseDto
                {
                    Id = x.Id,
                    ProductId = x.ProductId,
                    ProductName = $"{x.Product.Name} {x.Grade}",
                    Grade = x.Grade,
                    Price = x.Price,
                    IsActive = x.IsActive,
                    ShelfLifeDays = x.ShelfLifeDays,
                    ImageUrl = x.ImageUrl,
                    MinReceiptWeight = x.MinReceiptWeight,
                    AvailableBoxCount = boxCount
                });
            }
            return result;
        }

        public async Task<ProductVariantResponseDto> GetByIdAsync(int id)
        {
            _logger.LogInformation("Getting product variant {Id}", id);

            var variant = await _repo.GetProductVariantByIdAsync(id);

            if (variant == null)
                throw new NotFoundException("ProductVariant không tồn tại");

            var boxCount = await _boxRepo.GetAvailableBoxCountByVariantIdAsync(variant.Id);

            return new ProductVariantResponseDto
            {
                Id = variant.Id,
                ProductId = variant.ProductId,
                ProductName = $"{variant.Product.Name} {variant.Grade}",
                Grade = variant.Grade,
                Price = variant.Price,
                IsActive = variant.IsActive,
                ShelfLifeDays = variant.ShelfLifeDays,
                ImageUrl = variant.ImageUrl,
                MinReceiptWeight = variant.MinReceiptWeight,
                AvailableBoxCount = boxCount
            };
        }

        public async Task UpdateAsync(int id, UpdateProductVariantDto dto)
        {
            _logger.LogInformation("Updating ProductVariant {Id}", id);

            var variant = await _repo.GetProductVariantByIdAsync(id);

            if (variant == null)
                throw new NotFoundException("ProductVariant không tồn tại");

            if (dto.Grade.HasValue)
                variant.Grade = dto.Grade.Value;

            if (dto.Price.HasValue)
                variant.Price = dto.Price.Value;

            if (dto.IsActive.HasValue)
                variant.IsActive = dto.IsActive.Value;

            if (dto.ShelfLifeDays.HasValue)
                variant.ShelfLifeDays = dto.ShelfLifeDays.Value;

            if (dto.ImageUrl != null)
                variant.ImageUrl = dto.ImageUrl;

            variant.MinReceiptWeight = dto.MinReceiptWeight;

            _repo.Update(variant);

            await _uow.SaveChangesAsync();

            _logger.LogInformation("ProductVariant updated {Id}", id);
        }

        public async Task UpdateStatusAsync(int id, UpdateProductVariantStatusDto dto)
        {
            _logger.LogInformation("Updating status ProductVariant {Id}", id);

            var variant = await _repo.GetProductVariantByIdAsync(id);

            if (variant == null)
                throw new NotFoundException("ProductVariant không tồn tại");

            variant.IsActive = dto.IsActive;

            _repo.Update(variant);

            await _uow.SaveChangesAsync();

            _logger.LogInformation("ProductVariant updated {Id}", id);
        }

        public async Task DeleteAsync(int id)
        {
            _logger.LogInformation("Deleting ProductVariant {Id}", id);

            var variant = await _repo.GetProductVariantByIdAsync(id);

            if (variant == null)
                throw new NotFoundException("ProductVariant không tồn tại");

            _repo.Delete(variant);

            await _uow.SaveChangesAsync();

            _logger.LogInformation("ProductVariant deleted {Id}", id);
        }
    }
}
