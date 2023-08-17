using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace KirnuApplicationBot
{
    class Program
    {
        public static AppConfig AppConfig;
        public static bool RecreateCommands;
        
        private DiscordSocketClient _client;
        private TestCommand _testCommand;
        private SayMyName _sayMyName;
        public static Task Main(string[] args) => new Program().MainAsync(args);

        public async Task MainAsync(string[] args)
        {
            RecreateCommands = args.Contains("--recreatecommands");
            #region bot_startup
            using (StreamReader r = new StreamReader("./config.json"))
            {
                AppConfig = JsonConvert.DeserializeObject<AppConfig>(r.ReadToEnd());
            }

            _client = new DiscordSocketClient();
            _client.Log += Log;

            await _client.LoginAsync(TokenType.Bot, AppConfig.token);
            await _client.StartAsync();
            #endregion
        
            #region commands

            if (Program.RecreateCommands)
            {
                _client.Ready += async () =>
                {
                    var guild = _client.GetGuild(Program.AppConfig.guildid);
                    List<ApplicationCommandProperties> applicationCommandProperties = new List<ApplicationCommandProperties>();
                    await guild.BulkOverwriteApplicationCommandAsync(applicationCommandProperties.ToArray());
                };
            }
            
            _testCommand = new TestCommand(_client);
            _sayMyName = new SayMyName(_client);
            #endregion
            await Task.Delay(-1);
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}