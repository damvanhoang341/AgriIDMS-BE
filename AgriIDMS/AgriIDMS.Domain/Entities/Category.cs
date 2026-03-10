using AgriIDMS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Entities
{
    public class Category
    {
        public int Id { get; private set; }
        public string Name { get; private set; } = null!;
        public string? Description { get; private set; }
        public CategoryStatus Status { get; private set; }
        public ICollection<Product> Products { get; private set; }= new List<Product>();
        // Constructor cho EF
        private Category() { }

        // Constructor tạo mới
        public Category(string name, string? description)
        {
            Name = name;
            Description = description;
            Status = CategoryStatus.Active;
        }

        // Update
        public void Update(string name, string? description, CategoryStatus status)
        {
            Name = name;
            Description = description;
            Status = status;
        }

        public void UpdateStatus(CategoryStatus status)
        {
            Status = status;
        }

        // Soft delete
        public void Delete()
        {
            Status = CategoryStatus.Deleted;
        }
    }
}
