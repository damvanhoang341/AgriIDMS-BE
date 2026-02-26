using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Interfaces
{
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync();
        public interface IUnitOfWork
        {
            Task BeginTransactionAsync();
            Task CommitAsync();
            Task RollbackAsync();
            Task<int> SaveChangesAsync();
        }
    }
}
