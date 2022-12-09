using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Text;

using AberrantSMPP;

using TestClient.Facilities;

namespace TestClient
{
	internal class InteractiveTest : BatchedTests
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

		private readonly Dictionary<ConsoleKey, Command> _commands = new();
		private bool _mustQuit;
		private bool _enableTls;
		private int _id = 0;
		private readonly string _spacer = new('-', 80);

		private ISmppClientAdapter Client => _clients.FirstOrDefault().Value;

		public InteractiveTest(ISmppClientFactory clientFactory)
			: base(typeof(InteractiveTest), false, clientFactory)
		{
			SetupCommands();
		}

		private void SetupCommands()
		{
			_commands.Add(ConsoleKey.Q, new Command(() => _mustQuit = true, "quit"));
			_commands.Add(ConsoleKey.X, new Command(() => _mustQuit = true, "quit"));
			_commands.Add(ConsoleKey.T, new Command(() => ToggleTls(), () => $"toggle TLS . (TLS: {(_enableTls ? "enabled" : "disabled")})"));
			_commands.Add(ConsoleKey.D1, new Command(() => Start(), "start"));
			_commands.Add(ConsoleKey.D0, new Command(() => Stop(), "stop"));
			_commands.Add(ConsoleKey.C, new Command(() => Connect(), "connect"));
			_commands.Add(ConsoleKey.D, new Command(() => Disconnect(), "disconnect"));
			_commands.Add(ConsoleKey.B, new Command(() => Bind(), "bind"));
			_commands.Add(ConsoleKey.U, new Command(() => Unbind(), "unbind"));
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

		private void Start() => Client.Start();
		private void Stop() => Client.Stop();
		private void Connect()
		{
			if (Client is not IHasConnectDisconnect client)
				throw new NotImplementedException($"{Client?.GetType()} does not implement Connect().");
			client.Connect();
		}
		private void Disconnect()
		{
			if (Client is not IHasConnectDisconnect client)
				throw new NotImplementedException($"{Client?.GetType()} does not implement Disconnect().");
			client.Disconnect();
		}

		private void Bind()
		{
			if (Client is not IHasBindUnbind client)
				throw new NotImplementedException($"{Client?.GetType()} does not implement Bind().");
			client.Bind();
		}

		private void Unbind()
		{
			if (Client is not IHasBindUnbind client)
				throw new NotImplementedException($"{Client?.GetType()} does not implement Unbind().");
			client.Unbind();
		}

		private ConsoleKey PrintMenuAndAskCommand()
		{
			StringBuilder sb = new StringBuilder(string.Empty, 512);
			sb.AppendLine("");
			sb.AppendLine(_spacer);
			sb.Append("Client->State:").AppendLine(Client?.Status.ToString());
			sb.AppendLine(_spacer);
			foreach (var commandKvp in _commands)
			{
				sb.Append(" ").Append(GetKeyName(commandKvp.Key)).Append(" => ").Append(commandKvp.Value.Text).AppendLine(".");
			}
			sb.AppendLine(_spacer);
			_log.Info(sb.ToString());
			var key = Console.ReadKey().Key;
			_log.Info(Environment.NewLine + Environment.NewLine + _spacer);
			return key;
		}

		protected override ISmppClient CreateClient(string name)
		{
			return _enableTls
				? new SMPPClient("smppsims.smsdaemon.test", 15004, SslProtocols.Default | SslProtocols.Tls11 | SslProtocols.Tls12 | SslProtocols.Ssl2)
				: base.CreateClient(name);
		}

		protected override void Configure(ISmppClientAdapter client)
		{
			base.Configure(client);

			if (client is SMPPClient smppClient)
			{
				smppClient.ReconnectIntervals = new[] { TimeSpan.FromSeconds(5) };
				smppClient.DisconnectTimeout = TimeSpan.FromSeconds(2);

				//XXX: if interactive debug set ResponseTimeout to a big time (2 minutes)
				//smppClient.ResponseTimeout = TimeSpan.FromMinutes(2);
			}

			client.EnquireLinkInterval = TimeSpan.Zero;
		}

		protected override void Execute(int workers, int requests)
		{
			while (!_mustQuit)
			{
				ConsoleKey key = PrintMenuAndAskCommand();
				if (!_commands.TryGetValue(key, out var command))
				{
					_log.Info($"Key {key} not assigned to any command." + Environment.NewLine);
					continue;
				}

				try
				{
					command.Action();
				}
				catch (Exception ex)
				{
					_log.Error("==> Action Failed!!");
					_log.Error(ex.ToString());
				}
			}
		}
	}
}
