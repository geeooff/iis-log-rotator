using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Web.Administration;

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
	}
}
