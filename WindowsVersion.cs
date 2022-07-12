using System;

namespace IisLogRotator
{
    internal static class WindowsVersion
    {
        internal static readonly Version WindowsNT4 = new Version(4, 0);
        internal static readonly Version Windows2000 = new Version(5, 0);
        internal static readonly Version WindowsXP = new Version(5, 1);
        internal static readonly Version WindowsXP64 = new Version(5, 2);
        internal static readonly Version WindowsServer2003 = new Version(5, 2);
        internal static readonly Version WindowsVista = new Version(6, 0);
        internal static readonly Version WindowsServer2008 = new Version(6, 0);
        internal static readonly Version Windows7 = new Version(6, 1);
        internal static readonly Version WindowsServer2008R2 = new Version(6, 1);
        internal static readonly Version Windows8 = new Version(6, 2);
        internal static readonly Version WindowsServer2012 = new Version(6, 2);
        internal static readonly Version Windows81 = new Version(6, 3);
        internal static readonly Version WindowsServer2012R2 = new Version(6, 3);
        internal static readonly Version Windows10 = new Version(10, 0);
        internal static readonly Version WindowsServer2016 = new Version(10, 0);
    }

    internal static class WindowsBuildVersion
    {
        internal static readonly Version WindowsNT4 = new Version(4, 0, 1381);
        internal static readonly Version Windows2000 = new Version(5, 0, 2195);
        internal static readonly Version WindowsXP = new Version(5, 1, 2600);
        internal static readonly Version WindowsXP64 = new Version(5, 2, 3790);
        internal static readonly Version WindowsServer2003 = new Version(5, 2, 3790);
        internal static readonly Version WindowsVistaRTM = new Version(6, 0, 6000);
        internal static readonly Version WindowsVistaSP1 = new Version(6, 0, 6001);
        internal static readonly Version WindowsVistaSP2 = new Version(6, 0, 6002);
        internal static readonly Version WindowsServer2008RTM = new Version(6, 0, 6001);
        internal static readonly Version WindowsServer2008SP2 = new Version(6, 0, 6002);
        internal static readonly Version Windows7RTM = new Version(6, 1, 7600);
        internal static readonly Version Windows7SP1 = new Version(6, 1, 7601);
        internal static readonly Version WindowsServer2008R2RTM = new Version(6, 1, 7600);
        internal static readonly Version WindowsServer2008R2SP1 = new Version(6, 1, 7601);
        internal static readonly Version Windows8 = new Version(6, 2, 9200);
        internal static readonly Version WindowsServer2012 = new Version(6, 2, 9200);
        internal static readonly Version Windows81 = new Version(6, 3, 9600);
        internal static readonly Version WindowsServer2012R2 = new Version(6, 3, 9600);
        internal static readonly Version Windows10RTM = new Version(10, 0, 10240);
        internal static readonly Version Windows10TH2 = new Version(10, 0, 10586);
        internal static readonly Version Windows10RS1 = new Version(10, 0, 14393);
        //internal static readonly Version Windows10RS2 = new Version(10, 0, 15007);
        internal static readonly Version WindowsServer2016RTM = new Version(10, 0, 14393);
    }
}