using AgriIDMS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Entities
{
    public class Product
    {
        public int Id { get; private set; }          
        public string Name { get; private set; } = null!;
        public string? Description { get; private set; }
        public decimal Price { get; private set; }
        public string Unit { get; private set; } = null!;
        public string? ImageUrl { get; private set; }

        public int CategoryId { get; private set; }
        public Category Category { get; private set; } = null!;

        public ProductStatus Status { get; private set; }
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

        private Product() { }

        public Product(
            string name,
            decimal price,
            string unit,
            int categoryId,
            string? description = null,
            string? imageUrl = null)
        {
            Name = name;
            Price = price;
            Unit = unit;
            CategoryId = categoryId;
            Description = description;
            ImageUrl = imageUrl;
            Status = ProductStatus.Active;
        }
    }
}
