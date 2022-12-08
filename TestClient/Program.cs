using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

using AberrantSMPP;
using AberrantSMPP.Packet;
using AberrantSMPP.Packet.Request;
using AberrantSMPP.Packet.Response;
using AberrantSMPP.Exceptions;

namespace TestClient
{
	class Program
	{
		#region InnerTypes
		enum RunType
		{
			Interactive,
			Single,
			Multi,
			Legacy
		}
		#endregion

		static ConcurrentBag<string> SentMessages = new ConcurrentBag<string>();

		private static readonly global::Common.Logging.ILog _log = null;
		private static readonly Stopwatch _sw;

		static Program()
		{
			SetupLogging();
			_log = global::Common.Logging.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
			_sw = System.Diagnostics.Stopwatch.StartNew();
		}

		private static void SetupLogging()
		{
			NLog.LogManager.Configuration = new NLog.Config.XmlLoggingConfiguration(
				Path.Combine(
					Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
					"NLog.config"
				)
			);

			Common.Logging.LogManager.Adapter =
				new Common.Logging.NLog45.NLogLoggerFactoryAdapter(
					new Common.Logging.Configuration.NameValueCollection()
					{
						{ "configType", "EXTERNAL" }
					});
		}

		private static void Log(string text, bool logToFile = true)
		{
			if (logToFile)
				_log.Debug(text);
			else
				Console.WriteLine(text);
		}

		static T GetArg<T>(string[] args, int index, T defaultValue, Func<string, object> converter = null) where T : IConvertible
		{
			converter = converter ?? new Func<string, object>((string arg) => Convert.ChangeType(arg, typeof(T)));
			try
			{
				return args.Length <= index ? defaultValue : (T)converter(args[index]);
			}
			catch
			{
				return defaultValue;
			}
		}

		static void Main(string[] args)
		{
			var action = GetArg(args, index: 0, defaultValue: RunType.Interactive, (arg) => Enum.Parse(typeof(RunType), arg, true));

			int numberOfClients = GetArg(args, index: 1, defaultValue: 1);
			int requestPerClient = GetArg(args, index: 2, defaultValue: 100);

			switch (action)
			{
				case RunType.Interactive:
					new SMPPClientInteractive().Run(numberOfClients: 1, requestPerClient: 0);
					break;
				case RunType.Single:
					new SMPPClientMultiTaskPerClientTest().Run(numberOfClients: numberOfClients, requestPerClient: requestPerClient);
					break;
				case RunType.Multi:
					new SMPPClientSingleTaskPerClientTest().Run(numberOfClients: numberOfClients, requestPerClient: requestPerClient);
					break;
				case RunType.Legacy:
					new SMPPCommunicatorTest().Run(numberOfClients: numberOfClients, requestPerClient: requestPerClient);
					break;
				default:
					break;
			}
		}
	}
}
