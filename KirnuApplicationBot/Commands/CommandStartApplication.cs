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
                    .AddChoice(kp.Value.applicationname, kp.Key);
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

            if (command.Data.Name != Localizations.Localize("appstartname"))
            {
                return;
            }

            var applicationId = command.Data.Options.First().Value.ToString();

            var hasRequiredRole = _client.GetGuild(Program.AppConfig.guildid).GetUser(command.User.Id).Roles.Any(role =>
            {
                if (ApplicationHandler.Applications[applicationId].requiredroles.Length == 0)
                {
                    return true;
                }
                return ApplicationHandler.Applications[applicationId].requiredroles.Contains(role.Name);
            });

            var hasBlacklistedRole = _client.GetGuild(Program.AppConfig.guildid).GetUser(command.User.Id).Roles.Any(role =>
            {
                return ApplicationHandler.Applications[applicationId].blacklistedroles.Contains(role.Name);
            });

            var hasWhitelistedRole = _client.GetGuild(Program.AppConfig.guildid).GetUser(command.User.Id).Roles.Any(role =>
            {
                return ApplicationHandler.Applications[applicationId].whitelistedroles.Contains(role.Name);
            });

            if (!hasWhitelistedRole)
            {
                if (hasBlacklistedRole)
                {
                    await command.RespondAsync(Localizations.Localize("hasblacklistedrole"));
                    return;
                }

                if (!hasRequiredRole)
                {
                    string roles = "";
                    ApplicationHandler.Applications[applicationId].requiredroles.ToList().ForEach(role =>
                    {
                        roles += $"{role} ";
                    });
                    await command.RespondAsync($"{Localizations.Localize("missingroles")} {roles}");
                    return;
                }
            }

            await command.RespondAsync(Localizations.Localize("appstartresponse"));

            // var dmchannel= await command.User.CreateDMChannelAsync();
            // var msgs = dmchannel.GetMessagesAsync(100, CacheMode.AllowDownload);
            // msgs.ForEachAsync(msg =>
            // {
            //     foreach (IMessage message in msg)
            //     {
            //         message.DeleteAsync();
            //     }
            // });

            var startData = new CustomIDJson(applicationId, "start");
            var cancelData = new CustomIDJson(applicationId, "cancel");
            var embed = new EmbedBuilder()
                .WithTitle(ApplicationHandler.Applications[applicationId].applicationname)
                .WithColor(Color.Teal)
                .AddField(Localizations.Localize("startappmsg"), ":rock:");

            var componentBuilder = new ComponentBuilder()
                .WithButton(Localizations.Localize($"startappbtn"), JsonConvert.SerializeObject(startData),
                    ButtonStyle.Primary)
                .WithButton(Localizations.Localize($"cancelappbtn"), JsonConvert.SerializeObject(cancelData),
                    ButtonStyle.Danger);
            var msg= await command.User.SendMessageAsync(components: componentBuilder.Build(), embed: embed.Build());
        }
    }
}