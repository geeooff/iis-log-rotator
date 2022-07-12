using IisLogRotator.Configuration;
using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using TS = Microsoft.Win32.TaskScheduler;

namespace IisLogRotator
{
    [RunInstaller(true)]
    public class Installer : System.Configuration.Install.Installer
    {
        public const string DefaultTaskName = "IIS Logs Rotation";
        public const string DefaultTaskDescription = "Rotate IIS logs based on each website logging configuration";
        public const string DefaultEventLog = "IIS Log Rotation";
        public const string DefaultEventSource = "LogRotator";

        private EventLogInstaller eventLogInstaller;
        private bool previousEnableEventLog;

        public override void Install(IDictionary stateSaver)
        {
#if DEBUG
			if (!Debugger.IsAttached)
			{
				Debugger.Launch();
			}
			Debugger.Break();
#endif

            base.Install(stateSaver);

            previousEnableEventLog = InstallerConfig.EnableEventLog;

            // enable windows event logs
            if (!InstallerConfig.EnableEventLog)
            {
                InstallerConfig.EnableEventLog = true;
                InstallerConfig.Save();

                Trace.TraceInformation("Windows event logs for this application are now enabled.");
                this.Context.LogMessage("Information: Windows event logs for this application are now enabled.");
                this.Context.LogMessage("Information: See the /configuration/rotation/@enableEventLog attribute in the XML configuration file to disable.");
            }
            else
            {
                Trace.TraceInformation("Windows event logs for this application were already enabled.");
                this.Context.LogMessage("Information: Windows event logs for this application were already enabled.");
            }

            InstallTask(stateSaver);
        }

        public override void Rollback(IDictionary savedState)
        {
            base.Rollback(savedState);

            // restore previous event log settings
            if (InstallerConfig.EnableEventLog != previousEnableEventLog)
            {
                InstallerConfig.EnableEventLog = previousEnableEventLog;
                InstallerConfig.Save();

                Trace.TraceInformation("Windows event logs settings for this application has been rollbacked");
                this.Context.LogMessage("Information: Windows event logs settings for this application has been rollbacked");
            }
            else
            {
                Trace.TraceInformation("Windows event logs settings for this application don't need to be rollbacked");
                this.Context.LogMessage("Information: Windows event logs settings for this application don't need to be rollbacked");
            }
        }

        public override void Uninstall(IDictionary savedState)
        {
#if DEBUG
			if (!Debugger.IsAttached)
			{
				Debugger.Launch();
			}
			Debugger.Break();
#endif

            base.Uninstall(savedState);

            try
            {
                UninstallTask(savedState);
            }
            catch (FileNotFoundException)
            {
                this.Context.LogMessage("Warning: Unable to find the scheduled task");
                Trace.TraceWarning("Unable to find the scheduled task");
            }

            // disable windows event logs
            if (InstallerConfig.EnableEventLog)
            {
                InstallerConfig.EnableEventLog = false;
                InstallerConfig.Save();

                Trace.TraceInformation("Windows event logs for this application are now disabled");
                this.Context.LogMessage("Information: Windows event logs for this application are now disabled");
            }
            else
            {
                Trace.TraceInformation("Windows event logs for this application were already disabled.");
                this.Context.LogMessage("Information: Windows event logs for this application were already disabled.");
            }
        }

        private void InstallTask(IDictionary stateSaver)
        {
            FileInfo exeFileInfo = new FileInfo(this.GetType().Assembly.Location);

            // get the install directory when used inside a setup project, or current assembly directory
            string targetDir = this.Context.Parameters["TARGETDIR"] ?? exeFileInfo.DirectoryName;
            FileInfo targetExeFileInfo = new FileInfo(Path.Combine(targetDir, exeFileInfo.Name));

            string taskName;
            bool isNewGen;

            // init. task scheduler service engine
            using (TS.TaskService ts = new TS.TaskService())
            {
                // check if scheduler engine is V2
                isNewGen = (ts.HighestSupportedVersion >= new Version(1, 2));

                TS.TaskDefinition td = ts.NewTask();
                td.RegistrationInfo.Description = DefaultTaskDescription;

                td.Actions.Add(
                    new TS.ExecAction(targetExeFileInfo.FullName)
                );

                // triggers every day, one hour after midnight UTC
                TS.DailyTrigger trigger = new TS.DailyTrigger();
                trigger.StartBoundary = new DateTime(1982, 4, 15, 1, 0, 0, DateTimeKind.Utc);
                td.Triggers.Add(trigger);

                if (isNewGen)
                {
                    td.Settings.Priority = ProcessPriorityClass.BelowNormal;
                }

                td.Settings.ExecutionTimeLimit = TimeSpan.FromDays(1d);
                td.Settings.DisallowStartIfOnBatteries = false;
                td.Settings.StopIfGoingOnBatteries = false;

                // the task needs to be explicitly enabled by user
                td.Settings.Enabled = false;

                TS.Task task = ts.RootFolder.RegisterTaskDefinition(
                    DefaultTaskName,
                    td,
                    TS.TaskCreation.CreateOrUpdate,
                    isNewGen ? "SYSTEM" : null,
                    logonType: TS.TaskLogonType.ServiceAccount
                );
                taskName = task.Name;
            }

            // remembers the registered task name for future uninstall
            stateSaver["ScheduledTaskName"] = taskName;

            Trace.TraceInformation("Scheduled task \"{0}\" has been created", taskName);
            this.Context.LogMessage("Information: Scheduled task \"" + taskName + "\" has been created");
            this.Context.LogMessage("Information: The scheduled task is DISABLED by default !");

            // TODO don't advise to use schtasks.exe for older windows (which older ones ?)
            this.Context.LogMessage("Information: Execute this command line to enable: SCHTASKS /Change /TN \"" + taskName + "\" /ENABLE");
        }

        private void UninstallTask(IDictionary savedState)
        {
            string taskName = (savedState != null) ? (string)savedState["ScheduledTaskName"] ?? DefaultTaskName : DefaultTaskName;

            using (TS.TaskService ts = new TS.TaskService())
            {
                ts.RootFolder.DeleteTask(taskName);
            }

            Trace.TraceInformation("Scheduled task \"{0}\" has been deleted", taskName);
            this.Context.LogMessage("Information: Scheduled task \"" + taskName + "\" has been deleted");
        }

        private void InitializeComponent()
        {
            this.eventLogInstaller = new System.Diagnostics.EventLogInstaller();
            // 
            // eventLogInstaller
            // 
            this.eventLogInstaller.CategoryCount = 0;
            this.eventLogInstaller.CategoryResourceFile = null;
            this.eventLogInstaller.Log = DefaultEventLog;
            this.eventLogInstaller.MessageResourceFile = null;
            this.eventLogInstaller.ParameterResourceFile = null;
            this.eventLogInstaller.Source = DefaultEventSource;
            // 
            // Installer
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.eventLogInstaller});

        }
    }
}
