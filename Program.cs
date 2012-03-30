using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Globalization;
using System.Diagnostics;
using System.IO;
using System.DirectoryServices;

using Microsoft.Web.Administration;
using Smartgeek.LogRotator.Configuration;

namespace Smartgeek.LogRotator
{
	class Program
	{
		private const int MaxMissingCount = 100;
		private static bool SimulationMode = false;

		private static void Main(string[] args)
		{
			if (args != null)
			{
				SimulationMode = args.Contains("/simulate", StringComparer.OrdinalIgnoreCase) || args.Contains("/s", StringComparer.OrdinalIgnoreCase);

				if (SimulationMode)
				{
					Console.Out.WriteLine("Simulation Mode");
					Trace.TraceInformation("Simulation Mode");
				}
			}

			Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal;
			Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

			if (Environment.OSVersion.Platform != PlatformID.Win32NT)
			{
				Console.Error.WriteLine("Must be a Windows NT platform");
				Trace.TraceWarning("Abort: PlatformID = {0}", Environment.OSVersion.Platform);
				Environment.Exit(-1);
			}

			List<Folder> folders = new List<Folder>();

			if (Environment.OSVersion.Version.Major == 6)
			{
				Console.Out.WriteLine("Reading IIS 6.0 configuration...");
				Trace.TraceInformation("Reading IIS 6.0 configuration...");
				AddIis6xFolders(folders, skipHttp: true, skipFtp: true);

				Console.Out.WriteLine("Reading IIS 7.x configuration...");
				Trace.TraceInformation("Reading IIS 7.x configuration...");
				AddIis7xFolders(folders);
			}
			else if (Environment.OSVersion.Version.Major == 5 && Environment.OSVersion.Version.Minor == 1)
			{
				Console.Out.WriteLine("Reading IIS 5.x configuration...");
				Trace.TraceInformation("Reading IIS 5.x configuration...");
				AddIis6xFolders(folders);
			}
			else if (Environment.OSVersion.Version.Major == 5 && Environment.OSVersion.Version.Minor == 2)
			{
				Console.Out.WriteLine("Reading IIS 6.x configuration...");
				Trace.TraceInformation("Reading IIS 6.x configuration...");
				// IIS 6.0 is quite the same as IIS 5.0, so please don't blame me for this naming ^^
				AddIis6xFolders(folders);
			}
			else
			{
				Console.Error.WriteLine("Must be a Windows NT 5.1 or newer");
				Trace.TraceWarning("Abort: OSVersion = {0}", Environment.OSVersion);
				Environment.Exit(-1);
			}

			folders.ForEach(WriteFolderInfo);
			folders.ForEach(ProcessFolder);
		}

