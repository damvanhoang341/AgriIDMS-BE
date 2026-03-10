using AgriIDMS.Application.DTOs.ProductVariant;
using AgriIDMS.Application.Exceptions;
using AgriIDMS.Application.Interfaces;
using AgriIDMS.Domain.Entities;
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
        private readonly IUnitOfWork _uow;
        private readonly ILogger<ProductVariantService> _logger;

        public ProductVariantService(
            IProductVariantRepository repo,
            IUnitOfWork uow,
            ILogger<ProductVariantService> logger)
        {
            _repo = repo;
            _uow = uow;
            _logger = logger;
        }

        public async Task<int> CreateAsync(CreateProductVariantDto dto)
        {
            _logger.LogInformation("Creating ProductVariant for product {ProductId}", dto.ProductId);

            var variant = new ProductVariant
            {
                ProductId = dto.ProductId,
                Grade = dto.Grade,
                Price = dto.Price,
                IsActive = true,
                ShelfLifeDays = dto.ShelfLifeDays,
                ImageUrl = dto.ImageUrl
            };

            await _repo.AddAsync(variant);

            await _uow.SaveChangesAsync();

            _logger.LogInformation("ProductVariant created with id {Id}", variant.Id);

            return variant.Id;
        }

        public async Task<IEnumerable<ProductVariantResponseDto>> GetAllAsync()
        {
            _logger.LogInformation("Getting all product variants");

            var variants = await _repo.GetAllAsync();

            return variants.Select(x => new ProductVariantResponseDto
            {
                Id = x.Id,
                ProductId = x.ProductId,
                ProductName = x.Product.Name,
                Grade = x.Grade,
                Price = x.Price,
                IsActive = x.IsActive,
                ShelfLifeDays = x.ShelfLifeDays,
                ImageUrl = x.ImageUrl
            });
        }

        public async Task<ProductVariantResponseDto> GetByIdAsync(int id)
        {
            _logger.LogInformation("Getting product variant {Id}", id);

            var variant = await _repo.GetProductVariantByIdAsync(id);

            if (variant == null)
                throw new NotFoundException("ProductVariant không tồn tại");

            return new ProductVariantResponseDto
            {
                Id = variant.Id,
                ProductId = variant.ProductId,
                ProductName = variant.Product.Name,
                Grade = variant.Grade,
                Price = variant.Price,
                IsActive = variant.IsActive,
                ShelfLifeDays = variant.ShelfLifeDays,
                ImageUrl = variant.ImageUrl
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
