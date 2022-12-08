namespace Common.Logging.MicrosoftLogging
{
	using System;
	using System.Collections.Concurrent;

	using Microsoft.Extensions.Logging;

	internal class CommonLoggingProvider : ILoggerProvider
	{
		private bool _disposed;

		private readonly ConcurrentDictionary<string, CommonLoggingLogger> _loggers =
			new ConcurrentDictionary<string, CommonLoggingLogger>();

		public CommonLoggingProvider()
		{
		}

		public ILogger CreateLogger(string categoryName)
		{
			return _loggers.GetOrAdd(categoryName, CreateLoggerImplementation);
		}

		private CommonLoggingLogger CreateLoggerImplementation(string name)
		{
			var options = new CommonLoggingProviderOptions
			{
				Name = name,
			};
			var logger = new CommonLoggingLogger(options);

			return logger;
		}

		/// <summary>
		/// Finalizes the instance of the <see cref="CommonLoggingProvider"/> object.
		/// </summary>
		~CommonLoggingProvider()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Releases unmanaged and - optionally - managed resources.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
		protected virtual void Dispose(bool disposing)
		{
			if (!_disposed)
			{
				if (disposing)
				{
					_loggers.Clear();
				}

				_disposed = true;
			}
		}
	}
}