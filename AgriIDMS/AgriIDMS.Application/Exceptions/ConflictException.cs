using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Exceptions
{
    public sealed class ConflictException : ApplicationExceptionBase
    {
        public ConflictException(string message) : base(message) { }
    }

}