		private static void AddIis6xFolders(List<Folder> folders, bool skipHttp = false, bool skipFtp = false, bool skipSmtp = false, bool skipNntp = false)
		{
			using (DirectoryEntry lm = new DirectoryEntry(@"IIS://localhost"))
			{
				if (!skipHttp)
				{
					#region /LM/W3SVC
					DirectoryEntry w3svc = null;

					try
					{
						w3svc = lm.Children.Find("W3SVC", "IisWebService");
						Trace.TraceInformation("W3SVC feature found");
						Console.Out.WriteLine("W3SVC feature found");
					}
					catch (Exception ex)
					{
						Trace.TraceInformation("W3SVC feature not found ({0})", ex.Message);
						Console.Out.WriteLine("W3SVC feature not found");
					}

					if (w3svc != null)
					{
						try 
						{
							bool isCentralW3C = false, isCentralBinary = false, isUTF8 = false;

							// NT 5.2 or newer features
							if (Environment.OSVersion.Version >= new Version(5, 2))
							{
								// central log file is available starting with NT 5.2 SP1
								if (Environment.OSVersion.Version > new Version(5, 2)
									|| Environment.OSVersion.Version == new Version(5, 2) && !String.IsNullOrWhiteSpace(Environment.OSVersion.ServicePack))
								{
									isCentralW3C = (bool)w3svc.Properties["CentralW3CLoggingEnabled"].Value;
									isCentralBinary = (bool)w3svc.Properties["CentralBinaryLoggingEnabled"].Value;
								}

								// UTF-8 encoding is available starting with NT 5.2 RTM
								isUTF8 = (bool)w3svc.Properties["LogInUTF8"].Value;
							}

							if (isCentralW3C || isCentralBinary)
							{
								folders.Add(Folder.Create(
									w3svc,
									IisServiceType.W3SVC,
									isUTF8,
									isCentralW3C: isCentralW3C,
									isCentralBinary: isCentralBinary
								));
							}
							else
							{
								foreach (DirectoryEntry site in w3svc.Children.Cast<DirectoryEntry>().Where(e => e.SchemaClassName == "IIsWebServer"))
								{
									long siteId = Int64.Parse(site.Name);

									folders.Add(Folder.Create(
										site,
										IisServiceType.W3SVC,
										isUTF8,
										siteId: siteId
									));

									site.Close();
								}
							}

							w3svc.Close();
						}
						finally
						{
							w3svc.Dispose();
						}
					}
					#endregion
				}

				if (!skipFtp)
				{
					#region /LM/MSFTPSVC
					DirectoryEntry msftpsvc = null;

					try
					{
						msftpsvc = lm.Children.Find("MSFTPSVC", "IisFtpService");
						Trace.TraceInformation("MSFTPSVC feature found");
						Console.Out.WriteLine("MSFTPSVC feature found");
					}
					catch (Exception ex)
					{
						Trace.TraceInformation("MSFTPSVC feature not found ({0})", ex.Message);
						Console.Out.WriteLine("MSFTPSVC feature not found");
					}

					if (msftpsvc != null)
					{
						try
						{
							// server-level FTP log files encoding
							bool isFtpUTF8 = (bool)msftpsvc.Properties["FtpLogInUtf8"].Value;

							foreach (DirectoryEntry site in msftpsvc.Children.Cast<DirectoryEntry>().Where(e => e.SchemaClassName == "IIsFtpServer"))
							{
								long siteId = Int64.Parse(site.Name);

								// site-level FTP log files encoding
								bool isFtpSiteUTF8 = (bool)site.GetPropertyValue<bool>("FtpLogInUtf8", isFtpUTF8);

								folders.Add(Folder.Create(
									site,
									IisServiceType.MSFTPSVC,
									isFtpSiteUTF8,
									siteId: siteId
								));

								site.Close();
							}

							msftpsvc.Close();
						}
						finally
						{
							msftpsvc.Dispose();
						} 
					}
					#endregion
				}

				if (!skipSmtp)
				{
					#region /LM/SMTPSVC
					DirectoryEntry smtpsvc = null;

					try
					{
						smtpsvc = lm.Children.Find("SMTPSVC", "IIsSmtpService");
						Trace.TraceInformation("SMTPSVC feature found");
						Console.Out.WriteLine("SMTPSVC feature found");
					}
					catch (Exception ex)
					{
						Trace.TraceInformation("SMTPSVC feature not found ({0})", ex.Message);
						Console.Out.WriteLine("SMTPSVC feature not found");
					}

					if (smtpsvc != null)
					{
						try
						{
							foreach (DirectoryEntry site in smtpsvc.Children.Cast<DirectoryEntry>().Where(e => e.SchemaClassName == "IIsSmtpServer"))
							{
								long siteId = Int64.Parse(site.Name);

								folders.Add(Folder.Create(
									site,
									IisServiceType.SMTPSVC,
									false,
									siteId: siteId
								));

								site.Close();
							}

							smtpsvc.Close();
						}
						finally
						{
							smtpsvc.Dispose();
						}
					}
					#endregion
				}

				if (!skipNntp)
				{
					#region /LM/NNTPSVC
					DirectoryEntry nntpsvc = null;

					try
					{
						nntpsvc = lm.Children.Find("NNTPSVC", "IIsNntpService");
						Trace.TraceInformation("NNTPSVC feature found");
						Console.Out.WriteLine("NNTPSVC feature found");
					}
					catch (Exception ex)
					{
						Trace.TraceInformation("NNTPSVC feature not found ({0})", ex.Message);
						Console.Out.WriteLine("NNTPSVC feature not found");
					}

					if (nntpsvc != null)
					{
						try 
						{
							foreach (DirectoryEntry site in nntpsvc.Children.Cast<DirectoryEntry>().Where(e => e.SchemaClassName == "IIsNntpServer"))
							{
								long siteId = Int64.Parse(site.Name);

								folders.Add(Folder.Create(
									site,
									IisServiceType.NNTPSVC,
									false,
									siteId: siteId
								));

								site.Close();
							}
		    
							nntpsvc.Close();
						}
						finally
						{
							nntpsvc.Dispose();
						}
					}
					#endregion
				}

				lm.Close();
			}
		}

		private static void AddIis7xFolders(List<Folder> folders)
		{
			using (ServerManager serverManager = new ServerManager())
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
					folders.Add(Folder.Create(
						centralLogFile,
						IisServiceType.W3SVC,
						isUTF8,
						isCentralW3C: isCentralW3C,
						isCentralBinary: isCentralBinary
					));
				}

