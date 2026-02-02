using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Exceptions
{
    public sealed class UnauthorizedException : ApplicationExceptionBase
    {
        public UnauthorizedException(string message): base(message)
        {
        }
    }
}
