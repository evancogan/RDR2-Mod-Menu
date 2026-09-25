using RDR2;

namespace RDR2ModMenu
{
    // TEMPORARY Player button: shows the honor value stored in Rockstar's script memory, READ ONLY.
    //
    // There's no native for honor; the story scripts keep it in a script global whose address changes between game
    // versions. Halen84's RDR3-Script-Global-Research (g_savedGlobals, Global_40) reads it at Global_40 + 11095 + 35.
    // Before anything writes there, this confirms the address holds honor on this game version (1.0.1491.50) by
    // comparing the number with the in-game honor meter. Writing to a wrong address could crash the game or corrupt
    // a save, so there's deliberately no write here.
    public class CheckHonor : ModAction
    {
        protected override string DisplayName => "Check Honor (test)";

        protected override string Category => "Player";

        protected override string Description => "Test only: shows the honor value the mod can see, so it can be compared with your in-game honor. Changes nothing.";

        private const int HonorGlobal = 40 + 11095 + 35;

        protected override string Run()
        {
            int honor = Game.GetGlobalPtr(HonorGlobal).GetInt();
            Log.Write($"Honor global {HonorGlobal} reads {honor}");
            return $"Honor value: {honor}";
        }
    }
}
