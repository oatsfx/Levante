using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Levante.Configs;
using System.Drawing;
using System.IO;
using Levante.Util;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using APIHelper;
using APIHelper.Structs;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Fergun.Interactive;
using Discord.Interactions;
using Levante.Rotations;

namespace Levante.Commands
{
    public class Destiny : InteractionModuleBase<SocketInteractionContext>
    {
        public InteractiveService Interactive { get; set; }

        [SlashCommand("current-offers", "Gives a list of emblem offers. If hash code provided, command will return with the specific offer.")]
        public async Task CurrentOffers([Summary("emblem-hash-code", "Hash code of an emblem offer. Make sure the code is an existing offer.")] long EmblemHashCode = -1)
        {
            if (EmblemHashCode == -1)
            {
                await RespondAsync(embed: EmblemOffer.GetOfferListEmbed().Build());
                return;
            }

            if (!EmblemOffer.HasExistingOffer(EmblemHashCode))
                await RespondAsync("Are you sure you entered the correct hash code? Run this command without any arguments and try again.");
            else
                await RespondAsync(embed: EmblemOffer.GetSpecificOffer(EmblemHashCode).BuildEmbed().Build());
        }

        [SlashCommand("daily", "Display Daily reset information.")]
        public async Task Daily()
        {
            await RespondAsync(embed: CurrentRotations.DailyResetEmbed().Build());
            return;
        }

        [SlashCommand("free-emblems", "Display a list of universal emblem codes.")]
        public async Task FreeEmblems()
        {
            var auth = new EmbedAuthorBuilder()
            {
                Name = $"Universal Emblem Codes",
                IconUrl = Context.Client.GetApplicationInfoAsync().Result.IconUrl,
            };
            var foot = new EmbedFooterBuilder()
            {
                Text = $"These codes are not limited to one account and can be used by anyone."
            };
            var embed = new EmbedBuilder()
            {
                Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                Author = auth,
                Footer = foot,
            };
            embed.Title = "";
            embed.Description =
                $"[The Visionary](https://www.bungie.net/common/destiny2_content/icons/65b4047b1b83aeeeb2e628305071fcea.jpg): **XFV-KHP-N97**\n" +
                $"[Cryonautics](https://www.bungie.net/common/destiny2_content/icons/6719dde48dca592addb4102cb747e097.jpg): **RA9-XPH-6KJ**\n" +
                $"[Galilean Excursion](https://bungie.net/common/destiny2_content/icons/3e99d575d00fb307c15fb5513dee13c6.jpg): **JYN-JAA-Y7D**\n" +
                $"[Future in Shadow](https://bungie.net/common/destiny2_content/icons/dd9af60ef15319ee986a1f6cc029fe71.jpg): **7LV-GTK-T7J**\n" +
                $"[Sequence Flourish](https://www.bungie.net/common/destiny2_content/icons/01e9b3863c14f9149ff4035b896ad5ed.jpg): **7D4-PKR-MD7**\n" +
                $"[A Classy Order](https://www.bungie.net/common/destiny2_content/icons/adaf0e2c15610cdfff750725701222ec.jpg): **YRC-C3D-YNC**\n" +
                $"[Be True](https://www.bungie.net/common/destiny2_content/icons/a6d9b66f124b25ac73969ebe4bc45b90.jpg): **ML3-FD4-ND9**\n" +
                $"[Heliotrope Warren](https://www.bungie.net/common/destiny2_content/icons/385c302dc22e6dafb8b50c253486d040.jpg): **L7T-CVV-3RD**\n" +
                $"[Shadow's Light](https://www.bungie.net//common/destiny2_content/icons/b296588f57aea1d15a04c3db6de98220.jpg): **F99-KPX-NCF**\n" +
                $"[Sneer of the Oni](https://www.bungie.net//common/destiny2_content/icons/bffe84c0efb9215dbdc8c4890c3e6234.jpg): **6LJ-GH7-TPA**\n" +
                $"[Countdown to Convergence](https://www.bungie.net//common/destiny2_content/icons/2560de3d4009044b291c6cfb69d11a7f.jpg): **PHV-6LF-9CP**\n" +
                $"[Liminal Nadir](https://www.bungie.net//common/destiny2_content/icons/4f9f612716a973ff03e5e17e9d7e7c91.jpg): **VA7-L7H-PNC**\n" +
                $"[Tangled Web](https://www.bungie.net/common/destiny2_content/icons/93a14eab2b1633d7affbf815d42af337.jpg): **PKH-JL6-L4R**\n" +
                $"[соняшник](https://bungie.net/common/destiny2_content/icons/4c113db5e1c0296027a1c7e1f84fb8b3.jpg) **JVG-VNT-GGG**" +
                $"*Redeem those codes [here](https://www.bungie.net/7/en/Codes/Redeem).*";

            await RespondAsync(embed: embed.Build());
            return;
        }

