using UnityEngine;

public static class MasterDictionary
{
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
}
