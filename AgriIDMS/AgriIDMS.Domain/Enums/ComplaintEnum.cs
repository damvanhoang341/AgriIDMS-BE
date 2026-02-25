using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Enums
{
    public enum ComplaintType
    {
        Damaged = 1,
        MissingQuantity = 2,
        WrongItem = 3,
        Other = 4
    }

    public enum ComplaintStatus
    {
        Pending = 1,
        Verified = 2,
        Rejected = 3,
        Refunded = 4,
        Closed = 5
    }
}
