#region usings

using System;
using System.Threading.Tasks;
using Convex.Event;
using Convex.Example.Event;
using Convex.IRC;
using Convex.IRC.ComponentModel.Event;
using Convex.IRC.ComponentModel.Reference;
using Convex.Plugin.Registrar;
using Serilog;
using Serilog.Events;

#endregion

namespace Convex.Example {
    public class IrcBot : IDisposable {
        #region MEMBERS

        private string BotInfo => $"[Version {_bot.Version}] Evealyn is an IRC bot created by Antonio DiNostri as a primary learning project for C#.";

        public bool IsInitialised { get; private set; }
        public bool Executing => _bot.Server.Executing;
        private readonly Client _bot;
        private readonly string[] _defaultChannels = {"#testgrounds"};

        #endregion

        /// <summary>
        ///     Initialises class
        /// </summary>
        public IrcBot() {
            _bot = new Client("irc.foonetic.net", 6667);
            _bot.Initialised += (sender, args) => OnLog(sender, new AdvancedLoggingEventArgs(LogEventLevel.Information, "Client initialized."));
            _bot.Logged += (sender, args) => OnLog(sender, new AdvancedLoggingEventArgs(LogEventLevel.Information, args.Information));
            _bot.Server.Connection.Flushed += (sender, args) => OnLog(sender, new AdvancedLoggingEventArgs(LogEventLevel.Information, $" >> {args.Information}"));
            _bot.Server.ChannelMessaged += LogChannelMessage;

            Log.Logger = new LoggerConfiguration().WriteTo.RollingFile(_bot.ClientConfiguration.LogFilePath).WriteTo.LiterateConsole().CreateLogger();
        }

        #region INIT

        public async Task Initialise() {
            await _bot.Initialise();
            RegisterMethods();

            IsInitialised = true;
        }

        #endregion


        #region RUNTIME

        public async Task Execute() {
            await _bot.BeginListenAsync();
        }

        #endregion

        #region EVENTS

        private static Task OnLog(object sender, AdvancedLoggingEventArgs args) {
            switch (args.Level) {
                case LogEventLevel.Verbose:
                    Log.Verbose(args.Information);
                    break;
                case LogEventLevel.Debug:
                    Log.Debug(args.Information);
                    break;
                case LogEventLevel.Information:
                    Log.Information(args.Information);
                    break;
                case LogEventLevel.Warning:
                    Log.Warning(args.Information);
                    break;
                case LogEventLevel.Error:
                    Log.Error(args.Information);
                    break;
                case LogEventLevel.Fatal:
                    Log.Fatal(args.Information);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return Task.CompletedTask;
        }

        private Task LogChannelMessage(object source, ServerMessagedEventArgs e) {
            if (e.Message.Command.Equals(Commands.PRIVMSG))
                OnLog(this, new AdvancedLoggingEventArgs(LogEventLevel.Information, $"<{e.Message.Origin} {e.Message.Nickname}> {e.Message.Args}"));
            else if (e.Message.Command.Equals(Commands.ERROR))
                OnLog(this, new AdvancedLoggingEventArgs(LogEventLevel.Error, e.Message.RawMessage));
            else
                OnLog(this, new AdvancedLoggingEventArgs(LogEventLevel.Information, e.Message.RawMessage));

            return Task.CompletedTask;
        }

        #endregion

        #region REGISTRARS

        /// <summary>
        ///     Register all methods
        /// </summary>
        private void RegisterMethods() {
            _bot.RegisterMethod(new MethodRegistrar<ServerMessagedEventArgs>(Info, e => e.Message.InputCommand.Equals(nameof(Info).ToLower()), Commands.PRIVMSG, new Tuple<string, string>(nameof(Info), "returns the basic information about this bot")));
        }

        private async Task Info(ServerMessagedEventArgs e) {
            if (e.Message.SplitArgs.Count < 2 || !e.Message.SplitArgs[1].Equals("info"))
                return;

            await _bot.Server.Connection.SendDataAsync(this, new IrcCommandRecievedEventArgs(Commands.PRIVMSG, $"{e.Message.Origin} {BotInfo}"));
        }

        #endregion

        #region DISPOSE

        private void Dispose(bool disposing) {
            if (disposing)
                _bot?.Dispose();
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~IrcBot() {
            Dispose(false);
        }

        #endregion
    }
}
