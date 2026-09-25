using System;
using System.Linq;
using RDR2;
using RDR2.Native;

namespace RDR2ModMenu
{
    // Weapons button: fills Arthur's ammunition and reloads the gun in his hand.
    // Standard ammo for every gun type is always filled. Special ammo (express, split point, explosive and so on) is
    // only topped up if he already carries some, so it doesn't hand him ammo he never bought. Throwables are skipped.
    public class RefillAmmo : ModAction
    {
        protected override string Category => "Weapons";

        protected override string Description => "Fills ammo for every gun and bow, tops up special ammo you carry, and reloads your gun.";

        // More than any satchel holds; the game caps it at the maximum for each type.
        private const int Full = 9999;

        private static readonly eAmmoType[] StandardAmmo =
        {
            eAmmoType.Revolver, eAmmoType.Pistol, eAmmoType.Repeater, eAmmoType.Rifle,
            eAmmoType.Shotgun, eAmmoType.Ammo22, eAmmoType.Arrow,
        };

        private static readonly string[] GunAndBowPrefixes = { "Revolver", "Pistol", "Repeater", "Rifle", "Shotgun", "Ammo22", "Arrow" };

        protected override string Run()
        {
            Ped arthur = Game.Player.Character;
            int before = Count(arthur, eAmmoType.Revolver);

            int filled = 0;
            foreach (eAmmoType type in Enum.GetValues(typeof(eAmmoType)).Cast<eAmmoType>())
            {
                string name = type.ToString();
                if (!GunAndBowPrefixes.Any(prefix => name.StartsWith(prefix)))
                {
                    continue;
                }
                if (!StandardAmmo.Contains(type) && Count(arthur, type) == 0)
                {
                    continue;
                }
                WEAPON.SET_PED_AMMO_BY_TYPE(arthur.Handle, (uint)type, Full);
                filled++;
            }

            WEAPON._REFILL_AMMO_IN_CURRENT_PED_WEAPON(arthur.Handle);

            Log.Write($"Filled {filled} ammo types. Revolver ammo {before} -> {Count(arthur, eAmmoType.Revolver)}");
            return "Ammo refilled";
        }

        private static int Count(Ped ped, eAmmoType type) => WEAPON.GET_PED_AMMO_BY_TYPE(ped.Handle, (uint)type);
    }
}
