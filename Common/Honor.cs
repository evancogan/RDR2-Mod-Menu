using RDR2;

namespace RDR2ModMenu
{
    // Arthur's honor. There's no native for it: the story scripts keep it in a script global, g_savedGlobals (Global_40)
    // + 11095 + 35 (Halen84's RDR3-Script-Global-Research), and Halen84's decompiled scripts for this game version
    // (1491.50) change it at exactly that address (func_40 in rob_register.c).
    //
    // The game clamps honor to -240..240 until a point in the story unlocks -320..320: bit 6 of g_savedGlobals'
    // featureUnlocked, a script array whose size sits at Global_40 + 7857 with its first element (bits 0-30) right after
    // it, checked by func_58(6) in the same code.
    public static class Honor
    {
        private const int HonorGlobal = 40 + 11095 + 35;
        private const int FeatureUnlockedGlobal = 40 + 7857 + 1;
        private const int WiderRangeFeature = 6;

        public static int Value
        {
            get => Game.GetGlobalPtr(HonorGlobal).GetInt();
            set => Game.GetGlobalPtr(HonorGlobal).SetInt(value);
        }

        // How far honor can go either way at this point in the story.
        public static int Limit => (Game.GetGlobalPtr(FeatureUnlockedGlobal).GetInt() & (1 << WiderRangeFeature)) != 0 ? 320 : 240;
    }
}
