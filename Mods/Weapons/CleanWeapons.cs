using System;
using RDR2;
using RDR2.Native;

namespace RDR2ModMenu
{
    // Weapons button: makes every weapon Arthur is carrying (in his hands, holsters and on his back) spotless and
    // good as new: wear, dirt, soot and visual damage all reset. Weapons stowed on the horse aren't on him, so they're
    // skipped. There's no known native for rust (permanent degradation), so that stays.
    public class CleanWeapons : ModAction
    {
        protected override string Category => "Weapons";

        protected override string Description => "Cleans every weapon you're carrying: wear, dirt, soot and damage. Weapons on your horse aren't included.";

        protected override string Run()
        {
            Ped arthur = Game.Player.Character;
            int cleaned = 0;
            float worstBefore = 0f;

            // Every place a weapon can sit on him (eWeaponAttachPoint); several may hold the same weapon object.
            foreach (eWeaponAttachPoint point in Enum.GetValues(typeof(eWeaponAttachPoint)))
            {
                int attachPoint = (int)point;
                if (attachPoint < 0 || attachPoint >= (int)eWeaponAttachPoint.MaxWeaponAttachPoints)
                {
                    continue;
                }

                int weapon = WEAPON.GET_CURRENT_PED_WEAPON_ENTITY_INDEX(arthur.Handle, attachPoint);
                if (weapon == 0 || !ENTITY.DOES_ENTITY_EXIST(weapon))
                {
                    continue;
                }

                worstBefore = Math.Max(worstBefore, WEAPON.GET_WEAPON_DEGRADATION(weapon));
                WEAPON._SET_WEAPON_DEGRADATION(weapon, 0f);
                WEAPON._SET_WEAPON_DIRT(weapon, 0f, false);
                WEAPON._SET_WEAPON_SOOT(weapon, 0f, false);
                WEAPON._SET_WEAPON_DAMAGE(weapon, 0f, false);
                cleaned++;
            }

            Log.Write($"Cleaned {cleaned} weapon slots; worst wear before was {worstBefore:F2} (0 = clean, 1 = filthy)");
            return cleaned > 0 ? "Weapons cleaned" : "No weapons on you to clean";
        }
    }
}
