using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Exceptions
{
    public sealed class LockedException : ApplicationExceptionBase
    {
        public LockedException(string message) : base(message) { }
    }
}
