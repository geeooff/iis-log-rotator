using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using IisLogRotator.Configuration;

namespace IisLogRotator
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

		public void WriteLineOut(string text, bool traceInfo = false, bool traceWarning = false, bool traceError = false)
		{
			Console.Out.WriteLine(text);

			if (_eventLogEnabled)
			{
				_eventMessageBuilder.AppendLine(text);
			}

			WriteTrace(traceInfo, traceWarning, traceError, text);
		}

		public void WriteLineOut(string format, Object arg0, bool traceInfo = false, bool traceWarning = false, bool traceError = false)
		{
			Console.Out.WriteLine(format, arg0);

			if (_eventLogEnabled)
			{
				_eventMessageBuilder.AppendFormat(format, arg0);
				_eventMessageBuilder.AppendLine();
			}

			WriteTrace(traceInfo, traceWarning, traceError, format, arg0);
		}

		public void WriteLineOut(string format, Object arg0, Object arg1, bool traceInfo = false, bool traceWarning = false, bool traceError = false)
		{
			Console.Out.WriteLine(format, arg0, arg1);

			if (_eventLogEnabled)
			{
				_eventMessageBuilder.AppendFormat(format, arg0, arg1);
				_eventMessageBuilder.AppendLine();
			}

			WriteTrace(traceInfo, traceWarning, traceError, format, arg0, arg1);
		}

		public void WriteLineOut(string format, Object arg0, Object arg1, Object arg2, bool traceInfo = false, bool traceWarning = false, bool traceError = false)
		{
			Console.Out.WriteLine(format, arg0, arg1, arg2);

			if (_eventLogEnabled)
			{
				_eventMessageBuilder.AppendFormat(format, arg0, arg1, arg2);
				_eventMessageBuilder.AppendLine();
			}

			WriteTrace(traceInfo, traceWarning, traceError, format, arg0, arg1, arg2);
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

		public void WriteLineError(string text, bool traceInfo = false, bool traceWarning = false, bool traceError = false)
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

			WriteTrace(traceInfo, traceWarning, traceError, text);
		}

		public void WriteLineError(string format, Object arg0, bool traceInfo = false, bool traceWarning = false, bool traceError = false)
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

			WriteTrace(traceInfo, traceWarning, traceError, format, arg0);
		}

		public void WriteLineError(string format, Object arg0, Object arg1, bool traceInfo = false, bool traceWarning = false, bool traceError = false)
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

			WriteTrace(traceInfo, traceWarning, traceError, format, arg0, arg1);
		}

		public void WriteLineError(string format, Object arg0, Object arg1, Object arg2, bool traceInfo = false, bool traceWarning = false, bool traceError = false)
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

			WriteTrace(traceInfo, traceWarning, traceError, format, arg0, arg1, arg2);
		}

		private void WriteTrace(bool traceInformation, bool traceWarning, bool traceError, string text)
		{
			if (traceInformation) Trace.TraceInformation(text);
			if (traceWarning) Trace.TraceWarning(text);
			if (traceError) Trace.TraceError(text);
		}

		private void WriteTrace(bool traceInformation, bool traceWarning, bool traceError, string format, params Object[] args)
		{
			if (traceInformation) Trace.TraceInformation(format, args);
			if (traceWarning) Trace.TraceWarning(format, args);
			if (traceError) Trace.TraceError(format, args);
		}
	}
}
