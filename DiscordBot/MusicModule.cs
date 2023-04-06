using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using Victoria;
using Victoria.Enums;
using Victoria.EventArgs;
using Victoria.Responses.Search;

namespace DiscordBot
{
    [InVoicePrec]
    public class MusicModule : ModuleBase<SocketCommandContext>
    {
        private readonly LavaNode<XLavaPlayer> _lavaNode;
        private readonly AudioService _audioService;
        private static readonly IEnumerable<int> Range = Enumerable.Range(1900, 2000);
        public MusicModule(LavaNode<XLavaPlayer> lavaNode, AudioService audioService)
        {
            _lavaNode = lavaNode;
            //_lavaNode.OnTrackEnded += OnTrackEnded;
            _audioService = audioService;
        }

        [Command("play", RunMode = RunMode.Async)]
        [Summary("Plays a song")]
        public async Task PlayTask([Remainder] string searchQuery)
        {
            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                await ReplyAsync("Please provide search terms.");
                return;
            }

            var player = await _lavaNode.JoinAsync((Context.User as IVoiceState)?.VoiceChannel, Context.Channel as ITextChannel);

            if (!_lavaNode.HasPlayer(Context.Guild))
            {
                await ReplyAsync("I'm not connected to a voice channel.");
                return;
            }

            var query = searchQuery;
            var searchResponse = await _lavaNode.SearchAsync(SearchType.YouTube, query);
            if (searchResponse.Status == SearchStatus.LoadFailed ||
                searchResponse.Status == SearchStatus.NoMatches)
            {
                await ReplyAsync($"I wasn't able to find anything for `{query}`.");
                return;
            }

            if (player.PlayerState == PlayerState.Playing || player.PlayerState == PlayerState.Paused)
            {
                if (!string.IsNullOrWhiteSpace(searchResponse.Playlist.Name))
                {
                    player.Queue.Enqueue(searchResponse.Tracks.ElementAt<LavaTrack>(0));
                    await ReplyAsync($"Enqueued {searchResponse.Tracks.ElementAt<LavaTrack>(0).Title}");
                }
                else
                {
                    var track = searchResponse.Tracks.ElementAt<LavaTrack>(0);
                    player.Queue.Enqueue(track);
                    await ReplyAsync($"Enqueued: {track.Title}");
                }
            }
            else
            {
                var track = searchResponse.Tracks.ElementAt<LavaTrack>(0);

                await player.PlayAsync(x => { x.Track = track; x.ShouldPause = false; });
                //await ReplyAsync($"Now Playing: {track.Title}");
            }
        }
        /*public async Task PlayTask([Remainder] string query)
        {
            LavaPlayer player = null;
            var final = await ReplyAsync("Searching");
            try
            {
                player = await _lavaNode.JoinAsync((Context.User as IVoiceState)?.VoiceChannel);
            }
            catch (Exception ex)
            {
                await ReplyAsync(ex.Message);
            }
            await ReplyAsync($"Joined {(Context.User as IVoiceState)?.VoiceChannel.Name}");
            var response = await _lavaNode.SearchYouTubeAsync(query);

            if (response.Tracks.Count != 0)
            {
                Context.Guild.Id.PushTrack(response.Tracks.ElementAt<LavaTrack>(0));
                var lavalinkTrack = Context.Guild.Id.PopTrack();
                if (player.PlayerState != PlayerState.Playing)
                {
                    await player.PlayAsync(lavalinkTrack);
                    await final.ModifyAsync(x =>
                    {
                        x.Embed = new EmbedBuilder
                        {
                            Description =
                            $"{lavalinkTrack.Title} \nPlaying {lavalinkTrack.Title} now",
                            Color = new Color(213, 0, 249),
                            Title = "Now playing"
                        }.Build();
                        x.Content = null;
                    });
                }
                else
                {
                    Context.Guild.Id.PushTrack(response.Tracks.ElementAt<LavaTrack>(0));
                    await final.ModifyAsync(x =>
                    {
                        x.Embed = null;
                        x.Content = $"Added {lavalinkTrack.Title} to the queue.";
                    });
                }
            }
        }*/

        [Command("repeat", RunMode = RunMode.Async)]
        [Summary("loops queue")]
        public async Task RepeatTask()
        {
            Console.WriteLine(_audioService.repeatable);
            if (_audioService.repeatable)
            {
                _audioService.repeatable = false;
                Console.WriteLine("repeat off");
            }
            else
            {
                _audioService.repeatable = true;
                Console.WriteLine("repeat on");
            }
            Console.WriteLine(_audioService.repeatable);
            await ReplyAsync(_audioService.repeatable ? "Repeating queue" : "Repeating disabled");

        }

        [Command("queue", RunMode = RunMode.Async)]
        [Summary("Prints queue")]
        public Task QueueTask()
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                return ReplyAsync("not connected");
            }
            return ReplyAsync(player.PlayerState != PlayerState.Playing ? "not playing anything" : string.Join(Environment.NewLine, player.Queue.Select(x => x.Title)));
        }

        [Command("skip", RunMode = RunMode.Async)]
        [Summary("Skips current track")]
        public async Task SkipTask()
        {
            var player = _lavaNode.GetPlayer(Context.Guild) ??
                await _lavaNode.JoinAsync((Context.User as IVoiceState)?.VoiceChannel);
            if (_audioService.repeatable)
                player.Queue.Enqueue(player.Track);
            await player.StopAsync();
            if (!player.Queue.TryDequeue(out var queueable))
            {
                return;
            }

            if (!(queueable is LavaTrack track))
            {
                return;
            }
            await player.PlayAsync(queueable);
        }

        [Command("pause", RunMode = RunMode.Async)]
        [Summary("Pauses the track")]
        public async Task PauseTask()
        {
            var player = _lavaNode.GetPlayer(Context.Guild) ??
                         await _lavaNode.JoinAsync((Context.User as IVoiceState)?.VoiceChannel);
            await player.PauseAsync();
            await ReplyAsync("Paused");
        }

        [Command("resume", RunMode = RunMode.Async)]
        [Alias("unpause")]
        [Summary("Resumes the track")]
        public async Task ResumeTask()
        {
            var player = _lavaNode.GetPlayer(Context.Guild) ??
                         await _lavaNode.JoinAsync((Context.User as IVoiceState)?.VoiceChannel);
            if (player.PlayerState == PlayerState.Playing)
            {
                await ReplyAsync("Already playing " + player.Track.Title);
            }
            else
            {
                await ReplyAsync($"Resumed {player.Track.Title}");
                await player.ResumeAsync();
            }
        }

        [Command("stop", RunMode = RunMode.Async)]
        [Summary("Stops player and clears playlist")]
        public async Task StopTask()
        {
            var player = _lavaNode.GetPlayer(Context.Guild) ??
                await _lavaNode.JoinAsync((Context.User as IVoiceState)?.VoiceChannel);
            await player.StopAsync();
            player.Queue.Clear();
            await ReplyAsync("Playback stopped");
        }
    }
}
