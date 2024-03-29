﻿using System;
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
        public string ChatCode { get; set; }
        public Rarity Rarity { get; set; }
        public ItemType Type { get; set; }

        // Optional Fields
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public ItemSubType SubType { get; set; }
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
        PowerCore,
        JadeTechModule,
        Relic,
    }

    public enum ItemSubType
    {

        Unknown = 0,
        Armor_Boots = 100,
        Armor_Coat,
        Armor_Gloves,
        Armor_Helm,
        Armor_HelmAquatic,
        Armor_Leggings,
        Armor_Shoulders,
        Consumable_AppearanceChange = 200,
        Consumable_Booze,
        Consumable_ContractNpc,
        Consumable_Currency,
        Consumable_Food,
        Consumable_Generic,
        Consumable_Halloween,
        Consumable_Immediate,
        Consumable_MountRandomUnlock,
        Consumable_RandomUnlock,
        Consumable_Transmutation,
        Consumable_Unlock,
        Consumable_UpgradeRemoval,
        Consumable_Utility,
        Consumable_TeleportToFriend,
        Container_Default = 300,
        Container_GiftBox,
        Container_Immediate,
        Container_OpenUI,
        Gathering_Foraging = 400,
        Gathering_Logging,
        Gathering_Mining,
        Gathering_Fishing,
        Gathering_Bait,
        Gathering_Lure,
        Gizmo_Default = 500,
        Gizmo_ContainerKey,
        Gizmo_RentableContractNpc,
        Gizmo_UnlimitedConsumable,
        Trinket_Accessory = 600,
        Trinket_Amulet,
        Trinket_Ring,
        UpgradeComponent_Default = 700,
        UpgradeComponent_Gem,
        UpgradeComponent_Rune,
        UpgradeComponent_Sigil,
        Weapon_Axe = 800,
        Weapon_Dagger,
        Weapon_Mace,
        Weapon_Pistol,
        Weapon_Scepter,
        Weapon_Sword,
        Weapon_Focus,
        Weapon_Shield,
        Weapon_Torch,
        Weapon_Warhorn,
        Weapon_Greatsword,
        Weapon_Hammer,
        Weapon_LongBow,
        Weapon_Rifle,
        Weapon_ShortBow,
        Weapon_Staff,
        Weapon_Harpoon,
        Weapon_Speargun,
        Weapon_Trident,
        Weapon_LargeBundle,
        Weapon_SmallBundle,
        Weapon_Toy,
        Weapon_ToyTwoHanded,
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
                throw new Exception("Unhandled type");
            }
            return value;
        }

        public ItemSubType GetItemSubType(ItemType type, string subTypeStr)
        {
            string typeStr = type.ToString();
            string fullSubTypeStr = $"{typeStr}_{subTypeStr}";

            if (!Enum.TryParse(fullSubTypeStr, out ItemSubType subType))
            {
                if (type == ItemType.Gathering && subTypeStr == "Foo")
                {
                    subType = ItemSubType.Gathering_Fishing;
                }
                else
                {
                    throw new Exception("Unhandled type");
                    subType = ItemSubType.Unknown;
                }
            }

            return subType;
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
                condensedItem.ChatCode = rawItem.chat_link;
                condensedItem.Rarity = RarityStringToRarity(rawItem.rarity);
                condensedItem.Type = TypeStringToType<ItemType>(rawItem.type);

                switch (condensedItem.Type)
                {
                    case ItemType.Armor:
                        {
                            var rawSubItem = JsonSerializer.Deserialize<ArmorJson>(rawItem.details);
                            condensedItem.SubType = GetItemSubType(condensedItem.Type, rawSubItem.type);
                        }
                        break;
                    case ItemType.Consumable:
                        {
                            var rawSubItem = JsonSerializer.Deserialize<ConsumableJson>(rawItem.details);
                            condensedItem.SubType = GetItemSubType(condensedItem.Type, rawSubItem.type);
                        }
                        break;
                    case ItemType.Container:
                        {
                            var rawSubItem = JsonSerializer.Deserialize<ContainerJson>(rawItem.details);
                            condensedItem.SubType = GetItemSubType(condensedItem.Type, rawSubItem.type);
                        }
                        break;
                    case ItemType.Gathering:
                        {
                            var rawSubItem = JsonSerializer.Deserialize<GatheringJson>(rawItem.details);
                            condensedItem.SubType = GetItemSubType(condensedItem.Type, rawSubItem.type);
                        }
                        break;
                    case ItemType.Gizmo:
                        {
                            var rawSubItem = JsonSerializer.Deserialize<GizmoJson>(rawItem.details);
                            condensedItem.SubType = GetItemSubType(condensedItem.Type, rawSubItem.type);
                        }
                        break;
                    case ItemType.Trinket:
                        {
                            var rawSubItem = JsonSerializer.Deserialize<TrinketJson>(rawItem.details);
                            condensedItem.SubType = GetItemSubType(condensedItem.Type, rawSubItem.type);
                        }
                        break;
                    case ItemType.UpgradeComponent:
                        {
                            var rawSubItem = JsonSerializer.Deserialize<UpgradeJson>(rawItem.details);
                            condensedItem.SubType = GetItemSubType(condensedItem.Type, rawSubItem.type);
                        }
                        break;
                    case ItemType.Weapon:
                        {
                            var rawSubItem = JsonSerializer.Deserialize<WeaponJson>(rawItem.details);
                            condensedItem.SubType = GetItemSubType(condensedItem.Type, rawSubItem.type);
                        }
                        break;
                }

                condensedJson.Add(kv.Key, condensedItem);
            }

            string json = JsonSerializer.Serialize(condensedJson);
            File.WriteAllText(condensedJsonPath, json);

            Console.WriteLine($"Wrote {condensedJson.Count} items to {condensedJsonPath}");
        }

        public class QuickItem
        {
            public string Name { get; set; }
            public string IconUrl { get; set; }
        }

        public async Task CondenseForQuickItems(string condensedJsonPath)
        {
            var condensedJson = new Dictionary<int, QuickItem>();

            foreach (var kv in AllItemsJson)
            {
                var rawItem = JsonSerializer.Deserialize<ItemJson>(kv.Value);
                var condensedItem = new QuickItem();
                condensedItem.Name = rawItem.name;
                condensedItem.IconUrl = rawItem.icon;

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
