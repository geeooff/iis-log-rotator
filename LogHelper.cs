using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using Smartgeek.LogRotator.Configuration;

namespace Smartgeek.LogRotator
{
	public class LogHelper : IDisposable
	{
		private bool _disposed;
		private readonly bool _eventLogEnabled;
		private readonly EventLog _eventLog;
		private EventLogEntryType _eventLogEntryType;
		private readonly StringBuilder _eventMessageBuilder;

		public LogHelper()
		{
			if (_eventLogEnabled = RuntimeConfig.Rotation.EnableEventLog)
			{
				_eventLog = new EventLog(Installer.DefaultEventLog);
				_eventLog.Source = Installer.DefaultEventSource;
				_eventLogEntryType = EventLogEntryType.Information;
				_eventMessageBuilder = new StringBuilder();
			}
		}

		~LogHelper()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			if (!_disposed)
			{
				if (disposing)
				{
					if (_eventLogEnabled)
					{
						FlushToEventLog();
						_eventLog.Close();
						_eventLog.Dispose();
					}
				}
				_disposed = true;
			}
		}

		public void FlushToEventLog()
		{
			if (_eventLogEnabled)
			{
				_eventLog.WriteEntry(_eventMessageBuilder.ToString(), _eventLogEntryType);
				_eventMessageBuilder.Clear();
				_eventLogEntryType = EventLogEntryType.Information;
			}
		}

		public void WriteLineOut()
		{
			Console.Out.WriteLine();

			if (_eventLogEnabled)
			{
				_eventMessageBuilder.AppendLine();
			}
		}

		public void WriteLineOut(String text)
		{
			Console.Out.WriteLine(text);

			if (_eventLogEnabled)
			{
				_eventMessageBuilder.AppendLine(text);
			}
		}

		public void WriteLineOut(String format, Object arg0)
		{
			Console.Out.WriteLine(format, arg0);

			if (_eventLogEnabled)
			{
				_eventMessageBuilder.AppendFormat(format, arg0);
				_eventMessageBuilder.AppendLine();
			}
		}

		public void WriteLineOut(String format, Object arg0, Object arg1)
		{
			Console.Out.WriteLine(format, arg0, arg1);

			if (_eventLogEnabled)
			{
				_eventMessageBuilder.AppendFormat(format, arg0, arg1);
				_eventMessageBuilder.AppendLine();
			}
		}

		public void WriteLineOut(String format, Object arg0, Object arg1, Object arg2)
		{
			Console.Out.WriteLine(format, arg0, arg1, arg2);

			if (_eventLogEnabled)
			{
				_eventMessageBuilder.AppendFormat(format, arg0, arg1, arg2);
				_eventMessageBuilder.AppendLine();
			}
		}

		public void WriteLineOut(String format, params Object[] args)
		{
			Console.Out.WriteLine(format, args);

			if (_eventLogEnabled)
			{
				_eventMessageBuilder.AppendFormat(format, args);
				_eventMessageBuilder.AppendLine();
			}
		}

		public void WriteLineError()
		{
			Console.Error.WriteLine();

			if (_eventLogEnabled)
			{
				if (_eventLogEntryType != EventLogEntryType.Error)
				{
					_eventLogEntryType = EventLogEntryType.Error;
				}
				_eventMessageBuilder.AppendLine();
			}
		}

		public void WriteLineError(String text)
		{
			Console.Error.WriteLine(text);

			if (_eventLogEnabled)
			{
				if (_eventLogEntryType != EventLogEntryType.Error)
				{
					_eventLogEntryType = EventLogEntryType.Error;
				}
				_eventMessageBuilder.AppendLine(text);
			}
		}

		public void WriteLineError(String format, Object arg0)
		{
			Console.Error.WriteLine(format, arg0);

			if (_eventLogEnabled)
			{
				if (_eventLogEntryType != EventLogEntryType.Error)
				{
					_eventLogEntryType = EventLogEntryType.Error;
				}
				_eventMessageBuilder.AppendFormat(format, arg0);
				_eventMessageBuilder.AppendLine();
			}
		}

		public void WriteLineError(String format, Object arg0, Object arg1)
		{
			Console.Error.WriteLine(format, arg0, arg1);

			if (_eventLogEnabled)
			{
				if (_eventLogEntryType != EventLogEntryType.Error)
				{
					_eventLogEntryType = EventLogEntryType.Error;
				}
				_eventMessageBuilder.AppendFormat(format, arg0, arg1);
				_eventMessageBuilder.AppendLine();
			}
		}

		public void WriteLineError(String format, Object arg0, Object arg1, Object arg2)
		{
			Console.Error.WriteLine(format, arg0, arg1, arg2);

			if (_eventLogEnabled)
			{
				if (_eventLogEntryType != EventLogEntryType.Error)
				{
					_eventLogEntryType = EventLogEntryType.Error;
				}
				_eventMessageBuilder.AppendFormat(format, arg0, arg1, arg2);
				_eventMessageBuilder.AppendLine();
			}
		}

		public void WriteLineError(String format, params Object[] args)
		{
			Console.Error.WriteLine(format, args);

			if (_eventLogEnabled)
			{
				if (_eventLogEntryType != EventLogEntryType.Error)
				{
					_eventLogEntryType = EventLogEntryType.Error;
				}
				_eventMessageBuilder.AppendFormat(format, args);
				_eventMessageBuilder.AppendLine();
			}
		}
	}
}
