using AgriIDMS.Application.DTOs.Home;
using AgriIDMS.Application.DTOs.ProductVariant;
using AgriIDMS.Application.Interfaces;
using AgriIDMS.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Services
{
    /// <summary>Service dữ liệu trang chủ: hiển thị sản phẩm theo luồng Category → Product → ProductVariant.</summary>
    public class HomePageService : IHomePageService
    {
        private readonly ICategoryRepository _categoryRepo;
        private readonly IBoxRepository _boxRepo;
        private readonly ILogger<HomePageService> _logger;
        private readonly IProductVariantRepository _repo;

        public HomePageService(
            ICategoryRepository categoryRepo,
            IBoxRepository boxRepo,
            ILogger<HomePageService> logger,
            IProductVariantRepository repo)
        {
            _categoryRepo = categoryRepo;
            _boxRepo = boxRepo;
            _logger = logger;
            _repo = repo;
        }

        public async Task<HomePageCatalogResponse> GetCatalogForHomePageAsync()
        {
            _logger.LogInformation("Loading home page catalog: Category → Product → ProductVariant");

            var categories = (await _categoryRepo.GetActiveWithProductsAndVariantsForDisplayAsync()).ToList();
            var allVariants = categories
                .SelectMany(c => c.Products)
                .SelectMany(p => p.Variants)
                .ToList();
            var variantIds = allVariants.Select(v => v.Id).Distinct().ToList();

            var countTasks = variantIds.Select(id => _boxRepo.GetAvailableBoxCountByVariantIdAsync(id));
            var counts = await Task.WhenAll(countTasks);
            var availableByVariantId = variantIds.Zip(counts, (id, count) => (id, count)).ToDictionary(x => x.id, x => x.count);

            var categoryDtos = categories.Select(cat => new HomeCategoryDto
            {
                Id = cat.Id,
                Name = cat.Name,
                Description = cat.Description,
                Products = cat.Products
                    .OrderBy(p => p.Name)
                    .Select(p => new HomeProductDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        ImageUrl = p.ImageUrl,
                        Variants = p.Variants
                            .OrderBy(v => v.Grade.ToString())
                            .Select(v => new HomeProductVariantDto
                            {
                                Id = v.Id,
                                Name = v.Name,
                                Grade = v.Grade.ToString(),
                                Price = v.Price,
                                ImageUrl = v.ImageUrl,
                                AvailableBoxCount = availableByVariantId.GetValueOrDefault(v.Id, 0)
                            })
                            .ToList()
                    })
                    .ToList()
            }).ToList();

            return new HomePageCatalogResponse { Categories = categoryDtos };
        }

        public async Task<IEnumerable<ProductVariantResponseCustomerDto>> GetAllAsync()
        {
            _logger.LogInformation("Getting all product variants");

            var variants = await _repo.GetAllAsync();

            var result = new List<ProductVariantResponseCustomerDto>();
            foreach (var x in variants)
            {
                var boxCount = await _boxRepo.GetAvailableBoxCountByVariantIdAsync(x.Id);
                result.Add(new ProductVariantResponseCustomerDto
                {
                    Id = x.Id,
                    ProductId = x.ProductId,
                    ProductName = $"{x.Product.Name} {x.Grade}",
                    Grade = x.Grade,
                    Price = x.Price,
                    IsActive = x.IsActive,
                    ShelfLifeDays = x.ShelfLifeDays,
                    ImageUrl = x.ImageUrl,
                    AvailableBoxCount = boxCount
                });
            }
            return result;
        }
    }
}