				if (isFtpServerCentralW3C)
				{
					folders.Add(Folder.Create(
						ftpServerCentralLogFile,
						IisServiceType.FTPSVC,
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
							folders.Add(Folder.Create(
								logFile,
								IisServiceType.W3SVC,
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
							folders.Add(Folder.Create(
								ftpServerLogFile,
								IisServiceType.FTPSVC,
								isFtpServerUTF8,
								siteId: siteId
							));
						}
					}
				}
			}
		}

		private static void WriteFolderInfo(Folder folder)
		{

		}

		private static void ProcessFolder(Folder folder)
		{
			if (!folder.Enabled)
			{
				Trace.TraceInformation("Skipping folder {0} because logging is disabled", folder.Directory);
				Console.Out.WriteLine("Skipping folder {0} because logging is disabled", folder.Directory);
				return;
			}

			if (folder.LogFormat == IisLogFormatType.Custom)
			{
				Trace.TraceInformation("Skipping folder {0} because custom logging is used", folder.Directory);
				Console.Out.WriteLine("Skipping folder {0} because custom logging is used", folder.Directory);
				return;
			}

			DirectoryInfo di = new DirectoryInfo(folder.Directory);
			if (!di.Exists)
			{
				Trace.TraceInformation("Skipping inexistant folder {0}", folder.Directory);
				Console.Out.WriteLine("Skipping inexistant folder {0}", folder.Directory);
				return;
			}
			
			// specific folder rotation settings or default settings
			RotationSettingsElement settings = RuntimeConfig.Rotation.GetSiteSettingsOrDefault(folder.ID);

			if (!settings.Compress && !settings.Delete)
			{
				Trace.TraceInformation("Skipping folder {0} because compression and deletion are disabled", folder.Directory);
				Console.Out.WriteLine("Skipping folder {0} because compression and deletion are disabled", folder.Directory);
				return;
			}

			// get all log files
			List<FileLogInfo> logFiles = new List<FileLogInfo>(
				di.GetFiles("*" + folder.FileExtension)
					.Select(f => new FileLogInfo(f, folder))
					.Where(f => f.IsChild)
					.OrderBy(f => f.Date)
			);

			if (logFiles.Count > 0)
			{
				// it's generaly safer to skip the latest log file because we don't know if IIS is still using it
				logFiles.RemoveAt(logFiles.Count - 1);
			}

			// get compressed log files
			List<FileLogInfo> compressedLogFiles = new List<FileLogInfo>(
				di.GetFiles("*" + folder.FileExtension + ".zip")
					.Select(f => new FileLogInfo(f, folder))
					.Where(f => f.IsChild)
					.OrderBy(f => f.Date)
			);

			List<FileLogInfo> deletedLogFiles = new List<FileLogInfo>(0);

			// datetime references
			bool useUTC = !folder.IsLocalTimeRollover;
			DateTime now = useUTC ? DateTime.Now.ToUniversalTime() : DateTime.Now;

			// file logs deletion
			if (settings.Delete)
			{
				DateTime deleteBeforeDate = now.AddDays(settings.DeleteAfter * -1d);

				// filter
				List<FileLogInfo> logFilesToDelete = new List<FileLogInfo>(
					logFiles.Union(compressedLogFiles).Where(f => f.File.Exists && f.Date < deleteBeforeDate)
				);

				if (logFilesToDelete.Count > 0)
				{
					Trace.TraceInformation("Folder {0}: {1} log files to delete...", folder.Directory, logFilesToDelete.Count);
					Console.Out.WriteLine("Folder {0}: {1} log files to delete...", folder.Directory, logFilesToDelete.Count);

					int deletedCount = 0;

					logFilesToDelete.ForEach(delegate(FileLogInfo fileLog)
					{
						if (Delete(fileLog, DeleteReasonType.Obsolete))
						{
							deletedCount++;
						}
					});

					Trace.TraceInformation("Folder {0}: {1} log files deleted", folder.Directory, deletedCount);
					Console.Out.WriteLine("Folder {0}: {1} log files deleted", folder.Directory, deletedCount);
				}
				else
				{
					Trace.TraceInformation("Folder {0}: no file to delete", folder.Directory);
					Console.Out.WriteLine("Folder {0}: no file to delete", folder.Directory);
				}
			}

			// file logs compression
			if (settings.Compress)
			{
				DateTime compressBeforeDate = now.AddDays(settings.CompressAfter * -1d);

				// filter
				List<FileLogInfo> logFilesToCompress = new List<FileLogInfo>(
					logFiles.Where(f => f.File.Exists && f.Date < compressBeforeDate)
				);

				if (logFilesToCompress.Count > 0)
				{
					Trace.TraceInformation("Folder {0}: {1} log files to compress...", folder.Directory, logFilesToCompress.Count);
					Console.Out.WriteLine("Folder {0}: {1} log files to compress...", folder.Directory, logFilesToCompress.Count);

					int compressedCount = 0, deletedCount = 0;

					logFilesToCompress.ForEach(delegate(FileLogInfo fileLog)
					{
						if (Compress(fileLog))
						{
							compressedCount++;
							if (Delete(fileLog, DeleteReasonType.PreviouslyCompressed))
							{
								deletedCount++;
							}
						}
					});

					Trace.TraceInformation("Folder {0}: {1} compressed, {2} deleted", folder.Directory, compressedCount, deletedCount);
					Console.Out.WriteLine("Folder {0}: {1} compressed, {2} deleted", folder.Directory, compressedCount, deletedCount);
				}
				else
				{
					Trace.TraceInformation("Folder {0}: no file to compress", folder.Directory);
					Console.Out.WriteLine("Folder {0}: no file to compress", folder.Directory);
				}
			}
		}