        [Group("guardians", "Display Guardian information.")]
        public class Guardians : InteractionModuleBase<SocketInteractionContext>
        {
            [SlashCommand("linked-user", "Get Guardian information of a Linked User.")]
            public async Task LinkedUser([Summary("user", "User to get Guardian information for.")] IUser User,
                [Summary("class", "Guardian Class to get information for.")] Guardian.Class ClassType,
                [Summary("platform", "Only needed if the user does not have Cross Save activated. This will be ignored otherwise."),
                Choice("Xbox", 1), Choice("PSN", 2), Choice("Steam", 3), Choice("Stadia", 5)]int ArgPlatform = 0)
            {
                DataConfig.DiscordIDLink LinkedUser = DataConfig.GetLinkedUser(User.Id);
                Guardian.Platform Platform = (Guardian.Platform)ArgPlatform;

                await DeferAsync();

                if (LinkedUser == null || !DataConfig.IsExistingLinkedUser(LinkedUser.DiscordID))
                {
                    await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = $"User is not linked; tell them to link using {BotConfig.DefaultCommandPrefix}link [THEIR BUNGIE TAG]."; });
                    return;
                }

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("X-API-Key", BotConfig.BungieApiKey);

                    var response = client.GetAsync($"https://www.bungie.net/platform/Destiny2/" + LinkedUser.BungieMembershipType + "/Profile/" + LinkedUser.BungieMembershipID + "?components=100,200").Result;
                    var content = response.Content.ReadAsStringAsync().Result;
                    dynamic item = JsonConvert.DeserializeObject(content);

