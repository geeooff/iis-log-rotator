using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Resources;

[assembly: AssemblyTitle("LogRotator")]
[assembly: AssemblyDescription("IIS Log Rotation Robot")]
[assembly: AssemblyCompany("smartgeek.net")]
[assembly: AssemblyProduct("LogRotator")]
[assembly: AssemblyCopyright("Copyright © smartgeek.net 2012")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
[assembly: Guid("2f022c49-94cb-48e3-accb-c232eabbf4f5")]
[assembly: NeutralResourcesLanguage("en-US")]

#if (DEBUG && TRACE)
[assembly: AssemblyConfiguration("Debug & Trace")]
#elif DEBUG
[assembly: AssemblyConfiguration("Debug")]
#elif TRACE
[assembly: AssemblyConfiguration("Trace")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif

[assembly: AssemblyVersion("1.7.0.0")]
[assembly: AssemblyFileVersion("1.7.0.0")]
[assembly: AssemblyInformationalVersion("1.7 alpha")]