		// TODO predictive way of finding log files
		//private static void ProcessFolder2(Folder folder)
		//{
		//    Debug.WriteLine(
		//        "Folder: Enabled={0}, IsCustomFormat={1}, Directory={2}, FileLogFormat={3}, Period={4}, IsLocalTimeRollover={5}, TruncateSize={6}",
		//        folder.Enabled,
		//        folder.IsCustomFormat,
		//        folder.Directory,
		//        folder.FileLogFormat,
		//        folder.Period,
		//        folder.IsLocalTimeRollover,
		//        folder.TruncateSize
		//    );

		//    if (!folder.Enabled)
		//    {
		//        Console.Out.WriteLine("Skipping folder {0} because logging is disabled", folder.Directory);
		//        return;
		//    }

		//    if (folder.IsCustomFormat)
		//    {
		//        Console.Out.WriteLine("Skipping folder {0} because custom logging is used", folder.Directory);
		//        return;
		//    }

		//    // specific site rotation settings or default settings
		//    RotationSettingsElement settings;
		//    if (folder.SiteID.HasValue)
		//    {
		//        settings = RuntimeConfig.Rotation.GetSiteSettingsOrDefault(folder.SiteID.Value);
		//    }
		//    else
		//    {
		//        settings = RuntimeConfig.Rotation.DefaultSettings;
		//    }

		//    // datetime references
		//    bool useUTC = !folder.IsLocalTimeRollover;
		//    DateTime now = useUTC ? DateTime.Now.ToUniversalTime() : DateTime.Now;
		//    DateTime compressAfterDate = settings.Compress ? now.AddDays(settings.CompressAfter) : DateTime.MaxValue;
		//    DateTime deleteAfterDate = settings.Delete ? now.AddDays(settings.DeleteAfter) : DateTime.MaxValue;

		//    // file size rollover
		//    if (folder.Period == Folder.PeriodType.MaxSize)
		//    {
		//        for (int index = 1; ; index++)
		//        {
		//            String filename = Path.Combine(folder.Directory, String.Format(folder.FileLogFormat, index));
		//            String compressedFilename = String.Concat(filename, ".zip");
		//            String nextFilename = Path.Combine(folder.Directory, String.Format(folder.FileLogFormat, index + 1));

		//            // break on last log file extent
		//            // we don't know if IIS is still using it
		//            if (!File.Exists(nextFilename))
		//            {
		//                Console.Out.Write("Skipping last file {0}", filename);
		//                Trace.TraceInformation("{0} skipped (last file)", filename);
		//                break;
		//            }

		//            FileInfo fi = new FileInfo(filename);
		//            FileInfo fic = new FileInfo(compressedFilename);

		//            if (!fi.Exists && !fic.Exists)
		//            {
		//                break;
		//            }

		//            // deletion
		//            if ((useUTC && fi.CreationTimeUtc > deleteAfterDate) || (!useUTC && fi.CreationTime > deleteAfterDate))
		//            {
		//                Delete(fi);
		//            }
		//            if ((useUTC && fic.CreationTimeUtc > deleteAfterDate) || (!useUTC && fic.CreationTime > deleteAfterDate))
		//            {
		//                Delete(fic);
		//            }

