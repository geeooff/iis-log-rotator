using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Web.Administration;
using System.Globalization;
using System.IO;
using System.DirectoryServices;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Smartgeek.LogRotator.Resources;

namespace Smartgeek.LogRotator
{
	internal static class Extensions
	{
		public static String GetAttributeValue2(this ConfigurationElement element, String name)
		{
			ConfigurationAttribute attr = element.Attributes.FirstOrDefault(a => a.Name == name);

			if (attr == null)
				return "<not found>";

			if (attr.Value == null)
				return "<null>";

			if (attr.Value.Equals(String.Empty))
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

		public static String StripeUtf8Prefix(this String str)
		{
			if (str.StartsWith("u_", StringComparison.Ordinal))
			{
				return str.Substring(2);
			}
			return str;
		}

		public static T GetPropertyValue<T>(this DirectoryEntry entry, String propertyName)
		{
			return (T)entry.Properties[propertyName].Value;
		}

		public static T GetPropertyValue<T>(this DirectoryEntry entry, String propertyName, T defaultValue)
		{
			if (entry.Properties.Contains(propertyName))
			{
				return (T)entry.Properties[propertyName].Value;
			}
			return defaultValue;
		}

		public static String GetIisVersionString(this OperatingSystem os)
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
					}
					break;
			}
			return Strings.MsgUnknownIisVersionString;
		}
	}
}
