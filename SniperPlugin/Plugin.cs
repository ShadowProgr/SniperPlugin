using GoPlugin;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Discord;
using Discord.WebSocket;
using POGOProtos.Enums;
using System.IO;
using POGOProtos.Data;
using SniperPlugin.Entities;

namespace SniperPlugin
{
    public class Plugin : IPlugin
    {
        // GoManager variables
        public override string PluginName { get; set; } = "ShadowProgr's Sniper Plugin";

        public override IEnumerable<PluginDropDownItem> MenuItems { get; set; }

        private IEnumerable<IManager> _managers;

        // Discord.Net variables
        private DiscordSocketClient _client;


        // Sniper Plugin variables
        private List<Account> _accounts;

        private const int Delay = 60000;

        private bool _firstLaunch;

        public override async Task<bool> Load(IEnumerable<IManager> managers) // Occurs when the plugin is loaded.
        {
            Logger.Enabled = true;

            _firstLaunch = true;

            Logger.Write("Loading plugin...");

            _managers = managers;

            _accounts = new List<Account>();
            Logger.Write("Initialized account list");

            _client = new DiscordSocketClient();
            Logger.Write("Initialized DiscordSocketClient");

            // Hook into the MessageReceived event on DiscordSocketClient
            _client.MessageReceived += async (message) =>
            {
                if (message.Author.IsBot)
                {
                    if (message.Channel.Id == 278110430235197441 || // candies_vip
                        message.Channel.Id == 279813108938047488 || // candies
                        message.Channel.Id == 279401203392184322 || // lvl30gold
                        message.Channel.Id == 259536527221063683 || // lvl30community
                        message.Channel.Id == 259753946837417994 || // 100gold
                        message.Channel.Id == 252811121298243585 || // 100community
                        message.Channel.Id == 283442241262059520 || // cp100gold
                        message.Channel.Id == 283442715188920321 || // cp100community
                        message.Channel.Id == 283957730744598528 || // evo3community
                        message.Channel.Id == 285459058364907520 || // evo3community
                        message.Channel.Id == 259747484333637632 || // 90gold
                        message.Channel.Id == 252811322109067267 || // 90bronze
                        message.Channel.Id == 261331571028656128 || // 90community
                        message.Channel.Id == 282872644586700800 || // 2900cp
                        message.Channel.Id == 279399790729494529 || // 2800cp
                        message.Channel.Id == 282872847058075648 || // 2700cp
                        message.Channel.Id == 259296509361782784 || // 2600cp
                        message.Channel.Id == 259505011686375425 || // 2000cp
                        message.Channel.Id == 261865410897510400 || // super_rare
                        message.Channel.Id == 282755515313029122 || // unown
                        message.Channel.Id == 282767521248182272 || // tyranitar
                        message.Channel.Id == 264412707762077697 || // blissey
                        message.Channel.Id == 262451281703075840 || // dragonite
                        message.Channel.Id == 262451314041159681 || // snorlax
                        message.Channel.Id == 262451373889683467 // lapras
                    )
                        await ParseAndSnipe(message.Content);
                }
            };
            Logger.Write("Hooked into Pokedex100 channels");

            Logger.Write("Loaded plugin successfully");

            await Task.Delay(0);

            return true;
        }

