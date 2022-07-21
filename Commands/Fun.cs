using Discord;
using System.Threading.Tasks;
using Levante.Configs;
using Discord.Interactions;
using System;
using System.Collections.Generic;

namespace Levante.Commands
{
    public class Fun : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("ratio", "Get ratioed.")]
        public async Task Ratio()
        {
            string[] ratioMsgs =
            {
                "Counter",
                "L",
                "You fell off",
                "Skill issue",
                "GG",
                "Ice cold",
                "Bozo",
                "Ok and?",
                "Who asked?",
                "💀 x7",
                "Cope",
            };

            var random = new Random();
            int numOfMsgs = random.Next(1, 4);
            List<string> msgs = new List<string>();
            
            for (int i = 0; i < numOfMsgs; i++)
            {
                int msgNum = random.Next(ratioMsgs.Length);
                if (msgs.Contains(ratioMsgs[msgNum]))
                {
                    i--;
                }
                else
                {
                    msgs.Add(ratioMsgs[msgNum]);
                }
            }

            string result = "";
            foreach (var msg in msgs)
                result += $"{msg} + ";

            if (!DataConfig.IsExistingLinkedUser(Context.User.Id))
                result += $"You aren't linked";
            else
            {
                result += $"You don't have Wish Ascended";
            }

            await RespondAsync(result);
        }
    }
}
