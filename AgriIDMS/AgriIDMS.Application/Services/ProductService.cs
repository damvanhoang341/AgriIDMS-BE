using AgriIDMS.Application.DTOs.Product;
using AgriIDMS.Application.Interfaces;
using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Interfaces;
using AgriIDMS.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using AgriIDMS.Application.Exceptions;

namespace AgriIDMS.Application.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepo;
        private readonly ICategoryRepository _categoryRepo;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<ProductService> _logger;

        public ProductService(
            IProductRepository productRepo,
            ICategoryRepository categoryRepo,
            IUnitOfWork uow,
            ILogger<ProductService> logger)
        {
            _productRepo = productRepo;
            _categoryRepo = categoryRepo;
            _uow = uow;
            _logger = logger;
        }

        public async Task<int> CreateAsync(CreateProductRequest request)
        {
            _logger.LogInformation("Creating product: {Name}", request.Name);

            var existed = await _productRepo.ExistsByNameAsync(request.Name);
            if (existed)
                throw new InvalidBusinessRuleException("Sản phẩm đã tồn tại");

            var category = await _categoryRepo.GetByIdAsync(request.CategoryId);
            if (category == null)
                throw new NotFoundException("Danh mục không tồn tại");

            var product = new Product
            {
                Name = request.Name,
                Description = request.Description,
                CategoryId = request.CategoryId,
                ImageUrl = request.ImageUrl
            };

            await _productRepo.AddProductAsync(product);

            await _uow.SaveChangesAsync();

            _logger.LogInformation("Product created: {Id}", product.Id);

            return product.Id;
        }

        public async Task<IEnumerable<object>> GetAllProducts()
        {
            _logger.LogInformation("Getting all products");

            var products = await _productRepo.GetAllProductsAsync();
            return products.Select(x => new
            {
                x.Id,
                x.Name,
                x.Description,
                Category = x.Category.Name,
                x.IsActive,
                x.CreatedAt,
                x.ImageUrl
            });
        }

        public async Task<object> GetByIdAsync(int id)
        {
            _logger.LogInformation("Getting product by id: {Id}", id);

            var product = await _productRepo.GetProductByIdAsync(id);

            if (product == null)
                throw new NotFoundException("Sản phẩm không tồn tại");

            return new
            {
                product.Id,
                product.Name,
                product.Description,
                Category = product.Category.Name,
                product.IsActive,
                product.CreatedAt,
                product.ImageUrl
            };
        }

        public async Task UpdateAsync(int id, UpdateProductRequest request)
        {
            _logger.LogInformation("Updating product: {Id}", id);

            var product = await _productRepo.GetProductByIdAsync(id);

            if (product == null)
                throw new NotFoundException("Sản phẩm không tồn tại");

            if (request.Name != null)
                product.Name = request.Name;

            if (request.Description != null)
                product.Description = request.Description;

            if (request.ImageUrl != null)
                product.ImageUrl = request.ImageUrl;

            if (request.CategoryId.HasValue)
            {
                var exists = await _categoryRepo.GetByIdAsync(request.CategoryId.Value);
                if (exists==null)
                    throw new NotFoundException("Danh mục không tồn tại");
                product.CategoryId = request.CategoryId.Value;
            }
                

            if (request.IsActive.HasValue)
                product.IsActive = request.IsActive.Value;

            await _productRepo.UpdateProductAsync(product);

            await _uow.SaveChangesAsync();

            _logger.LogInformation("Product updated: {Id}", id);
        }

        public async Task UpdateStatusAsync(int id, UpdateProductStatusRequest request)
        {
            _logger.LogInformation("Updating product: {Id}", id);

            var product = await _productRepo.GetProductByIdAsync(id);

            if (product == null)
                throw new NotFoundException("Sản phẩm không tồn tại");

            product.IsActive = request.IsActive;

            await _productRepo.UpdateProductAsync(product);

            await _uow.SaveChangesAsync();

            _logger.LogInformation("Product updated: {Id}", id);
        }

        public async Task DeleteAsync(int id)
        {
            _logger.LogInformation("Deleting product: {Id}", id);

            var product = await _productRepo.GetProductByIdAsync(id);

            if (product == null)
                throw new NotFoundException("Sản phẩm không tồn tại");

            product.IsActive = false;

            _productRepo.UpdateProductAsync(product);

            await _uow.SaveChangesAsync();

            _logger.LogInformation("Product deleted: {Id}", id);
        }
    }
}