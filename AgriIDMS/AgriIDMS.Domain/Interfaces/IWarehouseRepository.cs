using AgriIDMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Interfaces
{
    public interface IWarehouseRepository
    {
        Task<Warehouse?> GetWarehouseByIdAsync(int warehouseId);
    }
}
