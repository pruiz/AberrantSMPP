using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Text;

using AberrantSMPP;

namespace TestClient
{
	internal class SMPPClientInteractiveTest : SMPPClientBatchedTests
	{
		#region Inner Types
		private class Command
		{
			private readonly Func<string> _text;

			public Action Action { get; }
			public string Text => _text();

			public Command(Action action, Func<string> text)
			{
				Action = action;
				_text = text;
			}

			public Command(Action action, string text) : this(action, () => text) { }
		}
		#endregion

		private readonly Dictionary<ConsoleKey, Command> _commands = new Dictionary<ConsoleKey, Command>();
		private bool _mustQuit;
		private bool _logToFile;
		private bool _enableTls;
		private int _id = 0;
		private readonly string _spacer = new string('-', 80);

		private SMPPClient Client => _clients.FirstOrDefault().Value;

		public SMPPClientInteractiveTest()
			: base(typeof(SMPPClientInteractiveTest), startClientOnCreate: false)
		{
			SetupCommands();
		}

		private void SetupCommands()
		{
			_commands.Add(ConsoleKey.X, new Command(() => _mustQuit = true, "quit"));
			_commands.Add(ConsoleKey.L, new Command(() => _logToFile = !_logToFile, () => $"toggle Log To File. (LogToFile {(_logToFile ? "enabled" : "disabled")})"));
			_commands.Add(ConsoleKey.T, new Command(() => ToggleTls(), () => $"toggle TLS . (TLS: {(_enableTls ? "enabled" : "disabled")})"));
			_commands.Add(ConsoleKey.D1, new Command(() => Client.Start(), "start"));
			_commands.Add(ConsoleKey.D0, new Command(() => Client.Stop(), "stop"));
			_commands.Add(ConsoleKey.C, new Command(() => Client.Connect(), "connect"));
			_commands.Add(ConsoleKey.D, new Command(() => Client.Disconnect(), "disconnect"));
			_commands.Add(ConsoleKey.B, new Command(() => Client.Bind(), "bind"));
			_commands.Add(ConsoleKey.U, new Command(() => Client.Unbind(), "unbind"));
			_commands.Add(ConsoleKey.S, new Command(() => CreateAndSendSubmitSm(Client, ++_id), "send short message"));
		}

		private static string GetKeyName(ConsoleKey key)
		{
			if (key >= ConsoleKey.D0 && key <= ConsoleKey.D9)
				return key.ToString().Substring(1);
			if (key >= ConsoleKey.NumPad0 && key <= ConsoleKey.NumPad9)
				return "Numpad " + key.ToString().Substring(6);
			return key.ToString();
		}

		private void ToggleTls()
		{
			_enableTls = !_enableTls;
			RecreateClients(_clients.Count);
		}

		private ConsoleKey PrintMenuAndAskCommand()
		{
			StringBuilder sb = new StringBuilder(string.Empty, 512);
			sb.AppendLine("");
			sb.AppendLine(_spacer);
			sb.Append("Client->State:").AppendLine(Client?.State.ToString());
			sb.AppendLine(_spacer);
			foreach (var commandKvp in _commands)
			{
				sb.Append(" ").Append(GetKeyName(commandKvp.Key)).Append(" => ").Append(commandKvp.Value.Text).AppendLine(".");
			}
			sb.AppendLine(_spacer);
			_log.Debug(sb.ToString());
			var key = Console.ReadKey().Key;
			_log.Debug(Environment.NewLine + Environment.NewLine + _spacer);
			return key;
		}

		protected override ISmppClient CreateClient(string name)
		{
			return _enableTls
				? new SMPPClient("smppsims.smsdaemon.test", 15004, SslProtocols.Default | SslProtocols.Tls11 | SslProtocols.Tls12 | SslProtocols.Ssl2)
				: base.CreateClient(name);
		}

		protected override void Configure(SMPPClient client)
		{
			base.Configure(client);

			client.ReconnectIntervals = new[] { TimeSpan.FromSeconds(5) };
			client.DisconnectTimeout = TimeSpan.FromSeconds(2);

			//XXX: if interactive debug set ResponseTimeout to a big time (2 minutes)
			//client.ResponseTimeout = TimeSpan.FromMinutes(2);

			//client.EnquireLinkInterval = TimeSpan.FromSeconds(5);
		}

		protected override void Execute(int workers, int requests)
		{
			while (!_mustQuit)
			{
				ConsoleKey key = PrintMenuAndAskCommand();
				if (!_commands.TryGetValue(key, out var command))
				{
					_log.Debug($"Key {key} not assigned to any command." + Environment.NewLine);
					continue;
				}

				try
				{
					command.Action();
				}
				catch (Exception ex)
				{
					_log.Debug("==> Action Failed!!");
					_log.Debug(ex.ToString());
				}
			}
		}
	}
}
