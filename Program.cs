using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Globalization;

using Microsoft.Web.Administration;
using Smartgeek.LogRotator.Configuration;
using System.Diagnostics;

namespace Smartgeek.LogRotator
{
	class Program
	{
		static void Main(string[] args)
		{
			Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
			Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

			List<ConfigurationElement> logFileElements = new List<ConfigurationElement>();

			using (ServerManager serverManager = new ServerManager())
			//using (ServerManager serverManager = ServerManager.OpenRemote("rovms502"))
			{
				// applicationHost.config
				Microsoft.Web.Administration.Configuration config = serverManager.GetApplicationHostConfiguration();

				#region system.applicationHost/log
				ConfigurationSection logSection = config.GetSection("system.applicationHost/log");

				// central log file ?
				bool isCentralLog;
				ConfigurationElement centralLogFile = null;
				ConfigurationAttribute centralLogFileModeAttr = logSection.GetAttribute("centralLogFileMode");
				switch ((int)centralLogFileModeAttr.Value)
				{
					case 0: isCentralLog = false; break;
					case 1: isCentralLog = true; centralLogFile = logSection.GetChildElement("centralBinaryLogFile"); break;
					case 2: isCentralLog = true; centralLogFile = logSection.GetChildElement("centralW3CLogFile"); break;
					default: throw new NotSupportedException("The value " + centralLogFileModeAttr.Value + " for system.applicationHost/log/@centralLogFileMode is not supported");
				}

				// server-wide HTTP log files encoding
				bool isUTF8 = (bool)logSection.GetAttributeValue("logInUTF8"); 
				#endregion

				#region system.ftpServer/log
				ConfigurationSection ftpServerLogSection = config.GetSection("system.ftpServer/log");

				// FTP central log file ?
				bool isFtpServerCentralLog;
				ConfigurationElement ftpServerCentralLogFile = null;
				ConfigurationAttribute ftpServerCentralLogFileModeAttr = ftpServerLogSection.GetAttribute("centralLogFileMode");
				switch ((int)ftpServerCentralLogFileModeAttr.Value)
				{
					case 0: isFtpServerCentralLog = false; break;
					case 1: isFtpServerCentralLog = true; ftpServerCentralLogFile = ftpServerLogSection.GetChildElement("centralLogFile"); break;
					default: throw new NotSupportedException("The value " + ftpServerCentralLogFileModeAttr.Value + " for system.ftpServer/log/@centralLogFileMode is not supported");
				}

				// server-wide FTP log files encoding
				bool isFtpServerUTF8 = (bool)ftpServerLogSection.GetAttributeValue("logInUTF8"); 
				#endregion

				if (isCentralLog)
				{
					// TODO central log file processing
					//throw new NotImplementedException();
					logFileElements.Add(centralLogFile);
				}

				if (isFtpServerCentralLog)
				{
					// TODO FTP central log file processing
					//throw new NotImplementedException();
					logFileElements.Add(ftpServerCentralLogFile);
				}

				if (!isCentralLog || !isFtpServerCentralLog)
				{
					// per-site log file processing
					ConfigurationSection sitesSection = config.GetSection("system.applicationHost/sites");
					ConfigurationElementCollection sitesCollection = sitesSection.GetCollection();

					foreach (ConfigurationElement site in sitesCollection)
					{
						ConfigurationElementCollection bindingsCollection = site.GetCollection("bindings");
						
						// any http/https binding ?
						bool isWebSite = bindingsCollection.Any(binding =>
						{
							String protocol = (String)binding.GetAttributeValue("protocol");
							return StringComparer.InvariantCultureIgnoreCase.Equals(protocol, "http") || StringComparer.InvariantCultureIgnoreCase.Equals(protocol, "https");
						});

						if (isWebSite && !isCentralLog)
						{
							ConfigurationElement logFile = site.GetChildElement("logFile");
							// TODO site log file processing
							logFileElements.Add(logFile);
						}

						// any ftp/ftps binding ?
						bool isFtpSite = bindingsCollection.Any(binding =>
						{
							String protocol = (String)binding.GetAttributeValue("protocol");
							return StringComparer.InvariantCultureIgnoreCase.Equals(protocol, "ftp") || StringComparer.InvariantCultureIgnoreCase.Equals(protocol, "ftps");
						});

						if (isFtpSite && !isFtpServerCentralLog)
						{
							ConfigurationElement ftpServer = site.GetChildElement("ftpServer");
							ConfigurationElement ftpServerLogFile = ftpServer.GetChildElement("logFile");
							// TODO FTP site log file processing
							logFileElements.Add(ftpServerLogFile);
						}
					}
				}

				/*
						 * 
						 * ConfigurationAttribute enabledAttr = logFile.GetAttribute("enabled");
						if (!String.IsNullOrEmpty(logFile.GetAttributeValue("customLogPluginClsid") as String))
							continue;
						

						// TODO handle other periods
						if (log.Period != LoggingRolloverPeriod.Daily)
							continue;

						RotationSettingsElement rotationSettings = RuntimeConfig.Rotation.GetSiteSettingsOrDefault(site.Id);

						bool isFtp = site.Bindings.Any(b => StringComparer.InvariantCultureIgnoreCase.Equals(b.Protocol, "ftp") || StringComparer.InvariantCultureIgnoreCase.Equals(b.Protocol, "ftps"));
						if (isFtp)
						{
						
						}

						bool isHttp = site.Bindings.Any(b => StringComparer.InvariantCultureIgnoreCase.Equals(b.Protocol, "http") || StringComparer.InvariantCultureIgnoreCase.Equals(b.Protocol, "https"));
						if (isHttp)
						{

						}*/
			}

			for (int index = 0, count = logFileElements.Count; index < count; index++)
			{
				ConfigurationElement logFileElement = logFileElements[index];

				Debug.WriteLine(
					"{0}. customLogPluginClsid={1}, directory={2}, enabled={3}, localTimeRollover={4}, logFormat={5}, period={6}, truncateSize={7}",
					(index + 1),
					logFileElement.GetAttributeValue2("customLogPluginClsid"),
					logFileElement.GetAttributeValue2("directory"),
					logFileElement.GetAttributeValue2("enabled"),
					logFileElement.GetAttributeValue2("localTimeRollover"),
					logFileElement.GetAttributeValue2("logFormat"),
					logFileElement.GetAttributeValue2("period"),
					logFileElement.GetAttributeValue2("truncateSize")
				);
			}
		}
	}
}
