using System;
using System.Collections.Generic;
using System.Text;

namespace Library.Domain.Exceptions
{
    public class BusinessRuleException : Exception
    {
        public BusinessRuleException(String message): base(message)
        {

        }
    }
}
