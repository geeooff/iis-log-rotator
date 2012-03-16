using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Web.Administration;
using System.IO;
using System.Diagnostics;

namespace Smartgeek.LogRotator
{
	public class Folder
	{
		public enum IisServiceType
		{
			W3SVC,
			FTPSVC,
			SMTPSVC
		}

		public enum PeriodType
		{
			MaxSize = 0,
			Daily = 1,
			Weekly = 2,
			Monthly = 3,
			Hourly = 4
		}

		private Folder ()
		{

		}

		public long? SiteID { get; private set; }
		public bool Enabled { get; private set; }
		public bool IsCustomFormat { get; private set; }
		public String Directory { get; private set; }
		public String FileLogFormat { get; private set; }
		public PeriodType Period { get; private set; }
		public bool IsLocalTimeRollover { get; private set; }
		public long TruncateSize { get; private set; }

		public static Folder Create(ConfigurationElement logFileElement, IisServiceType type, bool isUTF8, bool isCentralW3C = false, bool isCentralBinary = false, long? siteId = null)
		{
			String dir = (String)logFileElement["directory"];
			String subdir = type.ToString("G");
			PeriodType periodType = (PeriodType)logFileElement["period"];
			String fileprefix = isCentralBinary ? null : isUTF8 ? "u_" : null;
			String filesuffix = ".log";
			bool isCustomFormat = false;

			if (isCentralW3C)
			{
				fileprefix += (periodType == PeriodType.MaxSize) ? "extend" : "ex";
			}
			else if (isCentralBinary)
			{
				fileprefix = (periodType == PeriodType.MaxSize) ? "raw" : "ra";
				filesuffix = ".ibl";
			}
			else
			{
				subdir += siteId.Value.ToString();
				fileprefix += (periodType == PeriodType.MaxSize) ? "extend" : "ex";

				if (type != IisServiceType.FTPSVC)
				{
					switch ((int)logFileElement["logFormat"])
					{
						case 0: fileprefix += (periodType == PeriodType.MaxSize) ? "inetsv" : "in"; break;
						case 1: fileprefix += (periodType == PeriodType.MaxSize) ? "ncsa" : "nc"; break;
						case 3: isCustomFormat = true; break;
					}
				}
			}

			String fileformat = "{0}";

			switch (periodType)
			{
				case PeriodType.Hourly: fileformat = "{0:yyMMddhh}"; break;
				case PeriodType.Daily: fileformat = "{0:yyMMdd}"; break;
				case PeriodType.Weekly: fileformat = "{0:yyMMww}"; break;
				case PeriodType.Monthly: fileformat = "{0:yyMM}"; break;
			}

			String directory = null;
			String fileLogFormat = null;
			if (!isCustomFormat)
			{
				directory = Path.Combine(
					dir,
					subdir
				);
				fileLogFormat = String.Concat(
					fileprefix,
					fileformat,
					filesuffix
				);
			}

			return new Folder()
			{
				SiteID = siteId,
				Enabled = (bool)logFileElement["enabled"],
				IsCustomFormat = isCustomFormat,
				Directory = directory,
				FileLogFormat = fileLogFormat,
				Period = periodType,
				IsLocalTimeRollover = (bool)logFileElement["localTimeRollover"],
				TruncateSize = (long)logFileElement["truncateSize"]
			};
		}
	}
}
