using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Enums
{
    public enum ProductStatus
    {
        Inactive = 0,
        Active = 1,
        OutOfStock = 2
    }

    public enum ProductGrade
    {
        Grade1 = 1,   // Loại 1
        Grade2 = 2,   // Loại 2
        Grade3 = 3    // Loại 3 (nếu có)
    }
}
