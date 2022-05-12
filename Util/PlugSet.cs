﻿using Discord.WebSocket;
using Levante.Configs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Levante.Util
{
    public class PlugSet
    {
        public List<WeaponPerk> WeaponPerks { get; }
        private long HashCode { get; set; }
        private string APIUrl { get; set; }
        private string Content { get; set; }

        public PlugSet(long hashCode)
        {
            HashCode = hashCode;
            APIUrl = "https://www.bungie.net/platform/Destiny2/Manifest/DestinyPlugSetDefinition/" + HashCode;
            WeaponPerks = new List<WeaponPerk>();

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-API-Key", BotConfig.BungieApiKey);

                var response = client.GetAsync(APIUrl).Result;
                Content = response.Content.ReadAsStringAsync().Result;
                dynamic item = JsonConvert.DeserializeObject(Content);

                for (int i = 0; i < item.Response.reusablePlugItems.Count; i++)
                    WeaponPerks.Add(new WeaponPerk((long)item.Response.reusablePlugItems[i].plugItemHash));
            }
        }

        public string BuildStringList(SocketGuild guild)
        {
            string result = "";
            foreach (var Perk in WeaponPerks)
            {
                string json = File.ReadAllText(EmoteConfig.FilePath);
                var emoteCfg = JsonConvert.DeserializeObject<EmoteConfig>(json);
                if (!emoteCfg.Emotes.ContainsKey(Perk.GetName().Replace(" ", "").Replace("-", "").Replace("'", "")))
                {
                    using (WebResponse wrFileResponse = WebRequest.Create(Perk.GetIconUrl()).GetResponse())
                    {
                        using (Stream objWebStream = wrFileResponse.GetResponseStream())
                        {
                            MemoryStream ms = new MemoryStream();
                            objWebStream.CopyTo(ms, 8192);
                            Bitmap bitmap = (Bitmap)Image.FromStream(ms);
                            using (var stream = new MemoryStream())
                            {
                                bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                                byte[] bytes = stream.ToArray();
                                using (var fs = new FileStream(@"tempEmote.png", FileMode.Create))
                                {
                                    fs.Write(bytes, 0, bytes.Length);
                                    fs.Close();
                                }
                                Task.Run(() => emoteCfg.AddEmote(Perk.GetName().Replace(" ", "").Replace("-", "").Replace("'", ""), new Discord.Image(@"tempEmote.png")));
                                emoteCfg.UpdateJSON();
                            }
                        }
                    }
                }
                result += $"{emoteCfg.Emotes[Perk.GetName().Replace(" ", "").Replace("-", "").Replace("'", "")]}\n";
            }

            return result;
        }

        public string BuildStringList()
        {
            string result = "";

            // Remove Enhanced Perks
            var perkList = new List<WeaponPerk>();
            foreach (WeaponPerk perk in WeaponPerks.Where(perk => !perk.IsEnhanced()))
                perkList.Add(perk);

            foreach (var Perk in perkList)
            {
                string json = File.ReadAllText(EmoteConfig.FilePath);
                var emoteCfg = JsonConvert.DeserializeObject<EmoteConfig>(json);
                if (!emoteCfg.Emotes.ContainsKey(Perk.GetName().Replace(" ", "").Replace("-", "").Replace("'", "")))
                {
                    using (WebClient client = new WebClient())
                    {
                        client.DownloadFile(new Uri(Perk.GetIconUrl()), @"tempEmote.png");
                        Task.Run(() => emoteCfg.AddEmote(Perk.GetName().Replace(" ", "").Replace("-", "").Replace("'", ""), new Discord.Image(@"tempEmote.png"))).Wait();
                        emoteCfg.UpdateJSON();
                    }
                }
                result += $"{emoteCfg.Emotes[Perk.GetName().Replace(" ", "").Replace("-", "").Replace("'", "")]}{Perk.GetName()}\n";
            }

            return result;
        }

        private System.Drawing.Image GetImageFromPicPath(string strUrl)
        {
            using (WebResponse wrFileResponse = WebRequest.Create(strUrl).GetResponse())
            {
                using (Stream objWebStream = wrFileResponse.GetResponseStream())
                {
                    MemoryStream ms = new MemoryStream();
                    objWebStream.CopyTo(ms, 8192);
                    return System.Drawing.Image.FromStream(ms);
                }
            }
        }
    }
}
