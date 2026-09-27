using RDR2;
using RDR2.Native;

namespace RDR2ModMenu
{
    // Refilling the horse's needs, like Arthur's: the bar and the core behind it, for health and stamina.
    // The horse is the one he's riding, or otherwise his active horse (the one that comes when whistled for).
    public static class HorseNeeds
    {
        // Attribute core indexes (the same as Arthur's), and their maximum value.
        private const int HealthCore = 0;
        private const int StaminaCore = 1;
        private const int FullCore = 100;

        // Null when he has no horse, or it's dead.
        public static Ped Current
        {
            get
            {
                Ped player = Game.Player.Character;
                Ped horse = player.IsOnMount ? player.CurrentMount : Game.Player.ActiveHorse;
                return horse != null && horse.Exists() && !horse.IsDead ? horse : null;
            }
        }

        public static void RefillHealth(Ped horse)
        {
            horse.Health = horse.MaxHealth;
            ATTRIBUTE._SET_ATTRIBUTE_CORE_VALUE(horse.Handle, HealthCore, FullCore);
        }

        public static void RefillStamina(Ped horse)
        {
            // Adds stamina; adding the maximum fills it.
            PED._CHANGE_PED_STAMINA(horse.Handle, PED._GET_PED_MAX_STAMINA(horse.Handle));
            ATTRIBUTE._SET_ATTRIBUTE_CORE_VALUE(horse.Handle, StaminaCore, FullCore);
        }

        // For the log, e.g. "horse health 300/350, stamina 40/100, cores 100/60".
        public static string Describe()
        {
            Ped horse = Current;
            if (horse == null)
            {
                return "no horse";
            }
            return $"horse health {horse.Health}/{horse.MaxHealth}, stamina {PED._GET_PED_STAMINA(horse.Handle):F0}/{PED._GET_PED_MAX_STAMINA(horse.Handle):F0}, " +
                $"cores {ATTRIBUTE._GET_ATTRIBUTE_CORE_VALUE(horse.Handle, HealthCore)}/" +
                $"{ATTRIBUTE._GET_ATTRIBUTE_CORE_VALUE(horse.Handle, StaminaCore)}";
        }
    }

    // Horse rows: "Refill Horse Health  < Once | Always >", and the same for stamina (see NeedRefill).
    public abstract class HorseNeedRefill : NeedRefill
    {
        protected override string Category => "Horse";

        protected abstract void Refill(Ped horse);

        protected override bool Refill()
        {
            Ped horse = HorseNeeds.Current;
            if (horse == null)
            {
                return false;
            }
            Refill(horse);
            return true;
        }

        protected override string Describe() => HorseNeeds.Describe();
    }

    public class RefillHorseHealth : HorseNeedRefill
    {
        protected override string Need => "Horse health";

        protected override void Refill(Ped horse) => HorseNeeds.RefillHealth(horse);
    }

    public class RefillHorseStamina : HorseNeedRefill
    {
        protected override string Need => "Horse stamina";

        protected override void Refill(Ped horse) => HorseNeeds.RefillStamina(horse);
    }
}
