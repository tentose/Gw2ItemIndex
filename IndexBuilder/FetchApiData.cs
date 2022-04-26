using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace IndexBuilder
{
    internal class FetchApiData
    {
        const int RetryDelay = 61 * 1000;
        Dictionary<int, string> AllItemsJson = new Dictionary<int, string>();
        string ItemsJsonCachePath;

        public FetchApiData(string itemsJsonCachePath)
        {
            ItemsJsonCachePath = itemsJsonCachePath;
        }

        public async Task FetchAll()
        {
            try
            {
                AllItemsJson = JsonSerializer.Deserialize<Dictionary<int, string>>(File.ReadAllText(ItemsJsonCachePath));

                if (AllItemsJson == null)
                {
                    AllItemsJson = new Dictionary<int, string>();
                }
            }
            catch (Exception ex)
            {
                AllItemsJson = new Dictionary<int, string>();
            }

            Console.WriteLine($"Have {AllItemsJson.Count} items in cache");

            try
            {
                HttpClient client = new HttpClient();
                var ids = await client.GetFromJsonAsync<int[]>("https://api.guildwars2.com/v2/items");
                Console.WriteLine($"Found {ids.Length} items");

                List<int> idsToRead = new List<int>();
                foreach (int id in ids)
                {
                    if (!AllItemsJson.ContainsKey(id))
                    {
                        idsToRead.Add(id);
                    }

                    if (idsToRead.Count > 190)
                    {
                        bool succeeded = await ProcessIds(client, idsToRead);
                        if (succeeded)
                        {
                            Console.WriteLine($"Succeeded writing {idsToRead.Count} objects");
                            Console.WriteLine($"Cache size: {AllItemsJson.Count}");
                            idsToRead.Clear();
                        }
                        else
                        {
                            Console.WriteLine($"Failed. Exiting");
                            break;
                        }
                    }
                }

                if (idsToRead.Count > 0 && await ProcessIds(client, idsToRead))
                {
                    Console.WriteLine($"Succeeded writing {idsToRead.Count} objects");
                    Console.WriteLine($"Cache size: {AllItemsJson.Count}");
                    idsToRead.Clear();
                }

                Console.WriteLine($"Finished");
            }
            finally
            {
                string json = JsonSerializer.Serialize(AllItemsJson);
                File.WriteAllText(ItemsJsonCachePath, json);
            }
        }

        class CondensedItem
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public string IconUrl { get; set; }
            public Rarity Rarity { get; set; }
        }

        enum Rarity
        {
            Unknown = 0,
            Junk = 1,
            Basic,
            Fine,
            Masterwork,
            Rare,
            Exotic,
            Ascended,
            Legendary,
        };

        private Rarity RarityStringToRarity(string s)
        {
            switch (s)
            {
                case "Junk": return Rarity.Junk;
                case "Basic": return Rarity.Basic;
                case "Fine": return Rarity.Fine;
                case "Masterwork": return Rarity.Masterwork;
                case "Rare": return Rarity.Rare;
                case "Exotic": return Rarity.Exotic;
                case "Ascended": return Rarity.Ascended;
                case "Legendary": return Rarity.Legendary;
                default: return Rarity.Unknown;
            }
        }

        public async Task Condense(string condensedJsonPath)
        {
            Dictionary<int, CondensedItem> condensedJson = new Dictionary<int, CondensedItem>();

            foreach (var kv in AllItemsJson)
            {
                var rawItem = JsonSerializer.Deserialize<ItemJson>(kv.Value);
                var condensedItem = new CondensedItem();
                condensedItem.Name = rawItem.name;
                condensedItem.Description = rawItem.description;
                condensedItem.IconUrl = rawItem.icon;
                condensedItem.Rarity = RarityStringToRarity(rawItem.rarity);
                condensedJson.Add(kv.Key, condensedItem);
            }

            string json = JsonSerializer.Serialize(condensedJson);
            File.WriteAllText(condensedJsonPath, json);

            Console.WriteLine($"Wrote {condensedJson.Count} items to {condensedJsonPath}");
        }

        private async Task<bool> ProcessIds(HttpClient client, List<int> ids)
        {
            string url = "https://api.guildwars2.com/v2/items?ids=" + string.Join(',', ids);

            int maxRetry = 2;
            do
            {
                Console.WriteLine($"Requesting {url}");

                try
                {
                    var result = await client.GetFromJsonAsync<dynamic[]>(url);

                    if (result == null)
                    {
                        Console.WriteLine($"Request failed. Sleeping {RetryDelay}");
                        Thread.Sleep(RetryDelay);
                    }
                    else if (result.Count() != ids.Count)
                    {
                        Console.WriteLine($"Request failed. Count not equal ({result.Count()}, {ids.Count}). Sleeping {RetryDelay}");
                        Thread.Sleep(RetryDelay);
                    }
                    else
                    {
                        for (int i = 0; i < result.Count(); i++)
                        {
                            AllItemsJson[ids[i]] = JsonSerializer.Serialize(result[i]);
                        }
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Request failed: {ex}. Sleeping {RetryDelay}");
                    Thread.Sleep(RetryDelay);
                    continue;
                }
            } while (maxRetry-- > 0);

            return maxRetry > 0;
        }

    }
}