		//            // compression
		//            if ((useUTC && fi.CreationTimeUtc > compressAfterDate) || (!useUTC && fi.CreationTime > compressAfterDate))
		//            {
		//                if (Compress(fi, fic))
		//                {
		//                    Delete(fi);
		//                }
		//            }
		//        }
		//    }
		//    // time-based rollover
		//    else
		//    {
		//        DateTime reference = now, lastReference = reference, nextReference;
		//        FileInfo lastReferenceFileInfo = null;

		//        do
		//        {
		//            switch (folder.Period)
		//            {
		//                case Folder.PeriodType.Hourly: reference = reference.AddHours(-1d); nextReference = reference.AddHours(-1d); break;
		//                case Folder.PeriodType.Daily: reference = reference.AddDays(-1d); nextReference = reference.AddDays(-1d); break;
		//                case Folder.PeriodType.Weekly: reference = reference.AddDays(-7d); nextReference = reference.AddDays(-7d); break;
		//                case Folder.PeriodType.Monthly: reference = reference.AddMonths(-1); nextReference = reference.AddMonths(-1); break;
		//                default: throw new NotImplementedException("Period " + folder.Period + " is not yet implemented");
		//            }

		//            String filename = Path.Combine(folder.Directory, String.Format(folder.FileLogFormat, reference, reference.GetWeekOfMonth()));
		//            String compressedFilename = String.Concat(filename, ".zip");
		//            String nextFilename = Path.Combine(folder.Directory, String.Format(folder.FileLogFormat, nextReference, nextReference.GetWeekOfMonth()));

		//            FileInfo fi = new FileInfo(filename);
		//            FileInfo fic = new FileInfo(compressedFilename);

		//            if (!fi.Exists && !fic.Exists)
		//            {
		//                if (lastReference - reference > TimeSpan.FromDays(730d))
		//                {
		//                    if (lastReferenceFileInfo != null)
		//                        Trace.TraceInformation("No more log file found in {0}. Last one was {1}", folder.Directory, lastReferenceFileInfo.Name);
		//                    else
		//                        Trace.TraceInformation("No log file found in {0}", folder.Directory);
		//                    break;
		//                }
		//            }
		//            else
		//            {
		//                lastReference = reference;
		//                lastReferenceFileInfo = fi.Exists ? fi : fic;
		//            }
		//        }
		//        while (true);
		//    }
		//}

		private static bool Delete(FileLogInfo fileLog, DeleteReasonType reasonType)
		{
			Trace.TraceInformation("{0} deleting (reason: {1})...", fileLog.File.FullName, reasonType);
			try
			{
				if (!SimulationMode)
				{
					fileLog.File.Delete();
					fileLog.File.Refresh();
					Trace.TraceInformation("{0} deleted", fileLog.File.FullName);
				}
				else
				{
					Trace.TraceInformation("{0} not deleted (simulation mode)", fileLog.File.FullName);
				}				
				return true;
			}
			catch (Exception ex)
			{
				Trace.TraceError("{0} delete error: {1}", fileLog.File.FullName, ex.Message);
				Trace.TraceError(ex.ToString());
				return false;
			}
		}

		private static bool Compress(FileLogInfo fileLog)
		{
			FileInfo compressedFileInfo = new FileInfo(String.Concat(fileLog.File.FullName, ".zip"));
			Trace.TraceInformation("{0} compressing to {1}...", fileLog.File.FullName, compressedFileInfo.Name);
			try
			{
				if (compressedFileInfo.Exists)
				{
					Trace.TraceInformation("{0} deleted (overwrite)", fileLog.File.FullName);
					compressedFileInfo.Delete();
				}

				using (Ionic.Zip.ZipFile zip = new Ionic.Zip.ZipFile(compressedFileInfo.FullName))
				{
					zip.CompressionLevel = Ionic.Zlib.CompressionLevel.BestCompression;
					zip.AddFile(fileLog.File.FullName, String.Empty);

					if (!SimulationMode)
					{
						zip.Save();
					}
				}
				Trace.TraceInformation("{0} compressed", fileLog.File.FullName);

				compressedFileInfo.Refresh();

				if (compressedFileInfo.Exists)
				{
					compressedFileInfo.CreationTimeUtc = fileLog.File.CreationTimeUtc;
					compressedFileInfo.LastWriteTimeUtc = fileLog.File.LastWriteTimeUtc;
				}

				return true;
			}
			catch (Exception ex)
			{
				Trace.TraceError("{0} compression error: {1}", fileLog.File.FullName, ex.Message);
				Trace.TraceError(ex.ToString());
				return false;
			}
		}
	}
}
