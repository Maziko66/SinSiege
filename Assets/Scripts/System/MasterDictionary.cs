using UnityEngine;

public static class MasterDictionary
{
    public enum GameLanguage
    {
        English,            // en
        Turkish,            // tr
        French,             // fr
        Italian,            // it
        German,             // de
        Portuguese,         // pt
        Russian,            // ru
        Polish,             // pl
        Korean,             // kr
        Japanese,           // jp
        ChineseSimplified,  // zh
        ChineseTraditional  // tc
    }
    public enum Characters
    {
        Shotgunman,
        Scoundrel,
        Firemage,
        Noble,
        Papa,
        Detective,
        Jester,
        Scientist
    }
    
    public enum UpgradeRarity
    {
        Common,
        Uncommon,
        Rare,
        Mythical,
        Legendary,
        Unique,
        Demonic
    }
    
    public struct RarityColors
    {
        public static readonly Color Common = new Color(176, 195, 217);
        public static readonly Color Uncommon = new Color(94, 152, 217);
        public static readonly Color Rare = new Color(75, 105, 255);
        public static readonly Color Mythical = new Color(136, 71, 255);
        public static readonly Color Legendary = new Color(211, 44, 230);
        public static readonly Color Unique = new Color(173, 229, 92);
        public static readonly Color Demonic = new Color(235, 75, 75);
    }
    
    private static readonly Color[] rarityColors =
    {
        new Color32(176, 195, 217, 255),
        new Color32(94, 152, 217, 255),
        new Color32(75, 105, 255, 255),
        new Color32(136, 71, 255, 255),
        new Color32(211, 44, 230, 255),
        new Color32(173, 229, 92, 255),
        new Color32(235, 75, 75, 255)
    };

    public static Color GetRarityColor(UpgradeRarity rarity) => rarityColors[(int)rarity];
}
