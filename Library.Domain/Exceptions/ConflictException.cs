using System;
using System.Collections.Generic;
using System.Text;

namespace Library.Domain.Exceptions;

public class ConflictException : Exception
{
    public ConflictException(string message)
        : base(message)
    {
    }
}
