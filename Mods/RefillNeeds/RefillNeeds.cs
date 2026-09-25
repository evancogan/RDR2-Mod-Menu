using RDR2;
using RDR2.Native;

namespace AIPlayground
{
    // Menu action: fills Arthur's health, stamina and Dead Eye, both the bars and the cores behind them.
    public class RefillNeeds : ModAction
    {
        protected override string Description => "Fills Arthur's health, stamina and Dead Eye, bars and cores.";

        // Attribute core indexes, and their maximum value.
        private const int HealthCore = 0;
        private const int StaminaCore = 1;
        private const int DeadEyeCore = 2;
        private const int FullCore = 100;

        protected override string Run()
        {
            Ped arthur = Game.Player.Character;
            int player = Game.Player.Handle;
            string before = Describe(arthur);

            arthur.Health = arthur.MaxHealth;
            PLAYER.RESTORE_PLAYER_STAMINA(player, 100f);
            PLAYER._SPECIAL_ABILITY_RESTORE_BY_AMOUNT(player, 100f, 0, 0, 1);

            ATTRIBUTE._SET_ATTRIBUTE_CORE_VALUE(arthur.Handle, HealthCore, FullCore);
            ATTRIBUTE._SET_ATTRIBUTE_CORE_VALUE(arthur.Handle, StaminaCore, FullCore);
            ATTRIBUTE._SET_ATTRIBUTE_CORE_VALUE(arthur.Handle, DeadEyeCore, FullCore);

            Log.Write($"Before: {before}. After: {Describe(arthur)}");
            return "Health, stamina and Dead Eye refilled";
        }

        private static string Describe(Ped arthur)
        {
            return $"health {arthur.Health}/{arthur.MaxHealth}, stamina {PED._GET_PED_STAMINA_NORMALIZED(arthur.Handle):P0}, " +
                $"cores {ATTRIBUTE._GET_ATTRIBUTE_CORE_VALUE(arthur.Handle, HealthCore)}/" +
                $"{ATTRIBUTE._GET_ATTRIBUTE_CORE_VALUE(arthur.Handle, StaminaCore)}/" +
                $"{ATTRIBUTE._GET_ATTRIBUTE_CORE_VALUE(arthur.Handle, DeadEyeCore)}";
        }
    }
}
