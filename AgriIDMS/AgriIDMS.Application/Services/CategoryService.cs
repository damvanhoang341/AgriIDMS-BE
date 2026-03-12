using AgriIDMS.Application.DTOs.Category;
using AgriIDMS.Application.Exceptions;
using AgriIDMS.Application.Interfaces;
using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Enums;
using AgriIDMS.Domain.Exceptions;
using AgriIDMS.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace AgriIDMS.Application.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categoryRepo;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<CategoryService> _logger;

        public CategoryService(
            ICategoryRepository categoryRepo,
            IUnitOfWork uow,
            ILogger<CategoryService> logger)
        {
            _categoryRepo = categoryRepo;
            _uow = uow;
            _logger = logger;
        }

        public async Task<int> CreateAsync(CreateCategoryRequest request)
        {
            _logger.LogInformation("Creating category: {Name}", request.Name);

            var existed = await _categoryRepo.ExistsByNameAsync(request.Name);

            if (existed)
                throw new InvalidBusinessRuleException("Tên danh mục đã tồn tại");

            var category = new Category(request.Name, request.Description);

            await _categoryRepo.AddAsync(category);

            await _uow.SaveChangesAsync();

            _logger.LogInformation("Category created successfully: {Id}", category.Id);

            return category.Id;
        }

        public async Task UpdateAsync(int id, UpdateCategoryRequest request)
        {
            _logger.LogInformation("Updating category: {Id}", id);

            var category = await _categoryRepo.GetByIdAsync(id);

            if (category == null)
                throw new NotFoundException("Danh mục không tồn tại");

            category.Update(
                request.Name,
                request.Description
            );

            _categoryRepo.Update(category);

            await _uow.SaveChangesAsync();

            _logger.LogInformation("Category updated: {Id}", id);
        }

        public async Task UpdateStatusAsync(int id, UpdateStatusCategoryRequest request)
        {
            _logger.LogInformation("Updating status category: {Id}", id);

            var category = await _categoryRepo.GetByIdAsync(id);

            if (category == null)
                throw new NotFoundException("Danh mục không tồn tại");

            category.UpdateStatus(
                (CategoryStatus)request.Status
            );

            _categoryRepo.Update(category);

            await _uow.SaveChangesAsync();

            _logger.LogInformation("Category status updated: {Id}", id);
        }

        public async Task DeleteAsync(int id)
        {
            _logger.LogInformation("Deleting category: {Id}", id);

            var category = await _categoryRepo.GetByIdAsync(id);

            if (category == null)
                throw new NotFoundException("Danh mục không tồn tại");

            if (category.Status == CategoryStatus.Deleted)
                throw new InvalidBusinessRuleException("Danh mục đã bị xóa");

            category.Delete();

            _categoryRepo.Update(category);

            await _uow.SaveChangesAsync();

            _logger.LogInformation("Category deleted: {Id}", id);
        }

        public async Task<CategoryDto?> GetByIdAsync(int id)
        {
            var category = await _categoryRepo.GetByIdAsync(id);

            if (category == null)
                return null;

            return new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
            };
        }

        public async Task<IEnumerable<CategoryDto>> GetAllAsync()
        {
            var categories = await _categoryRepo.GetAllAsync();

            return categories
                .Where(x => x.Status != CategoryStatus.Deleted)
                .Select(x => new CategoryDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description,
                    Status = x.Status
                });
        }

    }
}