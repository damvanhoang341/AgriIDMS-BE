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

        public async Task<IEnumerable<ProductVariantResponseCustomerHomeDto>> GetAllProductVariantAsync()
        {
            _logger.LogInformation("Getting all product variants");

            var variants = await _repo.GetAllAsync();
            var result = new List<ProductVariantResponseCustomerHomeDto>();
            foreach (var x in variants)
            {
                result.Add(new ProductVariantResponseCustomerHomeDto
                {
                    Id = x.Id,
                    ProductId = x.ProductId,
                    ProductName = $"{x.Product.Name}",
                    Grade = x.Grade,
                    Price = x.Price,
                    ImageUrl = x.ImageUrl,
                });
            }
            return result;
        }

        public async Task<ProductVariantResponseCustomerDto> GetDetailAsync(int idProductVariant)
        {
            _logger.LogInformation("Getting detail product variants");

            var variant = await _repo.GetProductVariantByIdAsync(idProductVariant);

            if (variant == null)
                throw new Exception("Product variant not found");

            var boxTypeSummaries = await _boxRepo.GetAvailableBoxTypeSummaryByVariantIdAsync(variant.Id);

            var boxTypes = boxTypeSummaries
                .Select(bt => new BoxTypeDto
                {
                    BoxType = bt.IsPartial ? "Partial" : "Full",
                    Weight = bt.Weight,
                    AvailableCount = bt.AvailableCount,
                    BoxPrice = variant.Price * bt.Weight
                })
                .OrderBy(bt => bt.Weight)
                .ToList();

            var boxCount = boxTypes.Sum(bt => bt.AvailableCount);

            var result = new ProductVariantResponseCustomerDto
            {
                Id = variant.Id,
                ProductId = variant.ProductId,
                ProductName = $"{variant.Product.Name} {variant.Grade}",
                Grade = variant.Grade,
                Price = variant.Price,
                IsActive = variant.IsActive,
                ShelfLifeDays = variant.ShelfLifeDays,
                ImageUrl = variant.ImageUrl,
                AvailableBoxCount = boxCount,
                BoxTypes = boxTypes
            };

            return result;
        }
    }
}
