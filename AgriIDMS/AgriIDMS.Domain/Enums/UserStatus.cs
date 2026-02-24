using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Enums
{
    public enum UserStatus
    {
        Active = 1,
        Inactive = 2,
        Locked = 3,
        Deleted = 4
    }
    public enum SupplierStatus
    {
        Active = 0,
        Inactive = 1
    }
}
