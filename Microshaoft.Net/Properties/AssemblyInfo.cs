using Microshaoft.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyProduct(AssemblyInfoManager.AssemblyProduct)]

namespace Microshaoft.Net
{
    using System;
    using System.Reflection;
    public static class AssemblyInfoManager
    {

        public const string AssemblyProduct =
#if NET35
            "Microshaoft.Net for .NET 3.5"
#elif NET45
            "Microshaoft.Net for .NET 3.5"
#else
            "Microshaoft.Net for .NET"
#endif
            ;
    }
}
