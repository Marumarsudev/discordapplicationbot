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
    public class CommandStaffHandleApplication
    {
        private DiscordSocketClient _client;

        public CommandStaffHandleApplication(DiscordSocketClient client)
        {
            _client = client;

            if (Program.RecreateCommands)
                client.Ready += CreateSlashCommand;

            client.SlashCommandExecuted += CommandHandler;
        }

        private async Task CreateSlashCommand()
        {
            var guild = _client.GetGuild(Program.AppConfig.guildid);

            var verdict = new SlashCommandOptionBuilder()
                .WithName(Localizations.Localize("staffhandleappverdict"))
                .WithDescription(Localizations.Localize("staffhandleappdesc"))
                .WithRequired(true)
                .WithType(ApplicationCommandOptionType.String)
                .AddChoice(Localizations.Localize("approve"), "approve")
                .AddChoice(Localizations.Localize("deny"), "deny");

            var userSelect = new SlashCommandOptionBuilder()
                .WithName(Localizations.Localize("user"))
                .WithDescription(Localizations.Localize("userdesc"))
                .WithRequired(true)
                .WithType(ApplicationCommandOptionType.User);

            var roleSelect = new SlashCommandOptionBuilder()
                .WithName(Localizations.Localize("role"))
                .WithDescription(Localizations.Localize("roledesc"))
                .WithType(ApplicationCommandOptionType.Role);

            var guildCommand = new SlashCommandBuilder()
                .WithName(Localizations.Localize("staffhandleappname"))
                .WithDescription(Localizations.Localize("staffhandleappdesc"))
                .AddOption(verdict)
                .AddOption(userSelect)
                .AddOption(roleSelect);

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
            if (command.Data.Name != Localizations.Localize("staffhandleappname"))
            {
                return;
            }

            if (!_client.GetGuild(Program.AppConfig.guildid).GetUser(command.User.Id).Roles.Any(role =>
                    role.Name == Localizations.Localize("adminrolename")))
            {
                await command.RespondAsync(Localizations.Localize("apphandlenopermission"), ephemeral: true);
                return;
            }

            var choice = command.Data.Options.Where(c =>
                c.Name == Localizations.Localize("staffhandleappverdict"));

            var user = command.Data.Options.Where(c =>
                c.Name == Localizations.Localize("user"));

            var role = command.Data.Options.Where(c =>
                c.Name == Localizations.Localize("role"));

            if ((string)choice.First().Value == "approve")
            {
                var socketRole = (IRole)role.First().Value;
                var socketUser = (SocketGuildUser) user.First().Value;
                await socketUser.AddRoleAsync(socketRole.Id);
                await command.RespondAsync(Localizations.Localize("applicationapproved"));
                await socketUser.SendMessageAsync(Localizations.Localize("applicationapprovedmsg"));
            }
            else
            {
                var socketUser = (SocketGuildUser) user.First().Value;
                await command.RespondAsync(Localizations.Localize("applicationdenied"));
                await socketUser.SendMessageAsync(Localizations.Localize("applicationdeniedmsg"));
            }
        }
    }
}