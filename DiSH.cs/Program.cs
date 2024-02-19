using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using DiSH;
using dotenv.net;

#nullable disable

namespace DiSH.cs
{


    internal class Program
    {
        public static string log(string message)
        {
            info(message);
            return "OK";
        }
        public static void info(string value)
        {
            Console.WriteLine("[i] " + value);
        }
        public static void error(string value)
        {
            Console.WriteLine("[!] " + value);
        }
        public static void warn(string value)
        {
            Console.WriteLine("[-] " + value);
        }
        public static void question(string value)
        {
            Console.WriteLine("[?] " + value);
        }
        
        public static void Main(string[] args)
        {
            DotEnv.Load();
            Console.WriteLine(Environment.GetEnvironmentVariable("GUILD_ID"));
            foreach (DictionaryEntry de in Environment.GetEnvironmentVariables())
            { Console.WriteLine("  {0} = {1}", de.Key, de.Value); }
         
            string token = Environment.GetEnvironmentVariable("TOKEN");
            ulong guildId = (ulong)Decimal.Parse(Environment.GetEnvironmentVariable("GUILD_ID"));
            ulong categoryId = (ulong)Decimal.Parse(Environment.GetEnvironmentVariable("CATEGORY_ID"));
            ulong logsId = (ulong)Decimal.Parse(Environment.GetEnvironmentVariable("LOGS_ID"));
            DiSH dish = new(token, guildId, categoryId, logsId, log) ;
            Task Run() => dish.RunBot();
            Run().Wait();
        }
    }
}
