using Gw2Sharp;
using ProtoBuf;
using System.Net.Http.Json;
using System.Text.Json;
using Gma.DataStructures.StringSearch;
using System.Diagnostics;
using Gw2Sharp.WebApi.V2.Models;

namespace IndexBuilder
{

    public class ItemJson
    {
        public int id { get; set; }
        public string name { get; set; }
        public string chat_link { get; set; }
        public string icon { get; set; }
        public string description { get; set; }
        public string type { get; set; }
        public string rarity { get; set; }
        public int level { get; set; }
        public int vendor_value { get; set; }
        public int default_skin { get; set; }
        public List<string> flags { get; set; }
        public List<string> game_types { get; set; }
        public List<string> restrictions { get; set; }
        public dynamic details { get; set; }
    }

    public class InventoryItem
    {
        public string Source { get; set; }
        public string? LocationHint { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
        public int Count { get; set; }
        public int? Charges { get; set; }
        public int[]? Infusions { get; set; }
        public int[]? Upgrades { get; set; }
        public int? Skin { get; set; }
        public string? Binding { get; set; }
        public string? BoundTo { get; set; }

        public InventoryItem(AccountItem item, string source, string? locationHint = null)
        {
            Source = source;
            LocationHint = locationHint;
            Id = item.Id;
            Count = item.Count;
            Charges = item.Charges;
            Infusions = item.Infusions?.ToArray();
            Upgrades = item.Upgrades?.ToArray();
            Skin = item.Skin;
            Binding = item.Binding?.ToString();
            BoundTo = item.BoundTo;
        }

        public InventoryItem(CharacterEquipmentItem item, string source, string? locationHint = null)
        {
            Source = source;
            LocationHint = locationHint;
            Id = item.Id;
            Infusions = item.Infusions?.ToArray();
            Upgrades = item.Upgrades?.ToArray();
            Skin = item.Skin;
            Binding = item.Binding?.ToString();
            BoundTo = item.BoundTo;
        }

        public InventoryItem(int itemId, string source)
        {
            Source = source;
            Id = itemId;
        }

        public InventoryItem(AccountMaterial item)
        {
            Source = "Material Storage";
            Id = item.Id;
            Count = item.Count;
        }

        public InventoryItem(CommerceDeliveryItem item)
        {
            Source = "Trading Post Delivery Box";
            Id = item.Id;
            Count = item.Count;
        }

        public InventoryItem(CommerceTransactionCurrent item)
        {
            Source = "Trading Post Selling";
            Id = item.ItemId;
            Count = item.Quantity;
        }
    }

    public static class DictionaryExtensions
    {
        public static void AddOrUpdate(this Dictionary<int, List<InventoryItem>> dict, int id, InventoryItem item)
        {
            List<InventoryItem> existing;
            if (!dict.TryGetValue(id, out existing))
            {
                existing = new List<InventoryItem>();
                dict[id] = existing;
            }
            existing.Add(item);
        }
    }

    public class MainClass
    {
        public void AddStringToKeywords(string str, HashSet<string> keywords)
        {
            var words = str.Split(' ');
            foreach (var word in words)
            {
                keywords.Add(word);
            }
        }

        public async Task CollectKeywords(string allItemsPath, string condensedItemsPath)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            var AllItemsJson = JsonSerializer.Deserialize<Dictionary<int, CondensedItem>>(System.IO.File.ReadAllText(condensedItemsPath));
            Console.WriteLine($"Deserialize: {stopwatch.ElapsedMilliseconds}");
            stopwatch = Stopwatch.StartNew();

            var trie = new UkkonenTrie<int>(3);
            foreach (var kv in AllItemsJson)
            {
                trie.Add(kv.Value.Name.ToLowerInvariant(), kv.Key);
            }
            Console.WriteLine($"Trie build: {stopwatch.ElapsedMilliseconds}");
            stopwatch = Stopwatch.StartNew();

            var playerItems = await GetAllOwnedItems();
            Console.WriteLine($"Get player items: {stopwatch.ElapsedMilliseconds}");
            stopwatch = Stopwatch.StartNew();

            string s;
            Console.WriteLine("try:");
            while ((s = Console.ReadLine()) != "Q")
            {
                // UkkonenTrie
                stopwatch = Stopwatch.StartNew();
                var results = trie.Retrieve(s);
                Console.WriteLine($"UkkonenTrie found all matching items: {stopwatch.ElapsedMilliseconds}");

                // Brute force
                stopwatch = Stopwatch.StartNew();
                var results2 = AllItemsJson.Where(item => item.Value.Name.ToLowerInvariant().Contains(s)).Select(item => item.Key).ToList();
                Console.WriteLine($"Brute force found all matching items: {stopwatch.ElapsedMilliseconds}");

                List<InventoryItem> playerMatchingItems = new List<InventoryItem>();
                foreach (var id in results)
                {
                    List<InventoryItem> items;
                    if (playerItems.TryGetValue(id, out items))
                    {
                        foreach (var item in playerItems[id])
                        {
                            playerMatchingItems.Add(item);
                        }
                    }
                }
                Console.WriteLine($"Matched all matching items: {stopwatch.ElapsedMilliseconds}");

                foreach (var item in playerMatchingItems)
                {
                    CondensedItem staticItem;
                    if (AllItemsJson.TryGetValue(item.Id, out staticItem))
                    {
                        Console.WriteLine($"{item.Source} {item.LocationHint}: {staticItem.Name} x {item.Count}");
                    }
                    else
                    {
                        Console.WriteLine($"Unknown item {item.Id}, {item.Source}: {item.Count}");
                    }
                }
                Console.WriteLine($"{stopwatch.ElapsedMilliseconds}");
                Console.WriteLine("try:");
            }
        }

