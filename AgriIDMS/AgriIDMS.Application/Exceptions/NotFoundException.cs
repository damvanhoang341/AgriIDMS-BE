using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Exceptions
{
    public sealed class NotFoundException : ApplicationExceptionBase
    {
        public NotFoundException(string entity, object key)
            : base($"{entity} with key '{key}' was not found.") { }
    }
}
