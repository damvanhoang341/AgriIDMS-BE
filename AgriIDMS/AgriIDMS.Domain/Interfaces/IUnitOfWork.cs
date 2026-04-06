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
        Task BeginTransactionAsync();
        Task CommitAsync();
        Task RollbackAsync();
        Task<int> SaveChangesAsync();

        /// <summary>
        /// Runs <paramref name="operation"/> inside SqlServerRetryingExecutionStrategy + a single DB transaction.
        /// Required when EnableRetryOnFailure is on and the work uses explicit transactions.
        /// </summary>
        Task ExecuteInRetryableTransactionAsync(Func<Task> operation);
    }
}
