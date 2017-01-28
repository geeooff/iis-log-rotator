using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Web.Administration;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Globalization;
using System.DirectoryServices;

namespace IisLogRotator
{
	[DebuggerDisplay("ID = {ID}, Enabled = {Enabled}, FilenameFormat = {FilenameFormat}, Directory = {Directory}")]
	public class Folder
	{
		private static readonly Guid IisLogModuleId = new Guid("{FF160657-DE82-11CF-BC0A-00AA006111E0}");
		private static readonly Guid NcsaLogModuleId = new Guid("{FF16065F-DE82-11CF-BC0A-00AA006111E0}");
		private static readonly Guid W3cLogModuleId = new Guid("{FF160663-DE82-11CF-BC0A-00AA006111E0}");
		private static readonly Guid OdbcLogModuleId = new Guid("{FF16065B-DE82-11CF-BC0A-00AA006111E0}");

		private Folder ()
		{
			
		}

		public string ID { get; private set; }
		public bool Enabled { get; private set; }
		public bool IsUTF8 { get; private set; }
		public string Directory { get; private set; }
		public string FilenameFormat { get; private set; }
		public string FileExtension { get; private set; }
		public IisServiceType IisService { get; private set; }
		public IisPeriodType Period { get; private set; }
		public IisLogFormatType LogFormat { get; private set; }
		public bool IsLocalTimeRollover { get; private set; }
		public long TruncateSize { get; private set; }

		public static Folder Create(ConfigurationElement logFileElement, IisServiceType iisServiceType, bool isUTF8, bool isCentralW3C = false, bool isCentralBinary = false, long? siteId = null)
		{
			IisLogFormatType logFormat;

			if (isCentralBinary)
				logFormat = IisLogFormatType.CentralBinary;
			else if (isCentralW3C)
				logFormat = IisLogFormatType.CentralW3C;
			else if (iisServiceType == IisServiceType.FTPSVC)
				logFormat = IisLogFormatType.W3C;
			else
				logFormat = (IisLogFormatType)logFileElement["logFormat"];

			return Create(
				(bool)logFileElement["enabled"],
				(string)logFileElement["directory"],
				iisServiceType,
				logFormat,
				(IisPeriodType)logFileElement["period"],
				isUTF8,
				(bool)logFileElement["localTimeRollover"],
				(long)logFileElement["truncateSize"],
				siteId
			);
		}

		public static Folder Create(DirectoryEntry logFileEntry, IisServiceType iisServiceType, bool isUTF8, bool isCentralW3C = false, bool isCentralBinary = false, long? siteId = null)
		{
			IisLogFormatType logFormat = IisLogFormatType.Custom;

			if (isCentralBinary)
			{
				logFormat = IisLogFormatType.CentralBinary;
			}
			else if (isCentralW3C)
			{
				logFormat = IisLogFormatType.CentralW3C;
			}
			else if (logFileEntry.Properties["LogPluginClsid"].Value != null)
			{
				Guid logPluginClsid = Guid.Parse((string)logFileEntry.Properties["LogPluginClsid"].Value);

				if (logPluginClsid == IisLogModuleId)
					logFormat = IisLogFormatType.IIS;
				else if (logPluginClsid == NcsaLogModuleId)
					logFormat = IisLogFormatType.NCSA;
				else if (logPluginClsid == W3cLogModuleId)
					logFormat = IisLogFormatType.W3C;
			}

			bool isLocaltimeRollover = false;

			if (logFileEntry.SchemaClassName == "IIsWebService"
			 || logFileEntry.SchemaClassName == "IIsWebServer"
			 || logFileEntry.SchemaClassName == "IIsFtpService"
			 || logFileEntry.SchemaClassName == "IIsFtpServer")
			{
				isLocaltimeRollover = (bool)logFileEntry.Properties["LogFileLocaltimeRollover"].Value;
			}

			return Create(
				((int)logFileEntry.Properties["LogType"].Value == 1),
				(string)logFileEntry.Properties["LogFileDirectory"].Value,
				iisServiceType,
				logFormat,
				(IisPeriodType)logFileEntry.Properties["LogFilePeriod"].Value,
				isUTF8,
				isLocaltimeRollover,
				(int)logFileEntry.Properties["LogFileTruncateSize"].Value,
				siteId
			);
		}

		private static Folder Create(bool enabled, string dir, IisServiceType iisService, IisLogFormatType logFormat, IisPeriodType period, bool isUTF8, bool isLocalTimeRollover, long truncateSize, long? siteId)
		{
			string subdir = iisService.ToString("G");
			string filePrefix = (logFormat == IisLogFormatType.CentralBinary) ? null : isUTF8 ? "u_" : null;
			string fileExtension = ".log";

			if (logFormat != IisLogFormatType.CentralBinary && logFormat != IisLogFormatType.CentralW3C)
			{
				subdir += siteId.Value.ToString();
			}

			switch (logFormat)
			{
				case IisLogFormatType.CentralBinary:
					filePrefix = (period == IisPeriodType.MaxSize) ? "raw" : "ra";
					fileExtension = ".ibl";
					break;

				case IisLogFormatType.IIS:
					filePrefix += (period == IisPeriodType.MaxSize) ? "inetsv" : "in";
					break;

				case IisLogFormatType.NCSA:
					filePrefix += (period == IisPeriodType.MaxSize) ? "ncsa" : "nc";
					break;

				case IisLogFormatType.CentralW3C:
				case IisLogFormatType.W3C:
					filePrefix += (period == IisPeriodType.MaxSize) ? "extend" : "ex";
					break;

				default:
					throw new NotImplementedException("logFormat=" + logFormat + " is not yet implemented");
			}

			string fileformat;

			switch (period)
			{
				case IisPeriodType.MaxSize: fileformat = "{0}"; break;
				case IisPeriodType.Hourly: fileformat = "{0:yyMMddhh}"; break;
				case IisPeriodType.Daily: fileformat = "{0:yyMMdd}"; break;
				case IisPeriodType.Weekly: fileformat = "{0:yyMM}{1:00}"; break;
				case IisPeriodType.Monthly: fileformat = "{0:yyMM}"; break;
				default: throw new NotImplementedException("period=" + period + " is not yet implemented");
			}

			string directory = null;
			string fileLogFormat = null;

			if (logFormat != IisLogFormatType.Custom)
			{
				directory = Path.Combine(
					Environment.ExpandEnvironmentVariables(dir),
					subdir
				);
				fileLogFormat = string.Concat(
					filePrefix,
					fileformat,
					fileExtension
				);
			}

			return new Folder()
			{
				ID = subdir,
				Enabled = enabled,
				IsUTF8 = isUTF8,
				Directory = directory,
				FilenameFormat = fileLogFormat,
				FileExtension = fileExtension,
				IisService = iisService,
				Period = period,
				LogFormat = logFormat,
				IsLocalTimeRollover = isLocalTimeRollover,
				TruncateSize = truncateSize
			};
		}

		public override string ToString()
		{
			return this.Directory;
		}
	}
}
