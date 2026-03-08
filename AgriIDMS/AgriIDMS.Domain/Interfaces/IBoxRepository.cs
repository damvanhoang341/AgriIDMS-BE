using AgriIDMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Interfaces
{
    public interface IBoxRepository
    {
        Task<Box?> GetByIdAsync(int id);
        Task<Box?> GetByIdWithLotAndReceiptAsync(int id);
        Task CreateAsync(Box box);
        Task UpdateAsync(Box box);
    }
}
