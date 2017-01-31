using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Resources;

[assembly: AssemblyProduct("IisLogRotator")]
[assembly: AssemblyTitle("IIS Log Rotator")]
[assembly: AssemblyDescription("IIS Log Rotation Program")]
[assembly: AssemblyCompany("Geoffrey Vancoetsem")]
[assembly: AssemblyCopyright("Copyright 2012 © Geoffrey Vancoetsem")]
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

[assembly: AssemblyVersion("1.8.2.0")]
[assembly: AssemblyFileVersion("1.8.2.0")]
[assembly: AssemblyInformationalVersion("1.8.2")]