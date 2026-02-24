using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Enums
{
    public enum InventoryTransactionType
    {
        Import,
        Move,
        Export,
        Adjust
    }
    public enum ReferenceType
    {
        GoodsReceipt,
        GoodsIssue,
        TransferOrder,
        Adjustment
    }
}
