using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Exceptions
{
    public sealed class ForbiddenException : ApplicationExceptionBase
    {
        public ForbiddenException(string message) : base(message) { }
    }

}
