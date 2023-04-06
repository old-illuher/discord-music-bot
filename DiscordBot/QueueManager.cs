using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Victoria;

namespace DiscordBot
{
    public static class QueueManager
    {
        private static readonly Dictionary<ulong, Queue<LavaTrack>> Queue =
            new Dictionary<ulong, Queue<LavaTrack>>();

        public static string PushTrack(this ulong guildId, LavaTrack track)
        {
            Queue.TryAdd(guildId, new Queue<LavaTrack>());
            Queue[guildId].Enqueue(track);
            return "Added to queue";
        }

        public static LavaTrack PopTrack(this ulong guildId)
        {
            Queue.TryAdd(guildId, new Queue<LavaTrack>());
            if (!Queue[guildId].Any())
                throw new InvalidOperationException("Queue empty");
            return Queue[guildId].Dequeue();
        }

        public static void PopAll(this ulong guildId)
        {
            Queue.TryAdd(guildId, new Queue<LavaTrack>());
            Queue[guildId].Clear();
        }

        public static List<LavaTrack> Playlist(this ulong guildId)
        {
            Queue.TryAdd(guildId, new Queue<LavaTrack>());
            return Queue[guildId].ToList();
        }
    }
}
