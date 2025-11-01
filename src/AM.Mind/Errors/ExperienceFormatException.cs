using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.Errors;

public sealed class ExperienceFormatException : Exception
{ 
    public ExperienceFormatException(string msg) : base(msg) {} 
}
