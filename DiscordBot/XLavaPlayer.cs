﻿using Discord;
using Victoria;

namespace DiscordBot
{
    public class XLavaPlayer : LavaPlayer
    {
        public string ChannelName { get; }
        public XLavaPlayer(LavaSocket lavaSocket,IVoiceChannel voiceChannel, ITextChannel textChannel) : base(lavaSocket, voiceChannel, textChannel)
        {
            ChannelName = textChannel.Name;
        }
    }
}
