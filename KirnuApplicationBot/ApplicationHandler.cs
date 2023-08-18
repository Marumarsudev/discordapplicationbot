using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace KirnuApplicationBot
{
    public class ApplicationQuestion
    {
        public string question;
    }
    
    public class Application
    {
        public string id;
        public string applicationname;
        public ApplicationQuestion[] questions;
    }

    public class OnGoingApplication
    {
        public string applicationId;
        public string[] answers;
        public int currentQuestionId;

        public OnGoingApplication(string id, int questionCount)
        {
            applicationId = id;
            answers = new string[questionCount];
            currentQuestionId = 0;
        }
    }
    
    public class ApplicationHandler
    {
        public static Dictionary<string, Application> Applications;
        public static Dictionary<ulong, OnGoingApplication> OnGoingApplications;

        private DiscordSocketClient _client;

        public ApplicationHandler(DiscordSocketClient client)
        {
            _client = client;
            LoadApplications();

            OnGoingApplications = new Dictionary<ulong, OnGoingApplication>();
            
            _client.ButtonExecuted += ButtonHandler;
            _client.ModalSubmitted += ModalHandler;
        }

        private void LoadApplications()
        {
            using (StreamReader r = new StreamReader("./applications.json"))
            {
                Applications = JsonConvert.DeserializeObject<Dictionary<string, Application>>(r.ReadToEnd());
            }
        }

        private async Task ButtonHandler(SocketMessageComponent component)
        {
            var customIdJson = JsonConvert.DeserializeObject<CustomIDJson>(component.Data.CustomId);
            if (!Applications.ContainsKey(customIdJson.id))
            {
                return;
            }

            OnGoingApplications.TryAdd(component.User.Id, new OnGoingApplication(customIdJson.id, Applications[customIdJson.id].questions.Length));
            var currentApplication = OnGoingApplications[component.User.Id];

            switch (customIdJson.action)
            {
                case "next":
                    currentApplication.currentQuestionId += 1;
                    currentApplication.currentQuestionId =
                        currentApplication.currentQuestionId >= Applications[currentApplication.applicationId].questions.Length
                            ? Applications[currentApplication.applicationId].questions.Length - 1
                            : currentApplication.currentQuestionId;
                    
                    await component.Message.DeleteAsync();
                    await SendUpdatedEmbedMessage(component, currentApplication, customIdJson);
                    return;
                case "previous":
                    currentApplication.currentQuestionId -= 1;
                    currentApplication.currentQuestionId =
                        currentApplication.currentQuestionId < 0 ? 0 : currentApplication.currentQuestionId;
                    
                    await component.Message.DeleteAsync();
                    await SendUpdatedEmbedMessage(component, currentApplication, customIdJson);
                    return;
                case "send":
                    await component.Message.DeleteAsync();
                    await component.RespondAsync("Kiitos hakemuksesta, ylläpito tarkastaa hakemuksen tuota pikaa! :smile:");
                    await SendApplication(component, currentApplication, customIdJson);
                    return;
                case "cancel":
                    return;
                case "start":
                    await component.Message.DeleteAsync();
                    await SendUpdatedEmbedMessage(component, currentApplication, customIdJson);
                    return;
                case "answer":
                    var modal = new ModalBuilder()
                        .WithTitle(Applications[customIdJson.id].applicationname)
                        .WithCustomId(JsonConvert.SerializeObject(new CustomIDJson(customIdJson.id, "modal")))
                        .AddTextInput(Localizations.Localize("answer"),
                            new CustomIDJson(customIdJson.id, "modalsubmit").GetJsonString(), TextInputStyle.Paragraph,
                            "");
                    await component.RespondWithModalAsync(modal.Build());
                    return;
                default:
                    break;
            }
        }

        private async Task ModalHandler(SocketModal modal)
        {
            await modal.Message.DeleteAsync();
            var currentApplication = OnGoingApplications[modal.User.Id];
            var customIdJson = JsonConvert.DeserializeObject<CustomIDJson>(modal.Data.CustomId);

            if (!Applications.ContainsKey(customIdJson.id))
            {
                return;
            }

            currentApplication.answers[currentApplication.currentQuestionId] = modal.Data.Components.First().Value;

            await SendUpdatedEmbedMessage(modal, currentApplication, customIdJson);
        }

        private async Task SendUpdatedEmbedMessage(SocketInteraction interaction, OnGoingApplication currentApplication, CustomIDJson customIdJson)
        {
            var componentBuilder = new ComponentBuilder();

            if (currentApplication.currentQuestionId > 0)
            {
                componentBuilder.WithButton(Localizations.Localize($"previous"),
                    JsonConvert.SerializeObject(new CustomIDJson(customIdJson.id, "previous")),
                    ButtonStyle.Secondary); //, disabled: string.IsNullOrEmpty(currentApplication.answers[currentApplication.currentQuestionId])
            }
            if (currentApplication.currentQuestionId < Applications[currentApplication.applicationId].questions.Length - 1)
            {
                componentBuilder.WithButton(Localizations.Localize($"next"),
                    JsonConvert.SerializeObject(new CustomIDJson(customIdJson.id, "next")),
                    ButtonStyle.Secondary);
            }
            componentBuilder.WithButton(Localizations.Localize($"answerquestionbtn"),
                JsonConvert.SerializeObject(new CustomIDJson(customIdJson.id, "answer")),
                ButtonStyle.Primary);
            componentBuilder.WithButton(Localizations.Localize($"cancel"),
                JsonConvert.SerializeObject(new CustomIDJson(customIdJson.id, "cancel")),
                ButtonStyle.Danger);
            componentBuilder.WithButton(Localizations.Localize($"send"),
                JsonConvert.SerializeObject(new CustomIDJson(customIdJson.id, "send")),
                ButtonStyle.Success); //, disabled: currentApplication.answers.Count == Applications[currentApplication.applicationId].questions.Length

            string answer = "*Vastaus*";
            if (currentApplication.answers[currentApplication.currentQuestionId] != null)
            {
                answer = currentApplication.answers[currentApplication.currentQuestionId];
            }
            
            var embed = new EmbedBuilder()
                .WithTitle(Applications[customIdJson.id].applicationname)
                .WithColor(Color.Teal)
                .AddField(Applications[customIdJson.id].questions[currentApplication.currentQuestionId].question, answer);

            await interaction.RespondAsync(components: componentBuilder.Build(), embed: embed.Build());
        }

        private async Task SendApplication(SocketInteraction interaction, OnGoingApplication currentApplication, CustomIDJson customIdJson)
        {
            var channel =
                _client.GetGuild(Program.AppConfig.guildid).GetChannel(Program.AppConfig.appchannelid) as ITextChannel;

            var thread = await channel.CreateThreadAsync($"{Localizations.Localize("users")} {interaction.User.Username}",
                ThreadType.PublicThread, ThreadArchiveDuration.ThreeDays);
            
            for (var i = 0; i < Applications[currentApplication.applicationId].questions.Length; i++)
            {
                await thread.SendMessageAsync($"{Applications[currentApplication.applicationId].questions[i].question}\n{OnGoingApplications[interaction.User.Id].answers[i]}");
            }
        }
    }
}