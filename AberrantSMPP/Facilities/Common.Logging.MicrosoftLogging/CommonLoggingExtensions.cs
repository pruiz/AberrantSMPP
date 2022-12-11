namespace Common.Logging.MicrosoftLogging
{
	using Microsoft.Extensions.Logging;
	using Microsoft.Extensions.DependencyInjection;
	using Microsoft.Extensions.Options;

	internal static class CommonLoggingExtensions
	{
		/// <summary>
		/// Adds the common.logging.
		/// </summary>
		/// <param name="factory">The factory.</param>
		/// <returns>The <see cref="ILoggerFactory"/> with added common.logging provider</returns>
		public static ILoggerFactory AddCommonLogging(this ILoggerFactory factory)
		{
			factory.AddProvider(new CommonLoggingProvider());
			return factory;
		}

#if !NETCOREAPP1_1
		/// <summary>
		/// Adds the common.logging logging provider.
		/// </summary>
		/// <param name="builder">The logging builder instance.</param>
		/// <returns>The <see ref="ILoggingBuilder" /> passed as parameter with the new provider registered.</returns>
		public static ILoggingBuilder AddCommonLogging(this ILoggingBuilder builder)
		{
			builder.Services.AddSingleton<ILoggerProvider>(new CommonLoggingProvider());

			return builder;
		}
#endif
	}
}