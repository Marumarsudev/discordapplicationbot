using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace KirnuApplicationBot
{
    public class CommandStartApplication
    {
        private DiscordSocketClient _client;

        public CommandStartApplication(DiscordSocketClient client)
        {
            _client = client;

            if (Program.RecreateCommands)
                client.Ready += CreateSlashCommand;

            client.SlashCommandExecuted += CommandHandler;
        }

        private async Task CreateSlashCommand()
        {
            var guild = _client.GetGuild(Program.AppConfig.guildid);

            var option = new SlashCommandOptionBuilder()
                .WithName(Localizations.Localize("applicationtype"))
                .WithDescription(Localizations.Localize("applicationdesc"))
                .WithRequired(true)
                .WithType(ApplicationCommandOptionType.String);
            
            foreach (var kp in ApplicationHandler.Applications)
            {
                option
                    .AddChoice(kp.Value.applicationname, kp.Value.id);
            }

            var guildCommand = new SlashCommandBuilder()
                .WithName(Localizations.Localize("appstartname"))
                .WithDescription(Localizations.Localize("appstartdesc"))
                .AddOption(option);

            try
            {
                await guild.CreateApplicationCommandAsync(guildCommand.Build());
            }
            catch (HttpException e)
            {
                Console.WriteLine(JsonConvert.SerializeObject(e.Errors, Formatting.Indented));
            }
        }

        private async Task CommandHandler(SocketSlashCommand command)
        {
            // Console.WriteLine(command.Data.Options.First().Value);
            if (command.Data.Name != Localizations.Localize("appstartname"))
            {
                return;
            }

            var applicationId = command.Data.Options.First().Value.ToString();
            
            if (!Directory.Exists("./applications"))
            {
                Directory.CreateDirectory("./applications");
            }

            if (!File.Exists($"./applications/{command.User.Id}_{applicationId}.json"))
            {
                File.WriteAllText($"./applications/{command.User.Id}_{applicationId}.json", "{}");
            }

            await command.RespondAsync(Localizations.Localize("appstartresponse"));

            if (ApplicationHandler.OnGoingApplications.ContainsKey(command.User.Id))
            {
                
            }
            
            var startData = new CustomIDJson(applicationId, "start");
            var cancelData = new CustomIDJson(applicationId, "cancel");
            var embed = new EmbedBuilder()
                .WithTitle(ApplicationHandler.Applications[applicationId].applicationname)
                .WithColor(Color.Teal)
                .AddField(Localizations.Localize("startappmsg"), "wip");

            var componentBuilder = new ComponentBuilder()
                .WithButton(Localizations.Localize($"startappbtn"), JsonConvert.SerializeObject(startData),
                    ButtonStyle.Primary)
                .WithButton(Localizations.Localize($"cancelappbtn"), JsonConvert.SerializeObject(cancelData),
                    ButtonStyle.Danger);
            await command.User.SendMessageAsync(components: componentBuilder.Build(), embed: embed.Build());
        }
    }
}