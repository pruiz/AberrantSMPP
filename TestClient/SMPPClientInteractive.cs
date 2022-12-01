using AberrantSMPP;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestClient
{
    internal class SMPPClientInteractive : SMPPClientTestsBase
    {
        private Dictionary<ConsoleKey, Command> _commands = new Dictionary<ConsoleKey, Command>();
        private bool _mustQuit;
        private bool _logToFile;

        public SMPPClientInteractive() : base(typeof(SMPPClientInteractive))
        {
            _commands.Add(ConsoleKey.Q, new Command(() => _mustQuit = true, "quit"));
            _commands.Add(ConsoleKey.L, new Command(() => _logToFile = true, () => $"toggle Log To File. (LogToFile {(_logToFile ? "enabled" : "disabled")})" ));
            _commands.Add(ConsoleKey.D1, new Command(() => Client.Start(TimeSpan.FromSeconds(30)), "start"));
            _commands.Add(ConsoleKey.D0, new Command(() => Client.Stop(), "stop"));
            _commands.Add(ConsoleKey.C, new Command(() => Client.Connect(), "connect"));
            _commands.Add(ConsoleKey.D, new Command(() => Client.Disconnect(), "disconnect"));
            _commands.Add(ConsoleKey.B, new Command(() => Client.Bind(), "bind"));
            _commands.Add(ConsoleKey.U, new Command(() => Client.Unbind(), "unbind"));
            _commands.Add(ConsoleKey.S, new Command(() => CreateAndSendSubmitSm(1, 1, Client, 1), "send short message"));

            StartOnBuildClient = false;
        }

        protected override void PrintResume(int numberOfClients, int requestPerClient)
        {
            Client.ResponseTimeout = TimeSpan.FromMinutes(2);
            Client.EnquireLinkInterval = TimeSpan.FromSeconds(5);

            while (!_mustQuit)
            {
                ConsoleKey key = PrintMenuAndAskCommand();
                if (!_commands.TryGetValue(key, out var command))
                {
                    Console.WriteLine($"Key {key} not assigned to any command." + Environment.NewLine);
                    continue;
                }
                
                try
                {
                    command.Action();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
                
            }
        }

        protected override void Execute(int numberOfClients, int requestPerClient) =>
            throw new NotImplementedException();

        private ConsoleKey PrintMenuAndAskCommand()
        {
            CLog(new string('-', 80));
            CLog($"Client->State:{Client.State}");
            CLog(new string('-', 80));
            foreach( var commandKvp in _commands)
            {
                CLog($" {GetKeyName(commandKvp.Key)} => {commandKvp.Value.Text}.");
            }

            var key = Console.ReadKey().Key;
            CLog(new string('-', 80) + Environment.NewLine);
            return key;
        }

        private static string GetKeyName(ConsoleKey key)
        {
            if (key >= ConsoleKey.D0 && key <= ConsoleKey.D9)
                return key.ToString().Substring(1);
            if (key >= ConsoleKey.NumPad0 && key <= ConsoleKey.NumPad9)
                return "Numpad " + key.ToString().Substring(6);
            return key.ToString();
        }

        private SMPPClient Client => _clients.FirstOrDefault().Value;

        private void Log(string text, bool logToFile = true)
        {
            if (_logToFile)
                _log.Debug(text);
            else
                CLog(text);
        }
        private void CLog(string message)
        {
            Console.WriteLine(message);
        }

        public void SendShortMessage()
        {
            CreateAndSendSubmitSm(1, 1, Client, 1);
        }

        private class Command
        {
            private readonly Func<string> _text;

            public Action Action { get; }
            public string Text => _text();

            public Command(Action action, Func<string> text)
            {
                Action = action; _text = text;
            }

            public Command(Action action, string text) : this(action, () => text) { }
        }
    }
}
