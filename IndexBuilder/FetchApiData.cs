using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IndexBuilder
{
    public class CondensedItem
    {
        public string Name { get; set; }
        public string IconUrl { get; set; }
        public Rarity Rarity { get; set; }
        public ItemType Type { get; set; }

        // Optional Fields
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public ArmorType ArmorType { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public ConsumableType ConsumableType { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public ContainerType ContainerType { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public GatheringType GatheringType { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public GizmoType GizmoType { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public TrinketType TrinketType { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public UpgradeType UpgradeType { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public WeaponType WeaponType { get; set; }
    }

    public enum Rarity
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

    public enum ItemType
    {
        Unknown,
        Armor,
        Back,
        Bag,
        Consumable,
        Container,
        CraftingMaterial,
        Gathering,
        Gizmo,
        Key,
        MiniPet,
        Tool,
        Trait,
        Trinket,
        Trophy,
        UpgradeComponent,
        Weapon,
        JadeBotCore,
        JadeBotChip,
        Fishing,
    }

    public enum ArmorType
    {
        Unknown,
        Boots,
        Coat,
        Gloves,
        Helm,
        HelmAquatic,
        Leggings,
        Shoulders,
    }

    public enum ConsumableType
    {
        Unknown,
        AppearanceChange,
        Booze,
        ContractNpc,
        Currency,
        Food,
        Generic,
        Halloween,
        Immediate,
        MountRandomUnlock,
        RandomUnlock,
        Transmutation,
        Unlock,
        UpgradeRemoval,
        Utility,
        TeleportToFriend,
    }

    public enum ContainerType
    {
        Unknown,
        Default,
        GiftBox,
        Immediate,
        OpenUI,
    }

    public enum GatheringType
    {
        Unknown,
        Foraging,
        Logging,
        Mining,
    }

    public enum GizmoType
    {
        Unknown,
        Default,
        ContainerKey,
        RentableContractNpc,
        UnlimitedConsumable,
    }

    public enum TrinketType
    {
        Unknown,
        Accessory,
        Amulet,
        Ring,
    }

    public enum UpgradeType
    {
        Unknown,
        Default,
        Gem,
        Rune,
        Sigil,
    }

    public enum WeaponType
    {
        Unknown,
        Axe, Dagger, Mace, Pistol, Scepter, Sword,
        Focus, Shield, Torch, Warhorn,
        Greatsword, Hammer, LongBow, Rifle, ShortBow, Staff,
        Harpoon, Speargun, Trident,
        LargeBundle, SmallBundle, Toy, ToyTwoHanded
    }

    internal class FetchApiData
    {
        const int RetryDelay = 61 * 1000;
        Dictionary<int, string> AllItemsJson = new Dictionary<int, string>();
        string ItemsJsonCachePath;

        public FetchApiData(string itemsJsonCachePath)
        {
            ItemsJsonCachePath = itemsJsonCachePath;
        }

        public async Task FetchAll(string lang)
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
                        bool succeeded = await ProcessIds(client, lang, idsToRead);
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

                if (idsToRead.Count > 0 && await ProcessIds(client, lang, idsToRead))
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

        public T TypeStringToType<T>(string str) where T : struct,Enum
        {
            if (!Enum.TryParse<T>(str, out var value))
            {
                // Exceptions
                if (str == "Qux" && value is ItemType)
                {
                    return (T)(object)ItemType.JadeBotCore;
                }
                else if (str == "Quux" && value is ItemType)
                {
                    return (T)(object)ItemType.JadeBotChip;
                }
                else if (str == "Foo" && value is GatheringType)
                {
                    return (T)(object)ItemType.Fishing;
                }
                throw new Exception("Unhandled type");
            }
            return value;
        }

        public async Task Condense(string condensedJsonPath)
        {
            Dictionary<int, CondensedItem> condensedJson = new Dictionary<int, CondensedItem>();

            foreach (var kv in AllItemsJson)
            {
                var rawItem = JsonSerializer.Deserialize<ItemJson>(kv.Value);
                var condensedItem = new CondensedItem();
                condensedItem.Name = rawItem.name;
                condensedItem.IconUrl = rawItem.icon;
                condensedItem.Rarity = RarityStringToRarity(rawItem.rarity);
                condensedItem.Type = TypeStringToType<ItemType>(rawItem.type);

                switch (condensedItem.Type)
                {
                    case ItemType.Armor:
                        {
                            var rawSubItem = JsonSerializer.Deserialize<ArmorJson>(rawItem.details);
                            condensedItem.ArmorType = TypeStringToType<ArmorType>(rawSubItem.type);
                        }
                        break;
                    case ItemType.Consumable:
                        {
                            var rawSubItem = JsonSerializer.Deserialize<ConsumableJson>(rawItem.details);
                            condensedItem.ConsumableType = TypeStringToType<ConsumableType>(rawSubItem.type);
                        }
                        break;
                    case ItemType.Container:
                        {
                            var rawSubItem = JsonSerializer.Deserialize<ContainerJson>(rawItem.details);
                            condensedItem.ContainerType = TypeStringToType<ContainerType>(rawSubItem.type);
                        }
                        break;
                    case ItemType.Gathering:
                        {
                            var rawSubItem = JsonSerializer.Deserialize<GatheringJson>(rawItem.details);
                            condensedItem.GatheringType = TypeStringToType<GatheringType>(rawSubItem.type);
                        }
                        break;
                    case ItemType.Gizmo:
                        {
                            var rawSubItem = JsonSerializer.Deserialize<GizmoJson>(rawItem.details);
                            condensedItem.GizmoType = TypeStringToType<GizmoType>(rawSubItem.type);
                        }
                        break;
                    case ItemType.Trinket:
                        {
                            var rawSubItem = JsonSerializer.Deserialize<TrinketJson>(rawItem.details);
                            condensedItem.TrinketType = TypeStringToType<TrinketType>(rawSubItem.type);
                        }
                        break;
                    case ItemType.UpgradeComponent:
                        {
                            var rawSubItem = JsonSerializer.Deserialize<UpgradeJson>(rawItem.details);
                            condensedItem.UpgradeType = TypeStringToType<UpgradeType>(rawSubItem.type);
                        }
                        break;
                    case ItemType.Weapon:
                        {
                            var rawSubItem = JsonSerializer.Deserialize<WeaponJson>(rawItem.details);
                            condensedItem.WeaponType = TypeStringToType<WeaponType>(rawSubItem.type);
                        }
                        break;
                }

                condensedJson.Add(kv.Key, condensedItem);
            }

            string json = JsonSerializer.Serialize(condensedJson);
            File.WriteAllText(condensedJsonPath, json);

            Console.WriteLine($"Wrote {condensedJson.Count} items to {condensedJsonPath}");
        }

        private async Task<bool> ProcessIds(HttpClient client, string lang, List<int> ids)
        {
            string url = $"https://api.guildwars2.com/v2/items?lang={lang}&ids=" + string.Join(',', ids);

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
