using RDR2;
using RDR2.Native;

namespace AIPlayground
{
    // A scrolling row directly on the Actions page, "Body Type  < Skinny | Medium | Fat >".
    //
    // Uses the game's own body-weight system: equipping one of the body-weight outfits from pedattributes.ymt
    // (eBodyWeightOutfit) with _EQUIP_META_PED_OUTFIT, then refreshing his components and variation. That's what
    // refits his clothes to his new shape; the first version morphed the body directly (MetaPed expressions), which
    // left clothes clipping until a cutscene. Those expressions are reset to neutral here in case they're still set.
    // Hashes from the _EQUIP_META_PED_OUTFIT notes in alloc8or's rdr3-nativedb, listed smallest to biggest.
    public class BodyType : ModChoice
    {
        protected override string DisplayName => "Body Type";

        protected override string Description => "Left/Right: skinny, medium or fat. Uses the game's own weight system, so his clothes refit.";

        protected override string[] Choices => new[] { "Skinny", "Medium", "Fat" };

        protected override int InitialChoice => 1;

        // eBodyWeightOutfit, smallest to biggest: index 0 is the thinnest, 10 the default, 20 the heaviest.
        private static readonly int[] WeightOutfits =
        {
            -2045421226, -1745814259, -325933489, -1065791927, -844699484, -1273449080, 927185840,
            149872391, 399015098, -644349862, 1745919061, 1004225511, 1278600348, 502499352,
            -2093198664, -1837436619, 1736416063, 2040610690, -1173634986, -867801909, 1960266524,
        };

        // Which weight outfit each choice uses.
        private static readonly int[] ChoiceOutfit = { 0, 10, 20 };

        // MetaPed body expressions the first version set (waist, hips/stomach, chest, back, arms, forearms,
        // shoulders, thighs, calves, neck), reset to neutral so they don't stack on the weight outfit.
        private static readonly int[] OldBodyExpressions = { 50460, 49787, 27779, 41478, 46032, 8420, 7010, 64834, 42067, 36277 };

        private const ulong SET_ACTIVE_META_PED_COMPONENTS_UPDATED = 0xAAB86462966168CE;

        protected override string Apply(int index)
        {
            Ped arthur = Game.Player.Character;

            foreach (int expression in OldBodyExpressions)
            {
                PED._SET_PED_FACE_FEATURE(arthur.Handle, expression, 0f);
            }

            int outfit = ChoiceOutfit[index];
            PED._EQUIP_META_PED_OUTFIT(arthur.Handle, unchecked((uint)WeightOutfits[outfit]));

            // Reload his components so clothes fit the new body, then redraw him.
            Function.Call(SET_ACTIVE_META_PED_COMPONENTS_UPDATED, arthur.Handle, false);
            PED._UPDATE_PED_VARIATION(arthur.Handle, false, true, true, true, false);

            Log.Write($"Equipped body-weight outfit #{outfit + 1} of {WeightOutfits.Length}");
            return $"Body type: {Choices[index]}";
        }
    }
}
