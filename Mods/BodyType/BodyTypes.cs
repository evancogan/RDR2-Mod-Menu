using System.Linq;
using RDR2;
using RDR2.Native;

namespace AIPlayground
{
    // Actions submenu buttons that reshape Arthur's body: skinny, medium or fat.
    //
    // Uses the game's MetaPed expressions (_SET_CHAR_EXPRESSION, named _SET_PED_FACE_FEATURE in V2), which morph body
    // parts by a value from -1.0 to 1.0, then refreshes his appearance. The expression IDs come from the community list
    // linked in alloc8or's rdr3-nativedb. They're documented for the online character; they're expected to work on
    // Arthur, and the game's own weight system (eating and so on) may shift his shape again later.
    public static class BodyShape
    {
        private const int Waist = 50460;
        private const int HipsAndStomach = 49787;
        private const int Chest = 27779;
        private const int BackWidth = 41478;
        private const int Arms = 46032;
        private const int Forearms = 8420;
        private const int ShoulderThickness = 7010;
        private const int Thighs = 64834;
        private const int Calves = 42067;
        private const int Neck = 36277;

        private static readonly int[] Parts = { Waist, HipsAndStomach, Chest, BackWidth, Arms, Forearms, ShoulderThickness, Thighs, Calves, Neck };

        // Each preset's value per part, in the same order as Parts.
        public static readonly float[] Skinny = { -1f, -1f, -0.6f, -0.4f, -0.7f, -0.5f, -0.4f, -0.6f, -0.5f, -0.4f };
        public static readonly float[] Medium = { 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f };
        public static readonly float[] Fat = { 1f, 1f, 0.6f, 0.5f, 0.6f, 0.4f, 0.4f, 0.7f, 0.5f, 0.7f };

        public static void Apply(float[] preset)
        {
            Ped arthur = Game.Player.Character;
            string before = Describe(arthur);

            for (int i = 0; i < Parts.Length; i++)
            {
                PED._SET_PED_FACE_FEATURE(arthur.Handle, Parts[i], preset[i]);
            }
            PED._UPDATE_PED_VARIATION(arthur.Handle, false, true, true, true, false);

            Log.Write($"Body before: {before}. After: {Describe(arthur)}");
        }

        private static string Describe(Ped arthur)
        {
            return string.Join(", ", Parts.Select(part => $"{part}={PED._GET_PED_FACE_FEATURE(arthur.Handle, part):F2}"));
        }
    }

    public class BodyTypeSkinny : ModAction
    {
        protected override string DisplayName => "Body Type: Skinny";

        protected override string Description => "Makes Arthur skinny.";

        protected override string Run()
        {
            BodyShape.Apply(BodyShape.Skinny);
            return "Arthur is now skinny";
        }
    }

    public class BodyTypeMedium : ModAction
    {
        protected override string DisplayName => "Body Type: Medium";

        protected override string Description => "Returns Arthur's body to a neutral, medium build.";

        protected override string Run()
        {
            BodyShape.Apply(BodyShape.Medium);
            return "Arthur is now a medium build";
        }
    }

    public class BodyTypeFat : ModAction
    {
        protected override string DisplayName => "Body Type: Fat";

        protected override string Description => "Makes Arthur fat.";

        protected override string Run()
        {
            BodyShape.Apply(BodyShape.Fat);
            return "Arthur is now fat";
        }
    }
}
