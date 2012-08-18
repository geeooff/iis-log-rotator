using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Web.Administration;
using Smartgeek.LogRotator.Configuration;
using Smartgeek.LogRotator.Resources;

namespace Smartgeek.LogRotator
{
	class Program
	{
		private static bool s_simulationMode;
		private static LogHelper s_logHelper;

		private static void Main(string[] args)
		{
			using (s_logHelper = new LogHelper())
			{
				if (args != null)
				{
					s_simulationMode = args.Contains("/simulate", StringComparer.OrdinalIgnoreCase) || args.Contains("/s", StringComparer.OrdinalIgnoreCase);

					if (s_simulationMode)
					{
						s_logHelper.WriteLineOut(Strings.MsgSimulationMode, traceWarning: true);
						s_logHelper.WriteLineOut();
						Trace.TraceInformation(Strings.MsgSimulationMode);
					}
				}

				Process currentProcess = Process.GetCurrentProcess();

				// change the current process priority to be below normal
				// compression can be an expensive task, so we care about the other processes
				if (currentProcess.PriorityClass != ProcessPriorityClass.BelowNormal)
				{
					currentProcess.PriorityClass = ProcessPriorityClass.BelowNormal;
				}

				if (Environment.OSVersion.Platform != PlatformID.Win32NT)
				{
					s_logHelper.WriteLineError(Strings.MsgRequireWindowsNT, Environment.OSVersion.Platform, traceError: true);
					Environment.ExitCode = -1;
					return;
				}

				List<Folder> folders = new List<Folder>();

				String iisVersionInfo = Environment.OSVersion.GetIisVersionString();
				Trace.TraceInformation(Strings.MsgSummaryWindowsVersion, Environment.OSVersion);
				Trace.TraceInformation(Strings.MsgSummaryIisVersion, iisVersionInfo);

				if (Environment.OSVersion.Version.Major >= 6)
				{
					if (Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor > 2)
					{
						s_logHelper.WriteLineOut(Strings.MsgUnknownWindowsVersion, traceWarning: true);
					}

					bool skipLegacyFtpSvc = (Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor > 0);

					s_logHelper.WriteLineOut(Strings.MsgReadingAdsiConfig, iisVersionInfo, traceInfo: true);

					try
					{
						AddFoldersFromAdsi(folders, skipHttp: true, skipFtp: skipLegacyFtpSvc);
					}
					catch (Exception ex)
					{
						Die(s_logHelper, Strings.MsgAdsiAccessDenied, Strings.MsgCriticalException, ex);
					}

					s_logHelper.WriteLineOut(Strings.MsgReadingServerManagerConfig, iisVersionInfo, traceInfo: true);

					try
					{
						AddFoldersFromServerManager(folders);
					}
					catch (Exception ex)
					{
						Die(s_logHelper, Strings.MsgServerManagerAccessDenied, Strings.MsgCriticalException, ex);
					}
				}
				else if (Environment.OSVersion.Version.Major == 5 && Environment.OSVersion.Version.Minor >= 1)
				{
					s_logHelper.WriteLineOut(Strings.MsgReadingAdsiConfig, iisVersionInfo, traceInfo: true);

					try
					{
						AddFoldersFromAdsi(folders);
					}
					catch (Exception ex)
					{
						Die(s_logHelper, Strings.MsgAdsiAccessDenied, Strings.MsgCriticalException, ex);
					}
				}
				else
				{
					String message = String.Format(Strings.MsgRequireNewerWindows, Environment.OSVersion);
					s_logHelper.WriteLineError(message, traceError: true);
					Environment.Exit(-1);
				}

				s_logHelper.WriteLineOut();

				if (folders.Count > 0)
				{
					s_logHelper.WriteLineOut(Strings.MsgXFoldersToProcess, folders.Count, folders.Count > 1 ? Plurals.Folders : Plurals.Folder, traceInfo: true);
					folders.ForEach(WriteFolderInfo);

					s_logHelper.WriteLineOut();
					s_logHelper.WriteLineOut(Strings.MsgProcessing);

					folders.ForEach(ProcessFolder);
				}
				else
				{
					s_logHelper.WriteLineOut(Strings.MsgNoFolderToProcess, traceWarning: true);
				}

				s_logHelper.WriteLineOut();
				s_logHelper.WriteLineOut(Strings.MsgEnd);
			}
		}

