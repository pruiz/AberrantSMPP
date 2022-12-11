namespace Common.Logging.MicrosoftLogging
{
	using System;

	using Microsoft.Extensions.Logging;

	internal class CommonLoggingLogger : ILogger
	{
		private CommonLoggingProviderOptions _options;

		private ILog _log;

		public CommonLoggingLogger(CommonLoggingProviderOptions options)
		{
			_options = options ?? throw new ArgumentNullException(nameof(options));
			_log = LogManager.GetLogger(options.Name ?? string.Empty);
		}

		public IDisposable BeginScope<TState>(TState state) => default;

		public bool IsEnabled(LogLevel logLevel)
		{
			switch (logLevel)
			{
				case LogLevel.Critical:
					return _log.IsFatalEnabled;
				case LogLevel.Trace:
					return _log.IsTraceEnabled;
				case LogLevel.Debug:
					return _log.IsDebugEnabled;
				case LogLevel.Error:
					return _log.IsErrorEnabled;
				case LogLevel.Information:
					return _log.IsInfoEnabled;
				case LogLevel.Warning:
					return _log.IsWarnEnabled;
				case LogLevel.None:
					return false;
				default:
					throw new ArgumentOutOfRangeException(nameof(logLevel));
			}
		}

		public void Log<TState>(
			LogLevel logLevel,
			EventId eventId,
			TState state,
			Exception exception,
			Func<TState, Exception, string> formatter)
		{
			if (!IsEnabled(logLevel))
			{
				return;
			}

			if (formatter == null)
			{
				throw new ArgumentNullException(nameof(formatter));
			}

			var message = formatter(state, exception);

			switch (logLevel)
			{
				case LogLevel.Critical:
					_log.Fatal(message, exception);
					break;
				case LogLevel.Debug:
					_log.Debug(message, exception);
					break;
				case LogLevel.Error:
					_log.Error(message, exception);
					break;
				case LogLevel.Information:
					_log.Info(message, exception);
					break;
				case LogLevel.Warning:
					_log.Warn(message, exception);
					break;
				case LogLevel.Trace:
					_log.Trace(message, exception);
					break;
				default:
					break;
			}
		}
	}
}