﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Globalization;

using Microsoft.Web.Administration;
using Smartgeek.LogRotator.Configuration;
using System.Diagnostics;
using System.IO;

namespace Smartgeek.LogRotator
{
	class Program
	{
		private const int MaxMissingCount = 100;

		private static List<Folder> s_folders = new List<Folder>();

		private static void Main(string[] args)
		{
			Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
			Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

			//using (ServerManager serverManager = new ServerManager())
			using (ServerManager serverManager = ServerManager.OpenRemote("rovms502"))
			{
				// applicationHost.config
				Microsoft.Web.Administration.Configuration config = serverManager.GetApplicationHostConfiguration();

				#region system.applicationHost/log
				ConfigurationSection logSection = config.GetSection("system.applicationHost/log");

				// central log file ?
				bool isCentralW3C = false;
				bool isCentralBinary = false;
				ConfigurationElement centralLogFile = null;
				ConfigurationAttribute centralLogFileModeAttr = logSection.GetAttribute("centralLogFileMode");
				switch ((int)centralLogFileModeAttr.Value)
				{
					case 0: break;
					case 1: isCentralBinary = true; centralLogFile = logSection.GetChildElement("centralBinaryLogFile"); break;
					case 2: isCentralW3C = true; centralLogFile = logSection.GetChildElement("centralW3CLogFile"); break;
					default: throw new NotSupportedException("The value " + centralLogFileModeAttr.Value + " for system.applicationHost/log/@centralLogFileMode is not supported");
				}

				// server-wide HTTP log files encoding
				bool isUTF8 = (bool)logSection.GetAttributeValue("logInUTF8"); 
				#endregion

				#region system.ftpServer/log
				ConfigurationSection ftpServerLogSection = config.GetSection("system.ftpServer/log");

				// FTP central log file ?
				bool isFtpServerCentralW3C = false;
				ConfigurationElement ftpServerCentralLogFile = null;
				ConfigurationAttribute ftpServerCentralLogFileModeAttr = ftpServerLogSection.GetAttribute("centralLogFileMode");
				switch ((int)ftpServerCentralLogFileModeAttr.Value)
				{
					case 0: break;
					case 1: isFtpServerCentralW3C = true; ftpServerCentralLogFile = ftpServerLogSection.GetChildElement("centralLogFile"); break;
					default: throw new NotSupportedException("The value " + ftpServerCentralLogFileModeAttr.Value + " for system.ftpServer/log/@centralLogFileMode is not supported");
				}

				// server-wide FTP log files encoding
				bool isFtpServerUTF8 = (bool)ftpServerLogSection.GetAttributeValue("logInUTF8"); 
				#endregion

				if (isCentralW3C || isCentralBinary)
				{
					s_folders.Add(Folder.Create(
						centralLogFile,
						Folder.IisServiceType.W3SVC,
						isUTF8,
						isCentralW3C: isCentralW3C,
						isCentralBinary: isCentralBinary
					));
				}

				if (isFtpServerCentralW3C)
				{
					s_folders.Add(Folder.Create(
						ftpServerCentralLogFile,
						Folder.IisServiceType.FTPSVC,
						isFtpServerUTF8,
						isCentralW3C: isFtpServerCentralW3C
					));
				}

				if (!isCentralW3C || !isCentralBinary || !isFtpServerCentralW3C)
				{
					// per-site log file processing
					ConfigurationSection sitesSection = config.GetSection("system.applicationHost/sites");
					ConfigurationElementCollection sitesCollection = sitesSection.GetCollection();

					foreach (ConfigurationElement site in sitesCollection)
					{
						long siteId = (long)site["id"];
						ConfigurationElementCollection bindingsCollection = site.GetCollection("bindings");
						
						// any http/https binding ?
						bool isWebSite = bindingsCollection.Any(binding =>
						{
							String protocol = (String)binding.GetAttributeValue("protocol");
							return StringComparer.InvariantCultureIgnoreCase.Equals(protocol, "http") || StringComparer.InvariantCultureIgnoreCase.Equals(protocol, "https");
						});

						if (isWebSite && !(isCentralW3C || isCentralBinary))
						{
							ConfigurationElement logFile = site.GetChildElement("logFile");
							s_folders.Add(Folder.Create(
								logFile,
								Folder.IisServiceType.W3SVC,
								isUTF8,
								siteId: siteId
							));
						}

						// any ftp binding ?
						bool isFtpSite = bindingsCollection.Any(binding =>
						{
							String protocol = (String)binding.GetAttributeValue("protocol");
							return StringComparer.InvariantCultureIgnoreCase.Equals(protocol, "ftp");
						});

						if (isFtpSite && !isFtpServerCentralW3C)
						{
							ConfigurationElement ftpServer = site.GetChildElement("ftpServer");
							ConfigurationElement ftpServerLogFile = ftpServer.GetChildElement("logFile");
							s_folders.Add(Folder.Create(
								ftpServerLogFile,
								Folder.IisServiceType.FTPSVC,
								isFtpServerUTF8,
								siteId: siteId
							));
						}
					}
				}
			}

			s_folders.ForEach(ProcessFolder);
		}

