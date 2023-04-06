using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using System;
using Discord.Net;
using Victoria;
using Victoria.Enums;
using Victoria.EventArgs;

namespace DiscordBot
{
    public sealed class AudioService
    {
        private readonly LavaNode<XLavaPlayer> _lavaNode;
        public bool repeatable = false;
        //private readonly ILogger _logger;
        public readonly HashSet<ulong> VoteQueue;
        private readonly ConcurrentDictionary<ulong, CancellationTokenSource> _disconnectTokens;

        public AudioService(LavaNode<XLavaPlayer> lavaNode)
        {
            _lavaNode = lavaNode;
            //_logger = loggerFactory.CreateLogger<LavaNode>();
            _disconnectTokens = new ConcurrentDictionary<ulong, CancellationTokenSource>();

            //_lavaNode.OnLog += arg =>
            //{
            //    _logger.Log(LogLevel.Information, arg.Exception, arg.Message);
            //    return Task.CompletedTask;
            //};

            _lavaNode.OnPlayerUpdated += OnPlayerUpdated;
            _lavaNode.OnStatsReceived += OnStatsReceived;
            _lavaNode.OnTrackEnded += OnTrackEnded;
            _lavaNode.OnTrackStarted += OnTrackStarted;
            _lavaNode.OnTrackException += OnTrackException;
            _lavaNode.OnTrackStuck += OnTrackStuck;
            _lavaNode.OnWebSocketClosed += OnWebSocketClosed;

            VoteQueue = new HashSet<ulong>();
        }

        private Task OnPlayerUpdated(PlayerUpdateEventArgs arg)
        {
            //_logger.LogInformation($"Track update received for {arg.Track.Title}: {arg.Position}");
            return Task.CompletedTask;
        }

        private Task OnStatsReceived(StatsEventArgs arg)
        {
            //_logger.LogInformation($"Lavalink has been up for {arg.Uptime}.");
            return Task.CompletedTask;
        }

        private async Task OnTrackStarted(TrackStartEventArgs arg)
        {
            await arg.Player.TextChannel.SendMessageAsync($"Now playing: {arg.Track.Title}");
            if (!_disconnectTokens.TryGetValue(arg.Player.VoiceChannel.Id, out var value))
            {
                return;
            }

            if (value.IsCancellationRequested)
            {
                return;
            }

            value.Cancel(true);
            await arg.Player.TextChannel.SendMessageAsync("Auto dc cancelled");
        }

        private async Task OnTrackEnded(TrackEndedEventArgs arg)
        {
            if (arg.Reason != TrackEndReason.Finished)
            {
                return;
            }

            await arg.Player.TextChannel.SendMessageAsync($"{arg.Reason}: {arg.Track.Title}");

            var player = arg.Player;
            if (repeatable)
            {
                player.Queue.Enqueue(arg.Track);
            }

            //Console.WriteLine("queue:");
            //foreach (var track in player.Queue)
            //{
            //    Console.WriteLine(track.Title + "\t" + track.Duration + "\t" + track.Position);
            //}

            if (!player.Queue.TryDequeue(out var lavaTrack))
            {
                    await player.TextChannel.SendMessageAsync("queue completed");
                    _ = InitiateDisconnectAsync(arg.Player, TimeSpan.FromSeconds(10));
                    return;
            }

            if (lavaTrack is null)
            {
                await player.TextChannel.SendMessageAsync("not a track");
                return;
            }

            await arg.Player.PlayAsync(lavaTrack);
        }
        private async Task InitiateDisconnectAsync(LavaPlayer player, TimeSpan timeSpan)
        {
            if (!_disconnectTokens.TryGetValue(player.VoiceChannel.Id, out var value))
            {
                value = new CancellationTokenSource();
                _disconnectTokens.TryAdd(player.VoiceChannel.Id, value);
            }
            else if (value.IsCancellationRequested)
            {
                _disconnectTokens.TryUpdate(player.VoiceChannel.Id, new CancellationTokenSource(), value);
                value = _disconnectTokens[player.VoiceChannel.Id];
            }

            await player.TextChannel.SendMessageAsync($"auto dc in {timeSpan}");
            var isCancelled = SpinWait.SpinUntil(() => value.IsCancellationRequested, timeSpan);
            if (isCancelled)
            {
                return;
            }

            await _lavaNode.LeaveAsync(player.VoiceChannel);
        }

        private async Task OnTrackException(TrackExceptionEventArgs arg)
        {
            //_logger.LogError($"{arg.Track.Title} threw an exception");
            arg.Player.Queue.Enqueue(arg.Track);
            await arg.Player.TextChannel.SendMessageAsync($"{arg.Track.Title} readded to queue after exception");
        }

        private async Task OnTrackStuck(TrackStuckEventArgs arg)
        {
            //_logger.LogError(
            //    $"Track {arg.Track.Title} got stuck for {arg.Threshold}ms. Please check Lavalink console/logs.");
            arg.Player.Queue.Enqueue(arg.Track);
            await arg.Player.TextChannel.SendMessageAsync(
                $"{arg.Track.Title} has been re-added to queue after getting stuck.");
        }

        private Task OnWebSocketClosed(WebSocketClosedEventArgs arg)
        {
            //_logger.LogCritical($"Discord WebSocket connection closed with following reason: {arg.Reason}");
            return Task.CompletedTask;
        }
    }
}
