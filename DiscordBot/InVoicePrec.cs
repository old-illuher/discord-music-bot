using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace DiscordBot
{
    public class InVoicePrec : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            return Task.FromResult((context.User as IGuildUser)?.VoiceChannel is null
                ? PreconditionResult.FromError("You must be in a voice channel")
                : PreconditionResult.FromSuccess());
        }
    }
}
