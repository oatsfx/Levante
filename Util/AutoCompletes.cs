using Discord;
using Discord.Interactions;
using Levante.Commands;
using Levante.Configs;
using Levante.Helpers;
using Levante.Rotations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Levante.Util
{
    public class WeaponAutocomplete : AutocompleteHandler
    {
        public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
        {
            await Task.Delay(0);
            var random = new Random();
            // Create a collection with suggestions for autocomplete
            List<AutocompleteResult> results = new();
            string SearchQuery = autocompleteInteraction.Data.Current.Value.ToString();
            if (String.IsNullOrWhiteSpace(SearchQuery))
            {
                while (results.Count < 7)
                {
                    var weapon = ManifestHelper.Weapons.ElementAt(random.Next(0, ManifestHelper.Weapons.Count));
                    if (!results.Exists(x => x.Value.Equals(weapon.Key)))
                        results.Add(new AutocompleteResult(weapon.Value, $"{weapon.Key}"));
                }
            }
                
            else
                foreach (var Weapon in ManifestHelper.Weapons)
                    if (Weapon.Value.Split('[')[0].ToLower().Contains(SearchQuery.ToLower()))
                        results.Add(new AutocompleteResult(Weapon.Value, $"{Weapon.Key}"));


            results = results.OrderBy(x => x.Name).ToList();

            // max - 25 suggestions at a time (API limit)
            return AutocompletionResult.FromSuccess(results.Take(25));
        }
    }

    public class ArmorModsAutocomplete : AutocompleteHandler
    {
        public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
        {
            await Task.Delay(0);
            var random = new Random();
            // Create a collection with suggestions for autocomplete
            List<AutocompleteResult> results = new();
            string SearchQuery = autocompleteInteraction.Data.Current.Value.ToString();
            if (String.IsNullOrWhiteSpace(SearchQuery))
                while (results.Count < 7)
                {
                    var item = ManifestHelper.Ada1Items.ElementAt(random.Next(0, ManifestHelper.Ada1Items.Count));
                    if (!results.Exists(x => x.Value.Equals(item.Key)))
                        results.Add(new AutocompleteResult(item.Value, $"{item.Key}"));
                }
            else
                foreach (var Item in ManifestHelper.Ada1Items)
                    if (Item.Value.ToLower().Contains(SearchQuery.ToLower()))
                        results.Add(new AutocompleteResult(Item.Value, $"{Item.Key}"));


            results = results.OrderBy(x => x.Name).ToList();

            // max - 25 suggestions at a time (API limit)
            return AutocompletionResult.FromSuccess(results.Take(25));
        }
    }

    public class NightfallAutocomplete : AutocompleteHandler
    {
        public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
        {
            await Task.Delay(0);
            // Create a collection with suggestions for autocomplete
            List<AutocompleteResult> results = new();
            string SearchQuery = autocompleteInteraction.Data.Current.Value.ToString();
            if (String.IsNullOrWhiteSpace(SearchQuery))
                for (int i = 0; i < NightfallRotation.Nightfalls.Count; i++)
                    results.Add(new AutocompleteResult(NightfallRotation.Nightfalls[i], i));
            else
                for (int i = 0; i < NightfallRotation.Nightfalls.Count; i++)
                    if (NightfallRotation.Nightfalls[i].Contains(SearchQuery.ToLower()))
                        results.Add(new AutocompleteResult(NightfallRotation.Nightfalls[i], i));

            // max - 25 suggestions at a time (API limit)
            return AutocompletionResult.FromSuccess(results.Take(25));
        }
    }

    public class NightfallWeaponAutocomplete : AutocompleteHandler
    {
        public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
        {
            await Task.Delay(0);
            // Create a collection with suggestions for autocomplete
            List<AutocompleteResult> results = new();
            string SearchQuery = autocompleteInteraction.Data.Current.Value.ToString();
            if (String.IsNullOrWhiteSpace(SearchQuery))
                for (int i = 0; i < NightfallRotation.NightfallWeapons.Count; i++)
                    results.Add(new AutocompleteResult(NightfallRotation.NightfallWeapons[i].Name, i));
            else
                for (int i = 0; i < NightfallRotation.NightfallWeapons.Count; i++)
                    if (NightfallRotation.NightfallWeapons[i].Name.Contains(SearchQuery.ToLower()))
                        results.Add(new AutocompleteResult(NightfallRotation.NightfallWeapons[i].Name, i));

            // max - 25 suggestions at a time (API limit)
            return AutocompletionResult.FromSuccess(results.Take(25));
        }
    }

    public class LostSectorAutocomplete : AutocompleteHandler
    {
        public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
        {
            await Task.Delay(0);
            // Create a collection with suggestions for autocomplete
            List<AutocompleteResult> results = new();
            string SearchQuery = autocompleteInteraction.Data.Current.Value.ToString();
            if (String.IsNullOrWhiteSpace(SearchQuery))
                for (int i = 0; i < LostSectorRotation.LostSectors.Count; i++)
                    results.Add(new AutocompleteResult(LostSectorRotation.LostSectors[i].Name, i));
            else
                for (int i = 0; i < LostSectorRotation.LostSectors.Count; i++)
                    if (LostSectorRotation.LostSectors[i].Name.ToLower().Contains(SearchQuery.ToLower()))
                        results.Add(new AutocompleteResult(LostSectorRotation.LostSectors[i].Name, i));

            // max - 25 suggestions at a time (API limit)
            return AutocompletionResult.FromSuccess(results.Take(25));
        }
    }

    public class CurrentOfferAutocomplete : AutocompleteHandler
    {
        public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
        {
            await Task.Delay(0);
            // Create a collection with suggestions for autocomplete
            List<AutocompleteResult> results = new();
            string SearchQuery = autocompleteInteraction.Data.Current.Value.ToString();

            if (String.IsNullOrWhiteSpace(SearchQuery))
                foreach (var Offer in EmblemOffer.CurrentOffers)
                    results.Add(new AutocompleteResult(Offer.OfferedEmblem.GetName(), $"{Offer.EmblemHashCode}"));
            else
                foreach (var Offer in EmblemOffer.CurrentOffers)
                    if (Offer.OfferedEmblem.GetName().ToLower().Contains(SearchQuery.ToLower()))
                        results.Add(new AutocompleteResult(Offer.OfferedEmblem.GetName(), $"{Offer.EmblemHashCode}"));

            results = results.OrderBy(x => x.Name).ToList();

            // max - 25 suggestions at a time (API limit)
            return AutocompletionResult.FromSuccess(results.Take(25));
        }
    }

    public class CountdownAutocomplete : AutocompleteHandler
    {
        public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
        {
            await Task.Delay(0);
            // Create a collection with suggestions for autocomplete
            List<AutocompleteResult> results = new();
            string SearchQuery = autocompleteInteraction.Data.Current.Value.ToString();

            if (String.IsNullOrWhiteSpace(SearchQuery))
                foreach (var Countdown in CountdownConfig.Countdowns)
                    results.Add(new AutocompleteResult(Countdown.Key, Countdown.Key));
            else
                foreach (var Countdown in CountdownConfig.Countdowns)
                    if (Countdown.Key.ToLower().Contains(SearchQuery.ToLower()))
                        results.Add(new AutocompleteResult(Countdown.Key, Countdown.Key));

            results = results.OrderBy(x => x.Name).ToList();

            // max - 25 suggestions at a time (API limit)
            return AutocompletionResult.FromSuccess(results.Take(25));
        }
    }

    public class BungieTagAutocomplete : AutocompleteHandler
    {
        public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
        {
            await Task.Delay(0);
            // Create a collection with suggestions for autocomplete
            List<AutocompleteResult> results = new();
            string SearchQuery = autocompleteInteraction.Data.Current.Value.ToString();
            if (String.IsNullOrWhiteSpace(SearchQuery))
                return AutocompletionResult.FromSuccess();
            else if (SearchQuery.Contains('#'))
            {
                foreach (var linkUser in DataConfig.DiscordIDLinks)
                    if (linkUser.UniqueBungieName.ToLower().Contains(SearchQuery.ToLower()))
                        results.Add(new AutocompleteResult($"{linkUser.UniqueBungieName}", $"{linkUser.UniqueBungieName}"));
            }
            else
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("X-API-Key", BotConfig.BungieApiKey);
                    //Attempt to use post, but results in Error Code 30.
                    //var values = new Dictionary<string, string>
                    //{
                    //    { "displayNamePrefix", $"{SearchQuery}" }
                    //};
                    //var postContent = new FormUrlEncodedContent(values);

                    //var response = client.PostAsync("https://www.bungie.net/Platform/User/Search/GlobalName/0/", postContent).Result;
                    var response = client.GetAsync($"https://www.bungie.net/Platform/User/Search/Prefix/{SearchQuery}/0/").Result;
                    var content = response.Content.ReadAsStringAsync().Result;
                    dynamic item = JsonConvert.DeserializeObject(content);

                    foreach (var result in item.Response.searchResults)
                        results.Add(new AutocompleteResult($"{result.bungieGlobalDisplayName}#{$"{result.bungieGlobalDisplayNameCode}".PadLeft(4, '0')}",
                            $"{result.bungieGlobalDisplayName}#{$"{result.bungieGlobalDisplayNameCode}".PadLeft(4, '0')}"));
                }
            }

            results = results.OrderBy(x => x.Name).ToList();

            // max - 25 suggestions at a time (API limit)
            return AutocompletionResult.FromSuccess(results.Take(25));
        }
    }

    public class EmblemAutocomplete : AutocompleteHandler
    {
        public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
        {
            await Task.Delay(0);
            var random = new Random();
            // Create a collection with suggestions for autocomplete
            List<AutocompleteResult> results = new();
            string SearchQuery = autocompleteInteraction.Data.Current.Value.ToString();
            if (String.IsNullOrWhiteSpace(SearchQuery))
                while (results.Count < 7)
                {
                    var emblem = ManifestHelper.Emblems.ElementAt(random.Next(0, ManifestHelper.Emblems.Count));
                    if (!results.Exists(x => x.Value.Equals(emblem.Key)))
                        results.Add(new AutocompleteResult(emblem.Value, $"{emblem.Key}"));
                }
            else
                foreach (var Emblem in ManifestHelper.Emblems)
                    if (Emblem.Value.ToLower().Contains(SearchQuery.ToLower()))
                        results.Add(new AutocompleteResult(Emblem.Value, $"{Emblem.Key}"));

            results = results.OrderBy(x => x.Name).ToList();

            // max - 25 suggestions at a time (API limit)
            return AutocompletionResult.FromSuccess(results.Take(25));
        }
    }

    public class WeaponPerkAutocomplete : AutocompleteHandler
    {
        public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
        {
            await Task.Delay(0);
            var random = new Random();
            // Create a collection with suggestions for autocomplete
            List<AutocompleteResult> results = new();
            string SearchQuery = autocompleteInteraction.Data.Current.Value.ToString();
            if (String.IsNullOrWhiteSpace(SearchQuery))
                while (results.Count < 7)
                {
                    var perk = ManifestHelper.Perks.ElementAt(random.Next(0, ManifestHelper.Perks.Count));
                    if (!results.Exists(x => x.Value.Equals(perk.Key)))
                        results.Add(new AutocompleteResult(perk.Value, $"{perk.Key}"));
                }
            else
                foreach (var Perk in ManifestHelper.Perks)
                    if (Perk.Value.ToLower().Contains(SearchQuery.ToLower()))
                        results.Add(new AutocompleteResult(Perk.Value, $"{Perk.Key}"));

            results = results.OrderBy(x => x.Name).ToList();

            // max - 25 suggestions at a time (API limit)
            return AutocompletionResult.FromSuccess(results.Take(25));
        }
    }

    public class AltarsOfSorrowAutocomplete : AutocompleteHandler
    {
        public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
        {
            await Task.Delay(0);
            // Create a collection with suggestions for autocomplete
            List<AutocompleteResult> results = new();
            string SearchQuery = autocompleteInteraction.Data.Current.Value.ToString();
            if (String.IsNullOrWhiteSpace(SearchQuery))
                for (int i = 0; i < AltarsOfSorrowRotation.AltarsOfSorrows.Count; i++)
                    results.Add(new AutocompleteResult($"{AltarsOfSorrowRotation.AltarsOfSorrows[i].Weapon} ({AltarsOfSorrowRotation.AltarsOfSorrows[i].WeaponType})", i));
            else
                for (int i = 0; i < AltarsOfSorrowRotation.AltarsOfSorrows.Count; i++)
                    if ($"{AltarsOfSorrowRotation.AltarsOfSorrows[i].Weapon} ({AltarsOfSorrowRotation.AltarsOfSorrows[i].WeaponType})".ToLower().Contains(SearchQuery.ToLower()))
                        results.Add(new AutocompleteResult($"{AltarsOfSorrowRotation.AltarsOfSorrows[i].Weapon} ({AltarsOfSorrowRotation.AltarsOfSorrows[i].WeaponType})", i));

            // max - 25 suggestions at a time (API limit)
            return AutocompletionResult.FromSuccess(results.Take(25));
        }
    }

    public class TerminalOverloadAutocomplete : AutocompleteHandler
    {
        public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
        {
            await Task.Delay(0);
            // Create a collection with suggestions for autocomplete
            List<AutocompleteResult> results = new();
            string SearchQuery = autocompleteInteraction.Data.Current.Value.ToString();
            if (String.IsNullOrWhiteSpace(SearchQuery))
                for (int i = 0; i < TerminalOverloadRotation.TerminalOverloads.Count; i++)
                    results.Add(new AutocompleteResult($"{TerminalOverloadRotation.TerminalOverloads[i].Location}, {TerminalOverloadRotation.TerminalOverloads[i].Weapon} ({TerminalOverloadRotation.TerminalOverloads[i].WeaponType})", i));
            else
                for (int i = 0; i < TerminalOverloadRotation.TerminalOverloads.Count; i++)
                    if ($"{TerminalOverloadRotation.TerminalOverloads[i].Location}, {TerminalOverloadRotation.TerminalOverloads[i].Weapon} ({TerminalOverloadRotation.TerminalOverloads[i].WeaponType})".ToLower().Contains(SearchQuery.ToLower()))
                        results.Add(new AutocompleteResult($"{TerminalOverloadRotation.TerminalOverloads[i].Location}, {TerminalOverloadRotation.TerminalOverloads[i].Weapon} ({TerminalOverloadRotation.TerminalOverloads[i].WeaponType})", i));

            // max - 25 suggestions at a time (API limit)
            return AutocompletionResult.FromSuccess(results.Take(25));
        }
    }
}
