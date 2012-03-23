using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Web.Administration;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Globalization;

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

		public enum LogFormatType
		{
			CentralBinary = -2,
			CentralW3C = -1,
			IIS = 0,
			NCSA = 1,
			W3C = 2,
			Custom = 3
		}

		private static readonly Regex FilenameFormatRegex;

		static Folder()
		{
			FilenameFormatRegex = new Regex(
				@"^(?<utf8prefix>u_)?(?:(?<dateBased>(?<format>in|nc|ex|ra)(?<year>\d{2})(?<month>\d{2})(?<dayOrWeek>\d{2})?(?<hour>\d{2})?)|(?<sizeBased>(?<format>inetsv|ncsa|extend|raw)(?<index>\d+)))(?<ext>\.log|\.ibl)(?<zip>\.zip)?$",
				RegexOptions.Compiled
			);
		}

		private Folder ()
		{
			
		}

		public long? SiteID { get; private set; }
		public bool Enabled { get; private set; }
		public bool IsUTF8 { get; private set; }
		public String Directory { get; private set; }
		public String FilenameFormat { get; private set; }
		public String FileExtension { get; private set; }
		public IisServiceType IisService { get; private set; }
		public PeriodType Period { get; private set; }
		public LogFormatType LogFormat { get; private set; }
		public bool IsLocalTimeRollover { get; private set; }
		public long TruncateSize { get; private set; }

		public bool IsChildLog(FileInfo fi)
		{
			DateTime date;
			return IsChildLog(fi, out date);
		}

		public bool IsChildLog(FileInfo fi, out DateTime date)
		{
			date = DateTime.MaxValue;
			Match match = FilenameFormatRegex.Match(fi.Name);

			// not a IIS log file
			if (!match.Success)
				return false;

			Group
				utf8prefixGroup = match.Groups["utf8prefix"],
				extGroup = match.Groups["ext"],
				sizeBasedGroup = match.Groups["sizeBased"],
				dateBasedGroup = match.Groups["dateBased"],
				formatGroup = match.Groups["format"],
				yearGroup = match.Groups["year"],
				monthGroup = match.Groups["month"],
				dayOrWeekGroup = match.Groups["dayOrWeek"],
				hourGroup = match.Groups["hour"],
				indexGroup = match.Groups["index"],
				zipGroup = match.Groups["zip"];

			// its encoding is different
			if (this.IsUTF8 != utf8prefixGroup.Success)
				return false;

			// its extension is different
			if (!extGroup.Success || this.FileExtension != extGroup.Value)
				return false;

			// its filename format is different
			if ((this.Period == PeriodType.MaxSize && !sizeBasedGroup.Success)
				|| (this.Period != PeriodType.MaxSize && !dateBasedGroup.Success)
				|| !formatGroup.Success)
				return false;

			// its log format is different
			switch (this.LogFormat)
			{
				case LogFormatType.CentralBinary:
					if (formatGroup.Value != (this.Period == PeriodType.MaxSize ? "raw" : "ra"))
						return false;
					break;

				case LogFormatType.IIS:
					if (formatGroup.Value != (this.Period == PeriodType.MaxSize ? "inetsv" : "in"))
						return false;
					break;

				case LogFormatType.NCSA:
					if (formatGroup.Value != (this.Period == PeriodType.MaxSize ? "ncsa" : "nc"))
						return false;
					break;

				case LogFormatType.W3C:
				case LogFormatType.CentralW3C:
					if (formatGroup.Value != (this.Period == PeriodType.MaxSize ? "extend" : "ex"))
						return false;
					break;

				// not yet implemented file format
				default:
					return false;
			}

			int index = 1, hour = 0, dayOrWeek = 1, month = -1, year = -1;

			// its period is different or a value in not parsable
			switch (this.Period)
			{
				case PeriodType.MaxSize:
					if (!indexGroup.Success
					 || !Int32.TryParse(indexGroup.Value, out index))
						return false;
					break;

				case PeriodType.Hourly:
					if (!hourGroup.Success
					 || !Int32.TryParse(indexGroup.Value, out hour))
						return false;
					goto case PeriodType.Daily;

				case PeriodType.Daily:
				case PeriodType.Weekly:
					if (!dayOrWeekGroup.Success
					 || !Int32.TryParse(dayOrWeekGroup.Value, out dayOrWeek))
						return false;
					goto case PeriodType.Monthly;

				case PeriodType.Monthly:
					if (!monthGroup.Success
					 || !Int32.TryParse(monthGroup.Value, out month)
					 || !yearGroup.Success
					 || !Int32.TryParse(yearGroup.Value, out year))
						return false;
					break;

				// not yet implemented period
				// TODO Investigate possibly new IIS version periods
				default:
					return false;
			}

			// date
			if (this.Period == PeriodType.MaxSize)
			{
				date = fi.CreationTime;
			}
			else if (this.Period == PeriodType.Weekly)
			{
				// TODO Week number to day conversion
				throw new NotImplementedException("Week number to day conversion is not yet implemented");
			}
			else
			{
				year = DateTimeFormatInfo.CurrentInfo.Calendar.ToFourDigitYear(year);
				date = new DateTime(year, month, dayOrWeek, hour, 0, 0);
			}
			
			return true;
		}

		public static Folder Create(ConfigurationElement logFileElement, IisServiceType iisServiceType, bool isUTF8, bool isCentralW3C = false, bool isCentralBinary = false, long? siteId = null)
		{
			String dir = (String)logFileElement["directory"];
			String subdir = iisServiceType.ToString("G");
			PeriodType periodType = (PeriodType)logFileElement["period"];
			String filePrefix = isCentralBinary ? null : isUTF8 ? "u_" : null;
			String fileExtension = ".log";

			LogFormatType logFormatType;

			if (isCentralBinary)
				logFormatType = LogFormatType.CentralBinary;
			else if (isCentralW3C)
				logFormatType = LogFormatType.CentralW3C;
			else
			{
				subdir += siteId.Value.ToString();
				logFormatType = (iisServiceType != IisServiceType.FTPSVC) ? (LogFormatType)logFileElement["logFormat"] : LogFormatType.W3C;
			}

			switch (logFormatType)
			{
				case LogFormatType.CentralBinary:
					filePrefix = (periodType == PeriodType.MaxSize) ? "raw" : "ra";
					fileExtension = ".ibl";
					break;

				case LogFormatType.IIS:
					filePrefix += (periodType == PeriodType.MaxSize) ? "inetsv" : "in";
					break;

				case LogFormatType.NCSA:
					filePrefix += (periodType == PeriodType.MaxSize) ? "ncsa" : "nc";
					break;

				case LogFormatType.CentralW3C:
				case LogFormatType.W3C:
					filePrefix += (periodType == PeriodType.MaxSize) ? "extend" : "ex";
					break;

				default:
					throw new NotImplementedException("logFormat=" + logFormatType + " is not yet implemented");
			}

			String fileformat;

			switch (periodType)
			{
				case PeriodType.MaxSize: fileformat = "{0}"; break;
				case PeriodType.Hourly: fileformat = "{0:yyMMddhh}"; break;
				case PeriodType.Daily: fileformat = "{0:yyMMdd}"; break;
				case PeriodType.Weekly: fileformat = "{0:yyMM}{1:00}"; break;
				case PeriodType.Monthly: fileformat = "{0:yyMM}"; break;
				default: throw new NotImplementedException("period=" + periodType + " is not yet implemented");
			}

			String directory = null;
			String fileLogFormat = null;
			
			if (logFormatType != LogFormatType.Custom)
			{
				directory = Path.Combine(
					dir,
					subdir
				);
				fileLogFormat = String.Concat(
					filePrefix,
					fileformat,
					fileExtension
				);
			}

			return new Folder()
			{
				SiteID = siteId,
				Enabled = (bool)logFileElement["enabled"],
				IsUTF8 = isUTF8,
				Directory = directory,
				FilenameFormat = fileLogFormat,
				FileExtension = fileExtension,
				IisService = iisServiceType,
				Period = periodType,
				LogFormat = logFormatType,
				IsLocalTimeRollover = (bool)logFileElement["localTimeRollover"],
				TruncateSize = (long)logFileElement["truncateSize"]
			};
		}
	}
}
