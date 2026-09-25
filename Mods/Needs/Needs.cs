using RDR2;
using RDR2.Native;

namespace RDR2ModMenu
{
    // Refilling Arthur's needs, shared by the refill actions and Keep Needs Filled.
    // Each need has a bar (what drains in the moment) and a core behind it (the ring that drains over time).
    public static class Needs
    {
        // Attribute core indexes, and their maximum value.
        private const int HealthCore = 0;
        private const int StaminaCore = 1;
        private const int DeadEyeCore = 2;
        private const int FullCore = 100;

        public static void RefillHealth()
        {
            Ped arthur = Game.Player.Character;
            arthur.Health = arthur.MaxHealth;
            ATTRIBUTE._SET_ATTRIBUTE_CORE_VALUE(arthur.Handle, HealthCore, FullCore);
        }

        public static void RefillStamina()
        {
            PLAYER.RESTORE_PLAYER_STAMINA(Game.Player.Handle, 100f);
            ATTRIBUTE._SET_ATTRIBUTE_CORE_VALUE(Game.Player.Character.Handle, StaminaCore, FullCore);
        }

        public static void RefillDeadEye()
        {
            PLAYER._SPECIAL_ABILITY_RESTORE_BY_AMOUNT(Game.Player.Handle, 100f, 0, 0, 1);
            ATTRIBUTE._SET_ATTRIBUTE_CORE_VALUE(Game.Player.Character.Handle, DeadEyeCore, FullCore);
        }

        public static void RefillAll()
        {
            RefillHealth();
            RefillStamina();
            RefillDeadEye();
        }

        // For the log: bars and cores, e.g. "health 379/450, stamina 8%, cores 100/0/100".
        public static string Describe()
        {
            Ped arthur = Game.Player.Character;
            return $"health {arthur.Health}/{arthur.MaxHealth}, stamina {PED._GET_PED_STAMINA_NORMALIZED(arthur.Handle):P0}, " +
                $"cores {ATTRIBUTE._GET_ATTRIBUTE_CORE_VALUE(arthur.Handle, HealthCore)}/" +
                $"{ATTRIBUTE._GET_ATTRIBUTE_CORE_VALUE(arthur.Handle, StaminaCore)}/" +
                $"{ATTRIBUTE._GET_ATTRIBUTE_CORE_VALUE(arthur.Handle, DeadEyeCore)}";
        }
    }
}
