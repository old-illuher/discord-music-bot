using System;
using System.Collections.Generic;
using System.Text;
using Discord.WebSocket;
using System.Threading.Tasks;
using Discord.Commands;

namespace DiscordBot
{
    public class InfoModule:ModuleBase<SocketCommandContext>
    {
		// !commands -> list
		[Command("commands")]
		[Summary("Returns list of commands")]
		public Task SayAsync()
			=> ReplyAsync("List of commands:\n" +
				"play song - plays a song from youtube\n" +
				"skip - skips current track\n" +
				"queue - shows current queue\n" +
				"pause - pauses player\n" +
				"resume - resumes player");

		// ReplyAsync is a method on ModuleBase 
	}
}
