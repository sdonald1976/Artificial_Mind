using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.Errors;

public sealed class VersionMismatchException : Exception
{ public VersionMismatchException(string msg) : base(msg) {} }
