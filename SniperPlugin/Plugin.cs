using GoPlugin;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Discord;
using Discord.WebSocket;
using POGOProtos.Enums;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using POGOProtos.Data;
using SniperPlugin.Data;
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
        private ObservableCollection<Coord> _duplicateLog;
        private PokemonId _queuedRequest;
        private CancellationTokenSource _mainToken;
        private CancellationTokenSource _requestToken;
        private int _snipeDelay;
        private int _requestDelay;
        private bool _firstLaunch;
        private bool _stopPending;


        public override async Task<bool> Load(IEnumerable<IManager> managers) // Occurs when the plugin is loaded.
        {
            Logger.Enabled = true;
            Logger.Write("Loading plugin...");

            _managers = managers;

            _client = new DiscordSocketClient();

            _accounts = new List<Account>();
            _duplicateLog = new ObservableCollection<Coord>();
            _queuedRequest = PokemonId.Missingno;
            _mainToken = new CancellationTokenSource();
            _requestToken = new CancellationTokenSource();
            _snipeDelay = 1;
            _requestDelay = 2;
            _firstLaunch = true;
            _stopPending = false;

            // Make sure the list doesn't exceed 5 items
            _duplicateLog.CollectionChanged += (sender, args) =>
            {
                if (args.Action != NotifyCollectionChangedAction.Add) return;
                while (_duplicateLog.Count > 5) _duplicateLog.RemoveAt(0);
            };

            // Hook into the MessageReceived event on DiscordSocketClient
            _client.MessageReceived += async (message) =>
            {
                if (message.Author.IsBot || message.Channel.Id == 282755515313029122)
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
                        await Snipe(message);
                }
            };

            Logger.Write("Loaded plugin successfully");

            await Task.Delay(0);
            
            return true;
        }

        public override async Task Run(IEnumerable<IManager> managers)
        {
            _managers = managers;
            var enumerable = _managers as IList<IManager> ?? _managers.ToList();

            if (enumerable.Count == 0)
            {
                var dialogResult2 = MessageBox.Show("No accounts selected. Do you want to stop sniping on all accounts?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2);
                if (dialogResult2 != DialogResult.Yes) return;
                _stopPending = true;
                Logger.Write("A stop is pending. Finishing up current snipes");
                return;
            }

            Logger.Write("Detected " + enumerable.Count + " selected accounts");

            var dialogResult = MessageBox.Show("Do you want to start sniping on " + enumerable.Count + " accounts?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
            if (dialogResult == DialogResult.No) return;

            LoadSettings();

            Logger.Write("Currently sniping on " + _accounts.Count + " accounts");
            if (!_firstLaunch) return;
            _firstLaunch = false;

            // ShadowBot's token: MjkyMTcyNDE0MzEyMjUxMzkz.C60K2Q.P9xtcvtb_YlwptpBwXyiFMSSfYs
            // Mare's token: MjM5NDE2NzIxODE4MTI0Mjg5.C7FJ8A.-N7y-Jn3LvOhsUWG_kSu6ZgwA5U
            await _client.LoginAsync(TokenType.User, "MjM5NDE2NzIxODE4MTI0Mjg5.C7FJ8A.-N7y-Jn3LvOhsUWG_kSu6ZgwA5U");
            await _client.StartAsync();
            Logger.Write("Logged into Discord");

            await Task.Delay(TimeSpan.FromSeconds(5));

            Request(_requestToken.Token);

            // Block this task until the program is exited.
            await Task.Delay(-1, _mainToken.Token);
        }

        public override async Task<bool> Save() // Occurs when closing. Occurs after settings are saved
        {
            Logger.Write("GoManager closed");
            await Task.Delay(0);
            return true;
        }

        public async Task<Coord> Parse(SocketMessage message)
        {
            var newCoord = new Coord();

            var parts = message.Content.Split(' ');
            foreach (var part in parts)
            {
                var partNew = part;
                if (part.Contains("*")) partNew = part.Replace("*", "");

                if (PokemonList.Get().Contains(partNew))
                {
                    newCoord.Pokemon = (PokemonId)Enum.Parse(typeof(PokemonId), partNew);
                }
                else if (Regex.IsMatch(partNew, @"^(\-?\d+(\.\d+)?),\s*(\-?\d+(\.\d+)?)$"))
                {
                    var coords = partNew.Split(',');
                    newCoord.Lat = double.Parse(coords[0], CultureInfo.InvariantCulture);
                    newCoord.Lon = double.Parse(coords[1], CultureInfo.InvariantCulture);
                }
                else if (Regex.IsMatch(partNew, @"^IV\d{1,2}$"))
                {
                    newCoord.Iv = int.Parse(partNew.Replace("IV", ""));
                }
                else if (partNew.Equals(":100:"))
                {
                    newCoord.Iv = 100;
                }
                //else if (Regex.IsMatch(partNew, @"^CP\d{1,4}$"))
                //{
                //    newCoord.Cp = int.Parse(partNew.Replace("CP", ""));
                //}
            }

            await Task.Delay(0);
            if (!newCoord.Valid() || IsDuplicate(newCoord)) return null;
            Logger.Write("Found a " + newCoord.Pokemon.ToString() + " at " + newCoord.Lat + ", " + newCoord.Lon + " with IV: " + newCoord.Iv + " (#" + message.Channel.Name + ")");
            return newCoord;
        }

        public async Task Snipe(SocketMessage message)
        {
            if (_accounts.Count == 0) return;

            var coord = await Parse(message);
            if (coord == null) return;

            var sniped = false;

            // Start sniping
            foreach (var account in _accounts)
            {
                foreach (var requirement in account.Requirements)
                {
                    if ((requirement.PokemonId != coord.Pokemon && requirement.PokemonId != PokemonId.Missingno) || requirement.MinIv > coord.Iv) continue;
                    account.SnipeResult = account.Manager.ManualSnipe(coord.Lat, coord.Lon, coord.Pokemon);
                    sniped = true;
                    Logger.Write(account.Manager.AccountName + " starts sniping " + coord.Pokemon.ToString() + " (IV: " + coord.Iv + ")");
                }
            }

            // Wait for sniping to finish
            foreach (var account in _accounts)
            {
                account.SnipeResult.Wait();
                if (!account.SnipeResult.Result.Success) continue;
                foreach (var requirement in account.Requirements)
                {
                    if (requirement.PokemonId != coord.Pokemon) continue;
                    requirement.AmountCaught++;
                    Logger.Write(account.Manager.AccountName + " successfully caught " + coord.Pokemon.ToString() + " (IV: " + coord.Iv + "). Caught " + requirement.AmountCaught + "/" + requirement.CatchAmount);
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
                        Logger.Write(account.Manager.AccountName + " caught enough " + coord.Pokemon.ToString() + "s (" + requirement.AmountCaught + "/" + requirement.CatchAmount + ")");
                    }
                    else if (CountPokemonCp(account.Manager.Pokemon, requirement.PokemonId, requirement.MinCp, requirement.CatchAmount))
                    {
                        account.Requirements.Remove(requirement);
                        Logger.Write(account.Manager.AccountName + " caught enough " + coord.Pokemon.ToString() + "s (" + requirement.AmountCaught + "/" + requirement.CatchAmount + ")");
                    }
                }
                if (account.Requirements.Count != 0) continue;
                _accounts.Remove(account);
                Logger.Write(account.Manager.AccountName + " caught all needed pokemons");
            }

            if (_stopPending)
            {
                StopPlugin();
                return;
            }

            if (sniped)
            {
                Logger.Write("Waiting for " + _snipeDelay + " minute(s)");
                await Task.Delay(TimeSpan.FromMinutes(_snipeDelay));
            }

            // Queue a request if empty
            foreach (var account in _accounts)
            {
                foreach (var requirement in account.Requirements)
                {
                    if (!requirement.Snipe || _queuedRequest != PokemonId.Missingno) continue;
                    _queuedRequest = requirement.PokemonId;
                    return;
                }
            }
        }
        public async Task Request(CancellationToken token)
        {
            while (true)
            {
                if (_queuedRequest != PokemonId.Missingno)
                {
                    var channel = _client.GetChannel(278110430235197441) as ISocketMessageChannel;
                    var sendMessageAsync = channel?.SendMessageAsync("?c " + _queuedRequest);
                    Logger.Write("Requested a " + _queuedRequest.ToString());
                    _queuedRequest = PokemonId.Missingno;
                    await Task.Delay(TimeSpan.FromMinutes(_requestDelay), token);
                }
                else
                {
                    await Task.Delay(TimeSpan.FromMinutes(_requestDelay / 2.0), token);
                }
            }
        }

        public static bool CountPokemonCp(IEnumerable<PokemonData> pokemons, PokemonId pokemonId, int minCp, int catchAmount)
        {
            return pokemons.Count(pokemon => (pokemon.PokemonId == pokemonId || pokemonId == PokemonId.Missingno) && minCp <= pokemon.Cp) >= catchAmount;
        }

        public bool IsDuplicate(Coord newCoord)
        {
            return _duplicateLog.Any(coord => newCoord.Pokemon.Equals(coord.Pokemon) && Math.Abs(newCoord.Lat - coord.Lat) < 0.0001 && Math.Abs(newCoord.Lon - coord.Lon) < 0.0001);
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
                            MinCp = minCp,
                            Snipe = (parts.Length == 5)
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

        public void StopPlugin()
        {
            _accounts.Clear();
            Logger.Write("Removed all accounts from sniping list");
            _requestToken.Cancel();
            _mainToken.Cancel();
            Logger.Write("Stopped plugin");
            _stopPending = false;
            _firstLaunch = true;
        }
    }
}