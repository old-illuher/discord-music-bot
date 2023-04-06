using System;
using System.Reflection;
using Discord;
using Discord.Commands;
using System.Threading.Tasks;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Discord.Addons.Interactive;
using Victoria;

namespace DiscordBot
{
    public class CommandHandler
    {
        private readonly DiscordSocketClient _client;
        private AudioService _audioService;
        private  CommandService _commands;
        private  IServiceProvider _services;

        // Retrieve client and CommandService instance via ctor
        public CommandHandler(DiscordSocketClient client)
        {
            _client = client;
        }

        public async Task InitializeAsync()
        {
            _commands = new CommandService(new CommandServiceConfig
            {
                LogLevel = LogSeverity.Verbose
            });

            LavaConfig lavaConfig = new LavaConfig();
            LavaNode <XLavaPlayer> lavaNode = new LavaNode<XLavaPlayer>(_client, lavaConfig);
            _audioService = new AudioService(lavaNode);
            //ILoggerFactory loggerFactory = new LoggerFactory
            IServiceCollection services = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_commands)
                .AddSingleton<InteractiveService>()
                .AddSingleton(lavaConfig)
                .AddSingleton(lavaNode)
                .AddSingleton(_audioService);
            _services = services.BuildServiceProvider();
            _services.GetService<LavaConfig>().LogSeverity = LogSeverity.Verbose;
            _client.Ready += onReadyAsync;
            _client.Log += Log;
            await InstallCommandsAsync();
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        public async Task onReadyAsync()
        {
            // Avoid calling ConnectAsync again if it's already connected 
            // (It throws InvalidOperationException if it's already connected).
            if (!_services.GetService<LavaNode<XLavaPlayer>>().IsConnected)
            {                
                try
                {
                    await _services.GetService<LavaNode<XLavaPlayer>>().ConnectAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            // Other ready related stuff
        }

        public async Task InstallCommandsAsync()
        {
            // Hook the MessageReceived event into our command handler
            _client.MessageReceived += HandleCommandAsync;

            // Here we discover all of the command modules in the entry 
            // assembly and load them. Starting from Discord.NET 2.0, a
            // service provider is required to be passed into the
            // module registration method to inject the 
            // required dependencies.
            //
            // If you do not use Dependency Injection, pass null.
            // See Dependency Injection guide for more information.
            await _commands.AddModulesAsync(assembly: Assembly.GetEntryAssembly(),
                                            services: _services);
        }

        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            // Don't process the command if it was a system message
            var message = messageParam as SocketUserMessage;
            if (message == null) return;

            // Create a number to track where the prefix ends and the command begins
            int argPos = 0;

            // Determine if the message is a command based on the prefix and make sure no bots trigger commands
            if (!(message.HasCharPrefix('!', ref argPos) ||
                message.HasMentionPrefix(_client.CurrentUser, ref argPos)) ||
                message.Author.IsBot)
                return;

            // Create a WebSocket-based command context based on the message
            var context = new SocketCommandContext(_client, message);

            // Execute the command with the command context we just
            // created, along with the service provider for precondition checks.
            var result = await _commands.ExecuteAsync(
                context: context,
                argPos: argPos,
                services: _services);
            if (!result.IsSuccess)
            {
                Console.WriteLine(result.ErrorReason + $" at {context.Guild.Name}");
                switch (result.Error)
                {
                    case CommandError.UnknownCommand:
                        {
                            var guildEmote = Emote.Parse("<:unknowscmd:461157571701506049>");
                            await messageParam.AddReactionAsync(guildEmote);
                            break;
                        }
                    case CommandError.BadArgCount:
                        {
                            await context.Channel.SendMessageAsync(
                                "You are suppose to pass in a parameter with this" +
                                " command. type `help [command name]` for help");
                            break;
                        }
                    case CommandError.UnmetPrecondition:
                        {
                            await context.Channel.SendMessageAsync(
                                "You can not use this command at the moment.\nReason: " +
                                result.ErrorReason);
                            break;
                        }
                    default:
                        {
                            await context.Channel.SendMessageAsync(result.Error.ToString());
                            break;
                        }
                }
            }
        }
    }
}