                    if (DataConfig.IsBungieAPIDown(content))
                    {
                        await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = $"Bungie API is temporary down, try again later."; });
                        return;
                    }

                    if (item.ErrorCode != 1)
                    {
                        await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = $"An error occured with that account. Is there a connected Destiny 2 account?"; });
                        return;
                    }

                    List<Guardian> userGuardians = new List<Guardian>();
                    for (int i = 0; i < item.Response.profile.data.characterIds.Count; i++)
                    {
                        try
                        {
                            string charId = $"{item.Response.profile.data.characterIds[i]}";
                            if ((Guardian.Class)item.Response.characters.data[$"{charId}"].classType == ClassType)
                                userGuardians.Add(new Guardian(LinkedUser.UniqueBungieName, LinkedUser.BungieMembershipID, LinkedUser.BungieMembershipType, charId));
                        }
                        catch (Exception x)
                        {
                            Console.WriteLine($"{x}");
                        }
                    }

                    if (userGuardians.Count == 0)
                    {
                        await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = $"No guardian found."; });
                        return;
                    }

                    List<Embed> embeds = new List<Embed>();
                    foreach (var guardian in userGuardians)
                        embeds.Add(guardian.GetGuardianEmbed().Build());

                    await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Embeds = embeds.ToArray(); });
                    return;
                }
            }

            [SlashCommand("bungie-tag", "Get Guardian information of any player.")]
            public async Task BungieTag([Summary("player", "Player's Bungie tag to get Guardian information for.")] string BungieTag,
                [Summary("class", "Guardian Class to get information for.")] Guardian.Class ClassType,
                [Summary("platform", "Only needed if the user does not have Cross Save activated. This will be ignored otherwise."),
                Choice("Xbox", 1), Choice("PSN", 2), Choice("Steam", 3), Choice("Stadia", 5)]int ArgPlatform = 0)
            {
                Guardian.Platform Platform = (Guardian.Platform)ArgPlatform;

                await DeferAsync();

                string MembershipType = null;
                string MembershipID = null;
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("X-API-Key", BotConfig.BungieApiKey);

                    var response = client.GetAsync($"https://www.bungie.net/platform/Destiny2/SearchDestinyPlayer/-1/" + Uri.EscapeDataString(BungieTag)).Result;
                    var content = response.Content.ReadAsStringAsync().Result;
                    dynamic item = JsonConvert.DeserializeObject(content);

                    string memId = "";
                    string memType = "";
                    for (int i = 0; i < item.Response.Count; i++)
                    {
                        memId = item.Response[i].membershipId;
                        memType = item.Response[i].membershipType;

                        var memResponse = client.GetAsync($"https://www.bungie.net/platform/Destiny2/" + memType + "/Profile/" + memId + "/?components=100").Result;
                        var memContent = memResponse.Content.ReadAsStringAsync().Result;
                        dynamic memItem = JsonConvert.DeserializeObject(memContent);

                        if (memItem.ErrorCode == 1)
                        {
                            if (((int)memItem.Response.profile.data.userInfo.crossSaveOverride == (int)memItem.Response.profile.data.userInfo.membershipType) ||
                                ((int)memItem.Response.profile.data.userInfo.crossSaveOverride == 0 && (int)memItem.Response.profile.data.userInfo.membershipType == ((int)Platform)))
                            {
                                MembershipType = memType;
                                MembershipID = memId;
                                break;
                            }
                        }
                    }
                }

                if (MembershipType == null || MembershipID == null)
                {
                    await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = $"An error occurred when retrieving that player's Guardians. Run the command again and specify their platform."; });
                    return;
                }

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("X-API-Key", BotConfig.BungieApiKey);

                    var response = client.GetAsync($"https://www.bungie.net/platform/Destiny2/" + MembershipType + "/Profile/" + MembershipID + "?components=100,200").Result;
                    var content = response.Content.ReadAsStringAsync().Result;
                    dynamic item = JsonConvert.DeserializeObject(content);

                    if (DataConfig.IsBungieAPIDown(content))
                    {
                        await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = $"Bungie API is temporary down, try again later."; });
                        return;
                    }

                    if (item.ErrorCode != 1)
                    {
                        await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = $"An error occured with that account. Is there a connected Destiny 2 account?"; });
                        return;
                    }

                    List<Guardian> userGuardians = new List<Guardian>();
                    for (int i = 0; i < item.Response.profile.data.characterIds.Count; i++)
                    {
                        try
                        {
                            string charId = $"{item.Response.profile.data.characterIds[i]}";
                            if ((Guardian.Class)item.Response.characters.data[$"{charId}"].classType == ClassType)
                                userGuardians.Add(new Guardian(BungieTag, MembershipID, MembershipType, charId));
                        }
                        catch (Exception x)
                        {
                            Console.WriteLine($"{x}");
                        }
                    }

                    if (userGuardians.Count == 0)
                    {
                        await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = "No guardian found."; });
                        return;
                    }

                    List<Embed> embeds = new List<Embed>();
                    foreach (var guardian in userGuardians)
                        embeds.Add(guardian.GetGuardianEmbed().Build());

                    await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Embeds = embeds.ToArray(); });
                    return;
                }
            }
        }

        [SlashCommand("level", "Gets your Destiny 2 Season Pass Rank.")]
        public async Task GetLevel([Summary("user", "User you want the Season Pass rank of. Leave empty for your own.")] IUser User = null)
        {
            if (User == null)
            {
                User = Context.User as SocketGuildUser;
            }

            if (!DataConfig.IsExistingLinkedUser(User.Id))
            {
                await RespondAsync($"No account linked for {User.Mention}.", ephemeral: true);
                return;
            }
            try
            {
                string season = GetCurrentDestiny2Season(out int seasonNum);

                var app = await Context.Client.GetApplicationInfoAsync();
                var auth = new EmbedAuthorBuilder()
                {
                    Name = $"Season {seasonNum}: {season}",
                    IconUrl = User.GetAvatarUrl(),
                };
                var foot = new EmbedFooterBuilder()
                {
                    Text = $"Powered by Bungie API"
                };
                var embed = new EmbedBuilder()
                {
                    Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                    Author = auth,
                    Footer = foot,
                };
                embed.Description =
                    $"Player: **{DataConfig.GetLinkedUser(User.Id).UniqueBungieName}**\n" +
                    $"Level: **{DataConfig.GetUserSeasonPassLevel(User.Id, out int progress)}**\n" +
                    $"Progress to Next Level: **{String.Format("{0:n0}", progress)}/100,000**";

                await RespondAsync(embed: embed.Build());
            }
            catch
            {
                await RespondAsync($"An error occurred, please try again later.", ephemeral: true);
            }
        }

        [SlashCommand("lost-sector", "Get info on a Lost Sector based on Difficulty.")]
        public async Task LostSector([Summary("lost-sector", "Lost Sector name."),
                Choice("Bay of Drowned Wishes", 0), Choice("Chamber of Starlight", 1), Choice("Aphelion's Rest", 2),
                Choice("The Empty Tank", 3), Choice("K1 Logistics", 4), Choice("K1 Communion", 5),
                Choice("K1 Crew Quarters", 6), Choice("K1 Revelation", 7), Choice("Concealed Void", 8),
                Choice("Bunker E15", 9), Choice("Perdition", 10)] int ArgLS,
                [Summary("difficulty", "Lost Sector difficulty.")] LostSectorDifficulty ArgLSD)
        {
            /*LostSector LS = (LostSector)ArgLS;
            LostSectorDifficulty LSD = ArgLSD;*/

            await RespondAsync($"Gathering data on new Lost Sectors. Check back later!"/*, embed: LostSectorRotation.GetLostSectorEmbed(LS, LSD).Build()*/, ephemeral: true);
            return;
        }

        /*[SlashCommand("materials", "Gets your Destiny 2 material count.")]
        public async Task Materials([Summary("user", "User you want the Materials count for. Leave empty for your own.")] IUser User = null)
        {
            if (User == null)
            {
                User = Context.User as SocketGuildUser;
            }

            if (!DataConfig.IsExistingLinkedUser(User.Id))
            {
                await RespondAsync($"No account linked for {User.Mention}.", ephemeral: true);
                return;
            }

            var dil = DataConfig.GetLinkedUser(User.Id);

            int Glimmer, LegendaryShards, UpgradeModules, MasterworkCores, EnhancementPrisms, AscendantShards, SpoilsOfConquest, BrightDust,
                Adroit, Energetic, Mutable, Ruinous, Neutral, ResonantAlloy, AscendantAlloy = -1;

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-API-Key", BotConfig.BungieApiKey);

                var response = client.GetAsync($"https://www.bungie.net/Platform/Destiny2/" + dil.BungieMembershipType + "/Profile/" + dil.BungieMembershipID + "/?components=102").Result;
                var content = response.Content.ReadAsStringAsync().Result;
                dynamic item = JsonConvert.DeserializeObject(content);

                for (int i = 0; i < item.Response.profileInventory.data.items.Count; i++)
                {
                    // 
                }
            }
        }*/

        [SlashCommand("nightfall", "Display Nightfall information.")]
        public async Task Nightfall([Summary("nightfall", "Nightfall Strike."),
                Choice("The Hollowed Lair", 0), Choice("Lake of Shadows", 1), Choice("Exodus Crash", 2),
                Choice("The Corrupted", 3), Choice("The Devils' Lair", 4), Choice("Proving Grounds", 5)] int ArgNF)
        {
            await RespondAsync($"Gathering data on new Nightfalls. Check back later!", ephemeral: true);
            return;
        }

        [SlashCommand("patrol", "Display Patrol information.")]
        public async Task Patrol([Summary("location", "Patrol location."),
                Choice("The Dreaming City", 0), Choice("The Moon", 1), Choice("Europa", 2)] int ArgLocation)
        {
            await RespondAsync($"Command is under construction! Wait for a future update.", ephemeral: true);
            return;
        }

        [SlashCommand("raid", "Display Raid information.")]
        public async Task Raid([Summary("raid", "Raid name."),
                Choice("Last Wish", 0), Choice("Garden of Salvation", 1), Choice("Deep Stone Crypt", 2), Choice("Vault of Glass", 3)] int ArgRaid)
        {
            await RespondAsync($"Command is under construction! Wait for a future update.", ephemeral: true);
            return;
        }

        [SlashCommand("try-on", "Try on any emblem in the Bungie API.")]
        public async Task TryOut([Summary("emblem-hash", "Emblem hash code of the Emblem you want to try on.")] long HashCode,
            [Summary("name", "Put any name on the emblem. Leave blank to use your linked account, or discord name if not linked.")] string name = null)
        {
            if (name == null)
            {
                var linkedUser = DataConfig.GetLinkedUser(Context.User.Id);
                if (linkedUser != null)
                    name = linkedUser.UniqueBungieName.Substring(0, linkedUser.UniqueBungieName.Length - 5);
                else
                    name = Context.User.Username;
            }

            Emblem emblem;
            Bitmap bitmap;
            try
            {
                emblem = new Emblem(HashCode);
                bitmap = (Bitmap)GetImageFromPicPath(emblem.GetBackgroundUrl()); //load the image file
            }
            catch (Exception)
            {
                await RespondAsync($"No emblem found for Hash Code: {HashCode}.");
                return;
            }

            await DeferAsync();

            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                using (Font font = new Font("Neue Haas Grotesk Display Pro", 18, FontStyle.Bold))
                {
                    graphics.DrawString(name, font, Brushes.White, new PointF(83f, 7f));
                }

                using (Font font = new Font("Neue Haas Grotesk Display Pro", 14))
                {
                    graphics.DrawString($"Levante Bot{(BotConfig.BotSupportersDiscordIDs.Contains(Context.User.Id) ? " Supporter" : "")} {(RequireBotStaff.IsBotStaff(Context.User.Id) ? " Staff" : "")}", font, new SolidBrush(System.Drawing.Color.FromArgb(128, System.Drawing.Color.White)), new PointF(84f, 37f));
                }
            }

            using (var stream = new MemoryStream())
            {
                bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                byte[] bytes = stream.ToArray();
                using (var fs = new FileStream(@"temp.png", FileMode.Create))
                {
                    fs.Write(bytes, 0, bytes.Length);
                    fs.Close();
                }
            }

            var auth = new EmbedAuthorBuilder()
            {
                Name = $"Emblem Testing",
                IconUrl = emblem.GetIconUrl(),
            };
            var foot = new EmbedFooterBuilder()
            {
                Text = $"This is not a perfect replica."
            };
            int[] emblemRGB = emblem.GetRGBAsIntArray();
            var embed = new EmbedBuilder()
            {
                Color = new Discord.Color(emblemRGB[0], emblemRGB[1], emblemRGB[2]),
                Author = auth,
                Footer = foot
            };
            embed.Description =
                    $"This is probably what you'd look like in-game if you had **{emblem.GetName()}**.";
            embed.ImageUrl = @"attachment://temp.png";
            await Context.Interaction.FollowupWithFileAsync(filePath: "temp.png", embed: embed.Build());
        }

        [SlashCommand("view", "Get details on an emblem via its Hash Code found via Bungie's API.")]
        public async Task ViewEmblem([Summary("name", "Name of the item you want details for.")] string SearchQuery)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-API-Key", BotConfig.BungieApiKey);

                var response = client.GetAsync($"https://www.bungie.net/platform/Destiny2/Armory/Search/DestinyInventoryItemDefinition/" + SearchQuery + "/").Result;
                var content = response.Content.ReadAsStringAsync().Result;
                dynamic item = JsonConvert.DeserializeObject(content);

                if (DataConfig.IsBungieAPIDown(content))
                {
                    await RespondAsync($"Bungie API is temporary down, try again later.", ephemeral: true);
                    return;
                }

                if (item.Response.results.totalResults <= 0 || SearchQuery.Length < 4)
                {
                    await RespondAsync($"Unable to search using the term: {SearchQuery}", ephemeral: true);
                    return;
                }

                bool hasMultipleResults = false;
                List<EmblemSearch> emblemList = new List<EmblemSearch>();
                int resultNum = int.Parse($"{item.Response.results.totalResults}");
                int currentPage = -1;
                if (resultNum > 1)
                {
                    await RespondAsync($"Searching through {resultNum} entries, *estimated {(resultNum > 20 ? Math.Ceiling(resultNum / (double)25) * 4 + 2 : Math.Ceiling(resultNum / (double)25) + 2)} seconds*... <a:loading:872886173075378197>");
                    for (int i = 0; i < resultNum; i++)
                    {
                        if (i % 25 == 0)
                        {
                            currentPage++;
                            await Task.Delay(400);
                            response = client.GetAsync($"https://www.bungie.net/platform/Destiny2/Armory/Search/DestinyInventoryItemDefinition/" + SearchQuery + "/?page=" + currentPage).Result;
                            content = response.Content.ReadAsStringAsync().Result;
                            item = JsonConvert.DeserializeObject(content);
                        }
                        if (Emblem.HashIsAnEmblem(long.Parse($"{item.Response.results.results[i - (currentPage * 25)].hash}")))
                        {
                            emblemList.Add(new EmblemSearch(long.Parse($"{item.Response.results.results[i - (currentPage * 25)].hash}"), $"{item.Response.results.results[i - (currentPage * 25)].displayProperties.name}"));
                        }
                    }
                }
                else if (resultNum == 1)
                {
                    if (Emblem.HashIsAnEmblem(long.Parse($"{item.Response.results.results[0].hash}")))
                    {
                        emblemList.Add(new EmblemSearch(long.Parse($"{item.Response.results.results[0].hash}"), $"{item.Response.results.results[0].displayProperties.name}"));
                    }
                    await DeferAsync();
                }

                if (emblemList.Count > 1)
                    hasMultipleResults = true;

                int responseNum = 1;
                if (hasMultipleResults)
                {
                    string result = "";
                    for (int i = 0; i < emblemList.Count; i++)
                    {
                        result += $"**{i + 1})** {emblemList[i].GetName()}\n";
                    }
                    var multAuth = new EmbedAuthorBuilder()
                    {
                        Name = $"Multiple Emblems Found for: {SearchQuery}"
                    };
                    var multFoot = new EmbedFooterBuilder()
                    {
                        Text = $"Don't see the emblem you are looking for? Try doing a more specific search."
                    };
                    var multEmbed = new EmbedBuilder()
                    {
                        Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                        Author = multAuth,
                        Footer = multFoot
                    };
                    multEmbed.Title = $"React with the number of the emblem you want to view.";
                    multEmbed.Description = result;
                    Emoji[] reactEmotes =
                    {
                        "1️⃣",
                        "2️⃣",
                        "3️⃣",
                        "4️⃣",
                        "5️⃣",
                        "6️⃣",
                        "7️⃣",
                        "8️⃣",
                        "9️⃣",
                        "🔟",
                    };
                    ComponentBuilder buttons = new ComponentBuilder();

                    for (int i = 0; i < emblemList.Count; i++)
                        buttons.WithButton(customId: $"emblemSearch:{i}", emote: reactEmotes[i], style: ButtonStyle.Secondary, row: 0);

                    var msg = await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Embed = multEmbed.Build(); message.Content = null; message.Components = buttons.Build(); });

                    var responseButton = await Interactive.NextMessageComponentAsync(x => x.Channel.Id == Context.Channel.Id && x.User.Id == Context.Interaction.User.Id, timeout: TimeSpan.FromSeconds(BotConfig.DurationToWaitForNextMessage));

                    if (responseButton == null)
                    { 
                        await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = $"Closed command, invaild reaction."; message.Embed = new EmbedBuilder().Build(); message.Components = null; });
                        return;
                    }

                    for (int i = 0; i < emblemList.Count; i++)
                        if (responseButton.Value.Data.CustomId.Contains($"{i}"))
                            responseNum = i + 1;

                    await responseButton.Value.DeferAsync();
                }

                if (emblemList.Count == 0)
                {
                    await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = $"{SearchQuery} did not bring results of the Emblem type."; message.Embed = new EmbedBuilder().Build(); message.Components = null; });
                    return;
                }

                long HashCode = emblemList[responseNum - 1].GetEmblemHash();
                Emblem emblem;
                try
                {
                    emblem = new Emblem(HashCode);
                }
                catch (Exception)
                {
                    await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = $"No emblem found for Hash Code: {HashCode}."; message.Embed = new EmbedBuilder().Build(); message.Components = new ComponentBuilder().Build(); });
                    return;
                }

                if (!Emblem.HashIsAnEmblem(HashCode))
                {
                    await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = $"{emblem.GetName()} ({emblem.GetHashCode()}) is not an Emblem type."; message.Embed = new EmbedBuilder().Build(); message.Components = new ComponentBuilder().Build(); });
                    return;
                }

                await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Embed = emblem.GetEmbed().Build(); message.Content = null; message.Components = new ComponentBuilder().Build(); });
            }
        }

        [SlashCommand("weekly", "Display Weekly reset information.")]
        public async Task Weekly()
        {
            await RespondAsync($"", embed: CurrentRotations.WeeklyResetEmbed().Build());
            return;
        }

        private string GetCurrentDestiny2Season(out int SeasonNumber)
        {
            ulong seasonHash = 0;
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-API-Key", BotConfig.BungieApiKey);

                var response = client.GetAsync($"https://www.bungie.net/Platform/Destiny2/3/Profile/4611686018471482002/?components=100").Result;
                var content = response.Content.ReadAsStringAsync().Result;
                dynamic item = JsonConvert.DeserializeObject(content);

                seasonHash = item.Response.profile.data.currentSeasonHash;

                var response1 = client.GetAsync($"https://www.bungie.net/Platform/Destiny2/Manifest/DestinySeasonDefinition/" + seasonHash + "/").Result;
                var content1 = response1.Content.ReadAsStringAsync().Result;
                dynamic item1 = JsonConvert.DeserializeObject(content1);

                SeasonNumber = item1.Response.seasonNumber;
                return $"{item1.Response.displayProperties.name}";
            }
        }

        public static System.Drawing.Image GetImageFromPicPath(string strUrl)
        {
            using (WebResponse wrFileResponse = WebRequest.Create(strUrl).GetResponse())
            {
                var Platform = (BungieMembershipType) ArgPlatform;

                await DeferAsync();

                string MembershipType = null;
                string MembershipID = null;
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("X-API-Key", BotConfig.BungieApiKey);

                    var response = client.GetAsync("https://www.bungie.net/platform/Destiny2/SearchDestinyPlayer/-1/" +
                                                   Uri.EscapeDataString(BungieTag)).Result;
                    var content = response.Content.ReadAsStringAsync().Result;
                    dynamic item = JsonConvert.DeserializeObject(content);

                    if (item != null)
                        for (var i = 0; i < item.Response.Count; i++)
                        {
                            string memId = item.Response[i].membershipId;
                            string memType = item.Response[i].membershipType;

                            var memItem = API.GetProfile(long.Parse(memId), (BungieMembershipType) int.Parse(memType),
                                new[] {APIHelper.Structs.Components.QueryComponents.Profiles});

                            if (!(memItem is {ErrorCode: 1})) continue;
                            if (memItem.Response.profile.data.userInfo.crossSaveOverride !=
                                memItem.Response.profile.data.userInfo.membershipType &&
                                (memItem.Response.profile.data.userInfo.crossSaveOverride != 0 ||
                                 memItem.Response.profile.data.userInfo.membershipType != Platform))
                                continue;
                            MembershipType = memType;
                            MembershipID = memId;
                            break;
                        }
                }

                if (MembershipType == null)
                {
                    await Context.Interaction.ModifyOriginalResponseAsync(message =>
                    {
                        message.Content =
                            "An error occurred when retrieving that player's Guardians. Run the command again and specify their platform.";
                    });
                    return;
                }

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("X-API-Key", BotConfig.BungieApiKey);

                    var response = client.GetAsync("https://www.bungie.net/platform/Destiny2/" + MembershipType +
                                                   "/Profile/" + MembershipID + "?components=100,200").Result;
                    var content = response.Content.ReadAsStringAsync().Result;
                    dynamic item = JsonConvert.DeserializeObject(content);

                    if (DataConfig.IsBungieAPIDown(content))
                    {
                        await Context.Interaction.ModifyOriginalResponseAsync(message =>
                        {
                            message.Content = "Bungie API is temporary down, try again later.";
                        });
                        return;
                    }

                    if (item == null || item.ErrorCode != 1)
                    {
                        await Context.Interaction.ModifyOriginalResponseAsync(message =>
                        {
                            message.Content =
                                "An error occured with that account. Is there a connected Destiny 2 account?";
                        });
                        return;
                    }

                    var userGuardians = new List<Guardian>();
                    for (var i = 0; i < item.Response.profile.data.characterIds.Count; i++)
                        try
                        {
                            var charId = $"{item.Response.profile.data.characterIds[i]}";
                            if ((Guardian.Class) item.Response.characters.data[$"{charId}"].classType == ClassType)
                                userGuardians.Add(new Guardian(BungieTag, MembershipID, MembershipType, charId));
                        }
                        catch (Exception x)
                        {
                            Console.WriteLine($"{x}");
                        }

                    if (userGuardians.Count == 0)
                    {
                        await Context.Interaction.ModifyOriginalResponseAsync(message =>
                        {
                            message.Content = "No guardian found.";
                        });
                        return;
                    }

                    await Context.Interaction.ModifyOriginalResponseAsync(message =>
                    {
                        message.Embeds = userGuardians.Select(guardian => guardian.GetGuardianEmbed().Build()).ToArray();
                    });
                }
            }
        }
    }
}
