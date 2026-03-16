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