		private static void AddFoldersFromAdsi(List<Folder> folders, bool skipHttp = false, bool skipFtp = false, bool skipSmtp = false, bool skipNntp = false)
		{
			using (DirectoryEntry lm = new DirectoryEntry(@"IIS://localhost"))
			{
				if (!skipHttp)
				{
					#region /LM/W3SVC
					DirectoryEntry w3svc = GetIisFeature(lm, "W3SVC", "IisWebService");

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
					DirectoryEntry msftpsvc = GetIisFeature(lm, "MSFTPSVC", "IIsFtpServer");
					
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
					DirectoryEntry smtpsvc = GetIisFeature(lm, "SMTPSVC", "IIsSmtpService"); ;

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
					DirectoryEntry nntpsvc = GetIisFeature(lm, "NNTPSVC", "IIsNntpService");

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

		private static DirectoryEntry GetIisFeature(DirectoryEntry lm, String name, String schemaClassName)
		{
			DirectoryEntry entry = null;

			try
			{
				entry = lm.Children.Find(name, schemaClassName);
				s_logHelper.WriteLineOut(Strings.MsgIisFeatureFound, name, traceInfo: true);
			}
			catch (DirectoryNotFoundException ex)
			{
				Trace.TraceInformation(Strings.MsgIisFeatureNotFound, name, ex.Message);
			}
			catch (DirectoryServicesCOMException ex)
			{
				if (ex.ErrorCode == 0x2030)
					Trace.TraceInformation(Strings.MsgIisFeatureNotFound, name, ex.Message);
				else
					throw ex;
			}

			return entry;
		}

		private static void AddFoldersFromServerManager(List<Folder> folders)
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

				bool isFtpServerCentralW3C = false;
				bool isFtpServerUTF8 = false;
				ConfigurationElement ftpServerCentralLogFile = null;

				// check for FTP 7.5 feature
				bool isFtpSvc75 = (config.RootSectionGroup.SectionGroups["system.ftpServer"] != null);

				if (isFtpSvc75)
				{
					Trace.TraceInformation(Strings.MsgFtpSvcFeatureFound);

					ConfigurationSection ftpServerLogSection = config.GetSection("system.ftpServer/log");

					// FTP central log file ?
					ConfigurationAttribute ftpServerCentralLogFileModeAttr = ftpServerLogSection.GetAttribute("centralLogFileMode");
					switch ((int)ftpServerCentralLogFileModeAttr.Value)
					{
						case 0: break;
						case 1: isFtpServerCentralW3C = true; ftpServerCentralLogFile = ftpServerLogSection.GetChildElement("centralLogFile"); break;
						default: throw new NotSupportedException("The value " + ftpServerCentralLogFileModeAttr.Value + " for system.ftpServer/log/@centralLogFileMode is not supported");
					}

					// server-wide FTP log files encoding
					isFtpServerUTF8 = (bool)ftpServerLogSection.GetAttributeValue("logInUTF8");
				}
				else
				{
					Trace.TraceInformation(Strings.MsgFtpSvcFeatureFound);
				}

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

				if (isFtpSvc75 && isFtpServerCentralW3C)
				{
					folders.Add(Folder.Create(
						ftpServerCentralLogFile,
						IisServiceType.FTPSVC,
						isFtpServerUTF8,
						isCentralW3C: isFtpServerCentralW3C
					));
				}

				if (!isCentralW3C || !isCentralBinary || (isFtpSvc75 && !isFtpServerCentralW3C))
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

						if (isFtpSvc75 && isFtpSite && !isFtpServerCentralW3C)
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
			String message = String.Format("{0}: Period = {1}, Format = {2}, Folder = {3}", folder.ID, folder.Period, folder.FilenameFormat, folder.Directory);
			s_logHelper.WriteLineOut(message, traceInfo: true);
		}

		private static void ProcessFolder(Folder folder)
		{
			if (!folder.Enabled)
			{
				s_logHelper.WriteLineOut("{0}: skipping because logging is disabled", folder.ID, traceInfo: true);
				return;
			}

			if (folder.LogFormat == IisLogFormatType.Custom)
			{
				s_logHelper.WriteLineOut("{0}: skipping because custom logging is used", folder.ID, traceInfo: true);
				return;
			}

			DirectoryInfo di = new DirectoryInfo(folder.Directory);
			if (!di.Exists)
			{
				s_logHelper.WriteLineOut("{0}: folder not found", folder.ID, traceInfo: true);
				return;
			}
			
			// specific folder rotation settings or default settings
			RotationSettingsElement settings = RuntimeConfig.Rotation.GetSiteSettingsOrDefault(folder.ID);

			if (!settings.Compress && !settings.Delete)
			{
				s_logHelper.WriteLineOut("{0}: skipping because compression and deletion are disabled", folder.ID, traceInfo: true);
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
				int lastIndex = logFiles.Count - 1;
				FileLogInfo lastLogFile = logFiles[lastIndex];
				logFiles.RemoveAt(lastIndex);
				s_logHelper.WriteLineOut("{0}: skipping {1} log file because it's the latest one", folder.ID, lastLogFile.File.Name, traceInfo: true);
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
					s_logHelper.WriteLineOut("{0}: {1} {2} to delete...", folder.ID, logFilesToDelete.Count, logFilesToDelete.Count > 1 ? "log files" : "log file", traceInfo: true);

					int deletedCount = 0;

					logFilesToDelete.ForEach(delegate(FileLogInfo fileLog)
					{
						if (Delete(fileLog, DeleteReasonType.Obsolete))
						{
							deletedCount++;
						}
					});

					s_logHelper.WriteLineOut("{0}: {1} {2} deleted", folder.ID, deletedCount, deletedCount > 1 ? "log files" : "log file", traceInfo: true);
				}
				else
				{
					Trace.TraceInformation("{0}: no file to delete", folder.ID);
					s_logHelper.WriteLineOut("{0}: no file to delete", folder.ID);
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
					s_logHelper.WriteLineOut("{0}: {1} {2} to compress...", folder.ID, logFilesToCompress.Count, logFilesToCompress.Count > 1 ? "log files" : "log file", traceInfo: true);

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

					s_logHelper.WriteLineOut("{0}: {1} compressed, {2} deleted", folder.ID, compressedCount, deletedCount, traceInfo: true);
				}
				else
				{
					s_logHelper.WriteLineOut("{0}: no file to compress", folder.ID, traceInfo: true);
				}
			}
		}

		private static bool Delete(FileLogInfo fileLog, DeleteReasonType reasonType)
		{
			Trace.TraceInformation("{0} deleting (reason: {1})...", fileLog.File.FullName, reasonType);
			try
			{
				if (!s_simulationMode)
				{
					fileLog.File.Delete();
					fileLog.File.Refresh();
					Trace.TraceInformation("{0} deleted", fileLog.File.FullName);
				}
				else
				{
					// TODO check for file deletion permission
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
					if (!s_simulationMode)
					{
						Trace.TraceInformation("{0} deleted (overwrite)", fileLog.File.FullName);
						compressedFileInfo.Delete();
					}
					else
					{
						// TODO check for file deletion permission
						Trace.TraceInformation("{0} not deleted (overwrite, simulation mode)", fileLog.File.FullName);
					}
				}

				using (Ionic.Zip.ZipFile zip = new Ionic.Zip.ZipFile(compressedFileInfo.FullName))
				{
					zip.CompressionLevel = Ionic.Zlib.CompressionLevel.BestCompression;
					zip.AddFile(fileLog.File.FullName, String.Empty);

					if (!s_simulationMode)
					{
						zip.Save();
						Trace.TraceInformation("{0} compressed", fileLog.File.FullName);
					}
					else
					{
						// TODO check for file compression permission
						Trace.TraceInformation("{0} not compressed (simulation mode)", fileLog.File.FullName);
					}
				}
				
				compressedFileInfo.Refresh();

				if (compressedFileInfo.Exists && !s_simulationMode)
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

		private static void Die(LogHelper logHelper, String messageIfAccessDenied, String messageIfUnhandledException, Exception ex)
		{
			if (ex is UnauthorizedAccessException || (ex is COMException && ((COMException)ex).ErrorCode == -2147024891)) // 0x80070005
			{
				logHelper.WriteLineOut(messageIfAccessDenied, traceError: true);
			}
			else
			{
				logHelper.WriteLineError(messageIfUnhandledException, traceError: true);
			}
#if DEBUG
			Debug.Fail(messageIfAccessDenied, ex.ToString());
#else
			throw ex;
#endif
		}
	}
}
