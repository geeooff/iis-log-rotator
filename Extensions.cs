using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Web.Administration;
using System.Globalization;
using System.IO;

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

		public static DateTime GetIisLogDate(this FileInfo fi, Folder folder)
		{
			DateTime date;
			if (folder.IsChildLog(fi, out date))
			{
				return date;
			}
			return DateTime.MaxValue;
		}

		public static String StripeUtf8Prefix(this String str)
		{
			if (str.StartsWith("u_", StringComparison.Ordinal))
			{
				return str.Substring(2);
			}
			return str;
		}
	}
}
