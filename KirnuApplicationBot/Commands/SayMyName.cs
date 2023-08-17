using System;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace KirnuApplicationBot
{
    public class SayMyName
    {
        private string _commandName = "saymyname";
        private DiscordSocketClient _client;

        public SayMyName(DiscordSocketClient client)
        {
            _client = client;

            if (Program.RecreateCommands)
                client.Ready += CreateSlashCommand;

            client.SlashCommandExecuted += CommandHandler;
        }

        private async Task CreateSlashCommand()
        {
            var guild = _client.GetGuild(Program.AppConfig.guildid);

            var guildCommand = new SlashCommandBuilder()
                .WithName(_commandName)
                .WithDescription("Bot says your name.");

            try
            {
                await guild.CreateApplicationCommandAsync(guildCommand.Build());
            }
            catch (ApplicationCommandException e)
            {
                Console.WriteLine(JsonConvert.SerializeObject(e.Errors, Formatting.Indented));
            }
        }

        private async Task CommandHandler(SocketSlashCommand command)
        {
            if (command.Data.Name != _commandName)
            {
                return;
            }
            await command.RespondAsync($"{command.User.Username}");
        }
    }
}