		private static void ProcessFolder(Folder folder)
		{
			Debug.WriteLine(
				"Folder: Enabled={0}, IsCustomFormat={1}, Directory={2}, FileLogFormat={3}, Period={4}, IsLocalTimeRollover={5}, TruncateSize={6}",
				folder.Enabled,
				folder.IsCustomFormat,
				folder.Directory,
				folder.FileLogFormat,
				folder.Period,
				folder.IsLocalTimeRollover,
				folder.TruncateSize
			);

			if (!folder.Enabled)
			{
				Console.Out.WriteLine("Skipping folder {0} because logging is disabled", folder.Directory);
				return;
			}

			if (folder.IsCustomFormat)
			{
				Console.Out.WriteLine("Skipping folder {0} because custom logging is used", folder.Directory);
				return;
			}
			
			// specific site rotation settings or default settings
			RotationSettingsElement settings;
			if (folder.SiteID.HasValue)
			{
				settings = RuntimeConfig.Rotation.GetSiteSettingsOrDefault(folder.SiteID.Value);
			}
			else
			{
				settings = RuntimeConfig.Rotation.DefaultSettings;
			}

			// datetime references
			bool useUTC = !folder.IsLocalTimeRollover;
			DateTime now = useUTC ? DateTime.Now.ToUniversalTime() : DateTime.Now;
			DateTime compressAfterDate = settings.Compress ? now.AddDays(settings.CompressAfter) : DateTime.MaxValue;
			DateTime deleteAfterDate = settings.Delete ? now.AddDays(settings.DeleteAfter) : DateTime.MaxValue;

			// file size rollover
			if (folder.Period == Folder.PeriodType.MaxSize)
			{
				for (int index = 1; ; index++)
				{
					String filename = Path.Combine(folder.Directory, String.Format(folder.FileLogFormat, index));
					String nextFilename = Path.Combine(folder.Directory, String.Format(folder.FileLogFormat, index + 1));

					// break on last log file
					// we don't know if IIS is still using it
					if (!File.Exists(nextFilename))
					{
						Console.Out.Write("Skipping last file {0}", filename);
						Trace.TraceInformation("{0} skipped (last file)", filename);
						break;
					}

					FileInfo fi = new FileInfo(filename);

					// deletion
					if ((useUTC && fi.CreationTimeUtc > deleteAfterDate) || (!useUTC && fi.CreationTime > deleteAfterDate))
					{
						Console.Out.Write("Deleting {0}... ", filename);
						try
						{
							//fi.Delete();
							Trace.TraceInformation("{0} deleted", filename);
							Console.Out.WriteLine("OK");
						}
						catch (Exception ex)
						{
							Trace.TraceError("{0} delete error: {1}", filename, ex.Message);
							Trace.TraceError(ex.ToString());
							Console.Out.WriteLine("ERROR: {0}", ex.Message);
						}
					}

					// compression
					/*if (((useUTC && fi.CreationTimeUtc > compressAfterDate) || (!useUTC && fi.CreationTime > compressAfterDate)) && fi.Attributes )
					{
						Console.Out.Write("Compressing {0}... ", filename);
						try
						{
							//fi.Delete();
							Trace.TraceInformation("{0} compressed", filename);
							Trace.TraceInformation("{0} deleted", filename);
							Console.Out.WriteLine("OK");
						}
						catch (Exception ex)
						{
							Trace.TraceError("{0} delete error: {1}", filename, ex.Message);
							Trace.TraceError(ex.ToString());
							Console.Out.WriteLine("ERROR: {0}", ex.Message);
						}
					}*/
				}
			}
			// time-based rollover
			else
			{
				// TODO
			}
		}
	}
}