        public async Task<Dictionary<int, List<InventoryItem>>> GetAllOwnedItems()
        {
            string apiKey = "";
            var connection = new Gw2Sharp.Connection(apiKey);
            using var client = new Gw2Sharp.Gw2Client(connection);
            var webApiClient = client.WebApi.V2;

            Dictionary<int, List<InventoryItem>> allPlayerItems = new Dictionary<int, List<InventoryItem>>();

            var addItemToAllItems = (InventoryItem item) =>
            {
                allPlayerItems.AddOrUpdate(item.Id, item);
                if (item.Skin.HasValue && item.Skin.Value > 0)
                {
                    allPlayerItems.AddOrUpdate(item.Skin.Value, item);
                }
                if (item.Infusions != null)
                {
                    foreach (var infusionId in item.Infusions)
                    {
                        allPlayerItems.AddOrUpdate(infusionId, item);
                    }
                }
                if (item.Upgrades != null)
                {
                    foreach (var upgradeId in item.Upgrades)
                    {
                        allPlayerItems.AddOrUpdate(upgradeId, item);
                    }
                }
            };

            var bankItems = await webApiClient.Account.Bank.GetAsync();
            foreach (var item in bankItems)
            {
                if (item != null)
                {
                    addItemToAllItems(new InventoryItem(item, "Bank"));
                }
            }

            var sharedInventoryItems = await webApiClient.Account.Inventory.GetAsync();
            foreach (var item in sharedInventoryItems)
            {
                if (item != null)
                {
                    addItemToAllItems(new InventoryItem(item, "Bank"));
                }
            }

            var materials = await webApiClient.Account.Materials.GetAsync();
            foreach (var item in materials)
            {
                addItemToAllItems(new InventoryItem(item));
            }

            var tpBoxItems = (await webApiClient.Commerce.Delivery.GetAsync()).Items;
            foreach (var item in tpBoxItems)
            {
                addItemToAllItems(new InventoryItem(item));
            }

            var tpSellItems = await webApiClient.Commerce.Transactions.Current.Sells.GetAsync();
            foreach (var item in tpSellItems)
            {
                addItemToAllItems(new InventoryItem(item));
            }

            var characters = await webApiClient.Characters.AllAsync();
            foreach (var character in characters)
            {
                if (character.Bags != null)
                {
                    foreach (var bag in character.Bags)
                    {
                        if (bag != null)
                        {
                            addItemToAllItems(new InventoryItem(bag.Id, character.Name));
                            foreach (var item in bag.Inventory)
                            {
                                if (item != null)
                                {
                                    addItemToAllItems(new InventoryItem(item, character.Name, "Inventory"));
                                }
                            }
                        }
                    }
                }

                if (character.EquipmentTabs != null)
                {
                    foreach (var tab in character.EquipmentTabs)
                    {
                        if (tab != null)
                        {
                            foreach (var equipItem in tab.Equipment)
                            {
                                if (equipItem != null)
                                {
                                    addItemToAllItems(new InventoryItem(equipItem, character.Name, "Equipment Tab " + tab.Tab));
                                }
                            }
                        }
                    }
                }
            }

            return allPlayerItems;
        }

        public static void BuildIndex(string lang, string allItemsPath, string condensedItemsPath)
        {
            FetchApiData fetch = new FetchApiData(allItemsPath);
            fetch.FetchAll(lang).Wait();
            fetch.Condense(condensedItemsPath).Wait();
        }

        public static void TestIndex(string allItemsPath, string condensedItemsPath)
        {
            MainClass mc = new MainClass();
            mc.CollectKeywords(allItemsPath, condensedItemsPath).Wait();
        }

        public static void Main()
        {
            const string allItemsPath = @"all_itemsjson.json";
            const string condensedItemsPath = @"all_items.json";

            string[] languages = new string[]
            {
                "en", 
                "es",
                "de",
                "fr",
                "zh",
            };

            foreach (var lang in languages)
            {
                var langPath = $".\\{lang}\\";
                Directory.CreateDirectory(langPath);
                var langAllItemsPath = langPath + allItemsPath;
                var langCondensedItemsPath = langPath + condensedItemsPath;

                BuildIndex(lang, langAllItemsPath, langCondensedItemsPath);
                //TestIndex(langAllItemsPath, langCondensedItemsPath);
            }

            Console.ReadLine();
        }
    }

}