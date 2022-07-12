using IisLogRotator.Resources;
using Microsoft.Web.Administration;
using System;
using System.DirectoryServices;
using System.Globalization;
using System.Linq;

namespace IisLogRotator
{
    internal static class Extensions
    {
        public static string GetAttributeValue2(this ConfigurationElement element, string name)
        {
            ConfigurationAttribute attr = element.Attributes.FirstOrDefault(a => a.Name == name);

            if (attr == null)
                return "<not found>";

            if (attr.Value == null)
                return "<null>";

            if (attr.Value.Equals(string.Empty))
                return "<empty>";

            return "\"" + attr.Value.ToString() + "\"";
        }

        public static int GetWeekOfMonth(this DateTime date)
        {
            if (date.Month == 1)
            {
                return date.GetWeekOfYear();
            }
            else
            {
                DateTime first = new DateTime(date.Year, date.Month, 1);
                return date.GetWeekOfYear() - first.GetWeekOfYear() + 1;
            }
        }

        public static int GetWeekOfYear(this DateTime date)
        {
            DateTimeFormatInfo dtfi = DateTimeFormatInfo.CurrentInfo;
            return dtfi.Calendar.GetWeekOfYear(
                date,
                dtfi.CalendarWeekRule,
                dtfi.FirstDayOfWeek
            );
        }

        public static string StripeUtf8Prefix(this string str)
        {
            if (str.StartsWith("u_", StringComparison.Ordinal))
            {
                return str.Substring(2);
            }
            return str;
        }

        public static T GetPropertyValue<T>(this DirectoryEntry entry, string propertyName)
        {
            return (T)entry.Properties[propertyName].Value;
        }

        public static T GetPropertyValue<T>(this DirectoryEntry entry, string propertyName, T defaultValue)
        {
            if (entry.Properties.Contains(propertyName))
            {
                return (T)entry.Properties[propertyName].Value;
            }
            return defaultValue;
        }

        public static string GetIisLegacyVersionString(this OperatingSystem os)
        {
            switch (os.Version.Major)
            {
                case 5:
                    switch (os.Version.Minor)
                    {
                        case 0: return "IIS 5.0";
                        case 1: return "IIS 5.1";
                        case 2: return "IIS 6.0";
                    }
                    break;

                case 6:
                    switch (os.Version.Minor)
                    {
                        case 0:
                        case 1:
                        case 2:
                            return "IIS 6.0";
                    }
                    break;
            }
            return Strings.MsgUnknownIisVersionString;
        }

        public static string GetIisVersionString(this OperatingSystem os)
        {
            switch (os.Version.Major)
            {
                case 5:
                    switch (os.Version.Minor)
                    {
                        case 0: return "IIS 5.0";
                        case 1: return "IIS 5.1";
                        case 2: return "IIS 6.0";
                    }
                    break;

                case 6:
                    switch (os.Version.Minor)
                    {
                        case 0: return "IIS 7.0";
                        case 1: return "IIS 7.5";
                        case 2: return "IIS 8.0";
                        case 3: return "IIS 8.5";
                    }
                    break;

                case 10:
                    switch (os.Version.Minor)
                    {
                        case 0: return "IIS 10.0";
                    }
                    break;
            }
            return Strings.MsgUnknownIisVersionString;
        }

        public static bool LessThan(this OperatingSystem os, int major, int minor)
        {
            return (os.Version.Major < major || (os.Version.Major == major && os.Version.Minor < minor));
        }

        public static bool LessThanEqual(this OperatingSystem os, int major, int minor)
        {
            return (os.Version.Major < major || (os.Version.Major == major && os.Version.Minor <= minor));
        }

        public static bool Equal(this OperatingSystem os, int major, int minor)
        {
            return (os.Version.Major == major && os.Version.Minor == minor);
        }

        public static bool GreaterThan(this OperatingSystem os, int major, int minor)
        {
            return (os.Version.Major > major || (os.Version.Major == major && os.Version.Minor > minor));
        }

        public static bool GreaterThanEqual(this OperatingSystem os, int major, int minor)
        {
            return (os.Version.Major > major || (os.Version.Major == major && os.Version.Minor >= minor));
        }
    }
}
