using System.Collections.Generic;

namespace AgriIDMS.Application.DTOs.Home
{
    /// <summary>Biến thể sản phẩm để hiển thị trên trang chủ.</summary>
    public class HomeProductVariantDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Grade { get; set; } = null!;
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public int AvailableBoxCount { get; set; }
    }

    /// <summary>Sản phẩm và các biến thể để hiển thị trên trang chủ.</summary>
    public class HomeProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public IList<HomeProductVariantDto> Variants { get; set; } = new List<HomeProductVariantDto>();
    }

    /// <summary>Danh mục và sản phẩm để hiển thị trên trang chủ (luồng Category → Product → ProductVariant).</summary>
    public class HomeCategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public IList<HomeProductDto> Products { get; set; } = new List<HomeProductDto>();
    }

    /// <summary>Response API catalog trang chủ.</summary>
    public class HomePageCatalogResponse
    {
        public IList<HomeCategoryDto> Categories { get; set; } = new List<HomeCategoryDto>();
    }
}
