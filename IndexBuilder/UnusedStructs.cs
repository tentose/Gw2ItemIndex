using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndexBuilder
{


    public class Item
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ChatLink { get; set; }
        public string IconUrl { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public string Rarity { get; set; }
        public int Level { get; set; }
        public int VendorValue { get; set; }
        public int DefaultSkinId { get; set; }
        public List<string> Flags { get; set; }
        public List<string> GameTypes { get; set; }
        public List<string> Restrictions { get; set; }

    }

    public class ArmorJson
    {
        public string type { get; set; }
        public string weight_class { get; set; }
        public int defense { get; set; }
        public int suffix_item_id { get; set; }
        public string secondary_suffix_item_id { get; set; }

        public int[] stat_choices { get; set; }

    }

    public class BackJson
    {
        public int suffix_item_id { get; set; }
        public string secondary_suffix_item_id { get; set; }

        public int[] stat_choices { get; set; }
    }

    public class BagJson
    {
        public int size { get; set; }
        public bool no_sell_or_sort { get; set; }
    }

    public class ConsumableJson
    {
        public string type { get; set; }
        public string description { get; set; }
        public int duration_ms { get; set; }
        public string unlock_type { get; set; }
        public int color_id { get; set; }
        public int recipe_id { get; set; }
        public int[] extra_recipe_ids { get; set; }
        public int guild_upgrade_id { get; set; }
        public int apply_count { get; set; }
        public string name { get; set; }
        public string icon { get; set; }
        public int[] skins { get; set; }
    }

    public class ContainerJson
    {
        public string type { get; set; }
    }

    public class GatheringJson
    {
        public string type { get; set; }
    }

    public class GizmoJson
    {
        public string type { get; set; }
        public int guild_upgrade_id { get; set; }
        public int[] vendor_ids { get; set; }
    }

    public class MiniatureJson
    {
        public int minipet_id { get; set; }
    }

    public class SalvageKitJson
    {
        public string type { get; set; }
        public int charges { get; set; }
    }

    public class TrinketJson
    {
        public string type { get; set; }
        public int suffix_item_id { get; set; }
        public string secondary_suffix_item_id { get; set; }

        public int[] stat_choices { get; set; }
    }

    public class UpgradeJson
    {
        public string type { get; set; }
        public string[] flags { get; set; }
        public string[] infusion_upgrade_flags { get; set; }
        public string suffix { get; set; }
        public string[]? bonuses { get; set; }
    }

    public class WeaponJson
    {
        public string type { get; set; }
        public string damage_type { get; set; }
        public int min_power { get; set; }
        public int max_power { get; set; }
        public int defense { get; set; }
        public int suffix_item_id { get; set; }
        public string secondary_suffix_item_id { get; set; }
        public int[] stat_choices { get; set; }

    }
}