        public override async Task Run(IEnumerable<IManager> managers)
        {
            _managers = managers;
            var enumerable = _managers as IList<IManager> ?? _managers.ToList();
            Logger.Write("Detected " + enumerable.Count + " selected accounts");

            var dialogResult = MessageBox.Show("Do you want to start sniping on " + enumerable.Count + " accounts?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
            if (dialogResult == DialogResult.No) return;

            LoadSettings();

            Logger.Write("Currently sniping on " + _accounts.Count + " accounts");
            if (!_firstLaunch) return;
            _firstLaunch = false;

            await _client.LoginAsync(TokenType.User, "MjM5NDE2NzIxODE4MTI0Mjg5.C7FJ8A.-N7y-Jn3LvOhsUWG_kSu6ZgwA5U");
            //await _client.LoginAsync(TokenType.User, "MjkyMTcyNDE0MzEyMjUxMzkz.C60K2Q.P9xtcvtb_YlwptpBwXyiFMSSfYs");
            await _client.StartAsync();
            Logger.Write("Logged into Discord");

            // Block this task until the program is exited.
            await Task.Delay(-1);
        }

        public override async Task<bool> Save() // Occurs when closing. Occurs after settings are saved
        {
            Logger.Write("GoManager closed");
            await Task.Delay(0);
            return true;
        }

        public async Task ParseAndSnipe(string message)
        {
            var i = 0;
            var parts = message.Split(' ');
            if (message.StartsWith("#")) i = 1;

            // Parse PokemonID
            var name = parts[0 + i].Replace("*", "");
            var pokemonId = (PokemonId)Enum.Parse(typeof(PokemonId), name);

            // Parse lat,lon
            var coords = parts[1 + i].Split(',');
            var lat = double.Parse(coords[0], CultureInfo.InvariantCulture);
            var lon = double.Parse(coords[1], CultureInfo.InvariantCulture);

            // Parse IV
            int iv;
            if (parts[2 + i].Equals(":100:"))
            {
                iv = 100;
            }
            else
            {
                var strId = parts[2 + i].Substring(2);
                iv = int.Parse(strId);
            }
            Logger.Write("Found a " + pokemonId.ToString() + " at " + lat + ", " + lon + " with IV: " + iv);

            var sniped = false;
            // Start sniping
            foreach (var account in _accounts)
            {
                foreach (var requirement in account.Requirements)
                {
                    if ((requirement.PokemonId != pokemonId && requirement.PokemonId != PokemonId.Missingno) || requirement.MinIv > iv) continue;
                    account.SnipeResult = account.Manager.ManualSnipe(lat, lon, pokemonId);
                    sniped = true;
                    Logger.Write(account.Manager.AccountName + " starts sniping " + pokemonId.ToString() + "(IV: " + iv + ")");
                }
            }

            // Wait for sniping to finish
            foreach (var account in _accounts)
            {
                account.SnipeResult.Wait();
                if (!account.SnipeResult.Result.Success) continue;
                foreach (var requirement in account.Requirements)
                {
                    if (requirement.PokemonId != pokemonId) continue;
                    requirement.AmountCaught++;
                    Logger.Write(account.Manager.AccountName + " successfully caught " + pokemonId.ToString() + "(IV: " + iv + "). Caught " + requirement.AmountCaught + "/" + requirement.CatchAmount);
                }
            }

            // Check if requirements are fulfilled
            foreach (var account in _accounts)
            {
                foreach (var requirement in account.Requirements)
                {
                    if (requirement.AmountCaught < requirement.CatchAmount) continue;

                    if (requirement.MinCp == 0)
                    {
                        account.Requirements.Remove(requirement);
                        Logger.Write(account.Manager.AccountName + " caught enough " + pokemonId.ToString() + "s (" + requirement.AmountCaught + "/" + requirement.CatchAmount + ")");
                    }
                    else if (CountPokemonCp(account.Manager.Pokemon, requirement.PokemonId, requirement.MinCp, requirement.CatchAmount))
                    {
                        account.Requirements.Remove(requirement);
                        Logger.Write(account.Manager.AccountName + " caught enough " + pokemonId.ToString() + "s (" + requirement.AmountCaught + "/" + requirement.CatchAmount + ")");
                    }
                }
                if (account.Requirements.Count != 0) continue;
                _accounts.Remove(account);
                Logger.Write(account.Manager.AccountName + " caught all needed pokemons");
            }

            if (sniped)
            {
                Logger.Write("Waiting for " + Delay / 1000 + " seconds");
                await Task.Delay(Delay);
            }
        }

        public static bool CountPokemonCp(IEnumerable<PokemonData> pokemons, PokemonId pokemonId, int minCp, int catchAmount)
        {
            //var count = 0;
            //foreach (var pokemon in pokemons)
            //{
            //    if ((pokemon.PokemonId == pokemonId || pokemonId == PokemonId.Missingno) && minCp <= pokemon.Cp)
            //        count++;
            //}
            //return count >= catchAmount;
            return pokemons.Count(pokemon => (pokemon.PokemonId == pokemonId || pokemonId == PokemonId.Missingno) && minCp <= pokemon.Cp) >= catchAmount;
        }

        public void LoadSettings()
        {
            var openDialog = new OpenFileDialog
            {
                Filter = "Config Files (.ini)|*.ini",
                FilterIndex = 1,
                Multiselect = false
            };
            if (openDialog.ShowDialog() != DialogResult.OK) return;

            var newAccounts = new List<Account>();
            foreach (var manager in _managers)
            {
                var exists = _accounts.Any(account => account.Manager.Equals(manager));
                if (exists)
                {
                    Logger.Write(manager.AccountName + " is already sniping. Skipped");
                    break;
                }
                newAccounts.Add(new Account
                {
                    Manager = manager,
                    Requirements = new List<Requirement>()
                });
            }

            using (var reader = new StreamReader(openDialog.FileName))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var parts = line.Split(';');
                    foreach (var account in newAccounts)
                    {
                        PokemonId pokemonId;
                        try { pokemonId = (PokemonId)Enum.Parse(typeof(PokemonId), parts[0]); } catch { pokemonId = PokemonId.Missingno; }
                        var catchAmount = int.Parse(parts[1]);
                        var minIv = int.Parse(parts[2]);
                        var minCp = int.Parse(parts[3]);
                        account.Requirements.Add(new Requirement
                        {
                            PokemonId = pokemonId,
                            CatchAmount = catchAmount,
                            AmountCaught = 0,
                            MinIv = minIv,
                            MinCp = minCp
                        });
                        Logger.Write("Added new requirement [" + pokemonId.ToString() + "; " + catchAmount + "; " + minIv + "; " + minCp + "] to account " + account.Manager.AccountName);
                    }
                }
            }

            foreach (var account in newAccounts)
            {
                _accounts.Add(account);
            }
            Logger.Write("Added " + newAccounts.Count + " accounts");
        }
    }
}