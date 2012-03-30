using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Globalization;

namespace Smartgeek.LogRotator
{
	public class FileLogInfo : IEquatable<FileLogInfo>
	{
		private static readonly Regex FilenameFormatRegex;

		private bool _initialized;
		private bool _isChild;
		private DateTime _date;

		static FileLogInfo()
		{
			FilenameFormatRegex = new Regex(
				@"^(?<utf8prefix>u_)?(?:(?<dateBased>(?<format>in|nc|ex|ra)(?<year>\d{2})(?<month>\d{2})(?<dayOrWeek>\d{2})?(?<hour>\d{2})?)|(?<sizeBased>(?<format>inetsv|ncsa|extend|raw)(?<index>\d+)))(?<ext>\.log|\.ibl)(?<zip>\.zip)?$",
				RegexOptions.Compiled
			);
		}

		public FileLogInfo(FileInfo file, Folder folder)
		{
			this.File = file;
			this.Folder = folder;
		}

		public FileInfo File { get; private set; }
		public Folder Folder { get; private set; }

		public bool IsChild
		{
			get
			{
				Initialize();
				return _isChild;
			}
		}

		public DateTime Date
		{
			get
			{
				Initialize();
				return _date;
			}
		}

		private void Initialize()
		{
			if (_initialized)
				return;

			_date = DateTime.MaxValue;
			_isChild = false;
			_initialized = true;

			Match match = FilenameFormatRegex.Match(this.File.Name);

			// not a IIS log file
			if (!match.Success)
				return;

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
			if (this.Folder.IsUTF8 != utf8prefixGroup.Success)
				return;

			// its extension is different
			if (!extGroup.Success || this.Folder.FileExtension != extGroup.Value)
				return;

			// its filename format is different
			if ((this.Folder.Period == IisPeriodType.MaxSize && !sizeBasedGroup.Success)
				|| (this.Folder.Period != IisPeriodType.MaxSize && !dateBasedGroup.Success)
				|| !formatGroup.Success)
				return;

			// its log format is different
			switch (this.Folder.LogFormat)
			{
				case IisLogFormatType.CentralBinary:
					if (formatGroup.Value != (this.Folder.Period == IisPeriodType.MaxSize ? "raw" : "ra"))
						return;
					break;

				case IisLogFormatType.IIS:
					if (formatGroup.Value != (this.Folder.Period == IisPeriodType.MaxSize ? "inetsv" : "in"))
						return;
					break;

				case IisLogFormatType.NCSA:
					if (formatGroup.Value != (this.Folder.Period == IisPeriodType.MaxSize ? "ncsa" : "nc"))
						return;
					break;

				case IisLogFormatType.W3C:
				case IisLogFormatType.CentralW3C:
					if (formatGroup.Value != (this.Folder.Period == IisPeriodType.MaxSize ? "extend" : "ex"))
						return;
					break;

				// not yet implemented file format
				default:
					return;
			}

			int index = 1, hour = 0, dayOrWeek = 1, month = -1, year = -1;

			// its period is different or a value in not parsable
			switch (this.Folder.Period)
			{
				case IisPeriodType.MaxSize:
					if (!indexGroup.Success
						|| !Int32.TryParse(indexGroup.Value, out index))
						return;
					break;

				case IisPeriodType.Hourly:
					if (!hourGroup.Success
						|| !Int32.TryParse(indexGroup.Value, out hour))
						return;
					goto case IisPeriodType.Daily;

				case IisPeriodType.Daily:
				case IisPeriodType.Weekly:
					if (!dayOrWeekGroup.Success
						|| !Int32.TryParse(dayOrWeekGroup.Value, out dayOrWeek))
						return;
					goto case IisPeriodType.Monthly;

				case IisPeriodType.Monthly:
					if (!monthGroup.Success
						|| !Int32.TryParse(monthGroup.Value, out month)
						|| !yearGroup.Success
						|| !Int32.TryParse(yearGroup.Value, out year))
						return;
					break;

				// not yet implemented period
				// TODO Investigate possibly new IIS version periods
				default:
					return;
			}

			// date
			if (this.Folder.Period == IisPeriodType.MaxSize)
			{
				_date = this.File.CreationTime;
			}
			else if (this.Folder.Period == IisPeriodType.Weekly)
			{
				// TODO Week number to day conversion
				throw new NotImplementedException("Week number to day conversion is not yet implemented");
			}
			else
			{
				year = DateTimeFormatInfo.CurrentInfo.Calendar.ToFourDigitYear(year);
				_date = new DateTime(year, month, dayOrWeek, hour, 0, 0);
			}

			_isChild = true;
		}

		public override String ToString()
		{
			return this.File.FullName;
		}

		public bool Equals(FileLogInfo other)
		{
			if (other == null)
				return false;

			return StringComparer.CurrentCultureIgnoreCase.Equals(this.File.FullName, other.File.FullName);
		}
	}
}
