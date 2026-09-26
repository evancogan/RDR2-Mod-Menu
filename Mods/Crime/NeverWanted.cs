using System;
using System.Collections.Generic;
using RDR2;
using RDR2.Native;

namespace RDR2ModMenu
{
    // Crime mod: while it's on, the law never comes after Arthur and nobody witnesses his crimes.
    //
    // The first version only disabled the wanted level and cleared it twice a second as a backstop. The law kept
    // noticing crimes, so it flipped between chasing and giving up (and the HUD with it). Instead, this stops crimes
    // being noticed and answered at all:
    // - everyone around him is flagged so they can't witness crimes (PCF_CantWitnessCrimes), a few times a second to
    //   catch people as they appear
    // - witnesses are stopped from calling the law, every frame
    // - law dispatch is disabled, even for crimes that do get witnessed or reported
    // - disturbance crimes (brawling, noise) aren't registered
    // If witnesses appear anyway, the incident is cleared as a backstop. Any pursuit already under way is cleared once
    // when it's turned on. Bounties aren't touched.
    //
    // Tried and dropped: SUPPRESS_CRIME_THIS_FRAME for every crime type (its last three arguments are undocumented;
    // with zeros the game never counted a crime as suppressed, and witnesses still appeared).
    public class NeverWanted : ModScript
    {
        protected override string Category => "Crime";

        protected override string Description => "The law never comes after you and nobody witnesses your crimes while it's on. Bounties aren't touched.";

        // CPED config flag 146 (femga's rdr3_discoveries, AI/CPED_CONFIG_FLAGS).
        private const int CantWitnessCrimes = 146;

        // How often new people are flagged.
        private const int FlagIntervalMs = 250;

        // People this mod flagged, so turning it off only unflags those (the game may flag some itself, e.g. in missions).
        // Anyone the game has since unloaded is dropped each pass: their flag went with them, and the game may reuse
        // their handle for someone new.
        private readonly HashSet<int> flagged = new HashSet<int>();
        private int nextFlag;
        private bool witnessesSeen;

        protected override bool OnEnable()
        {
            int player = Game.Player.Handle;
            SetLawIgnoring(player, true);
            LAW.CLEAR_WANTED_SCORE(player);
            LAW._SET_BOUNTY_HUNTER_PURSUIT_CLEARED();
            return true;
        }

        protected override void OnEnabledTick()
        {
            int player = Game.Player.Handle;
            PLAYER.SUPPRESS_WITNESSES_CALLING_POLICE_THIS_FRAME(player);
            FlagPeople();
            ClearWitnesses(player);
        }

        private void FlagPeople()
        {
            int now = Environment.TickCount;
            if (now < nextFlag)
            {
                return;
            }
            nextFlag = now + FlagIntervalMs;

            int arthur = Game.Player.Character.Handle;
            int added = 0;
            var present = new HashSet<int>();
            foreach (Ped ped in World.GetAllPeds())
            {
                int handle = ped.Handle;
                present.Add(handle);
                if (handle != arthur && !PED.GET_PED_CONFIG_FLAG(handle, CantWitnessCrimes, true))
                {
                    PED.SET_PED_CONFIG_FLAG(handle, CantWitnessCrimes, true);
                    flagged.Add(handle);
                    added++;
                }
            }
            flagged.IntersectWith(present);
            if (added > 0)
            {
                Log.Write($"Made {added} more people unable to witness crimes ({flagged.Count} around now)");
            }
        }

        // Backstop: if witnesses show up anyway, clear the incident they're witnessing, once each time they appear.
        private void ClearWitnesses(int player)
        {
            bool active = LAW.ARE_WITNESSES_ACTIVE(player);
            bool pending = LAW._ARE_WITNESSES_PENDING(player);
            bool witnesses = active || pending;
            if (witnesses && !witnessesSeen)
            {
                uint crime = LAW._GET_HUD_PLAYER_CRIME_TYPE(player);
                LAW.CLEAR_WANTED_SCORE(player);
                Log.Write($"Witnesses appeared anyway (active {active}, pending {pending}, crime 0x{crime:X8}); cleared the incident. " +
                    $"Still there: active {LAW.ARE_WITNESSES_ACTIVE(player)}, pending {LAW._ARE_WITNESSES_PENDING(player)}");
            }
            witnessesSeen = witnesses;
        }

        protected override void OnDisable()
        {
            SetLawIgnoring(Game.Player.Handle, false);

            int restored = 0;
            foreach (int handle in flagged)
            {
                if (ENTITY.DOES_ENTITY_EXIST(handle) && ENTITY.IS_ENTITY_A_PED(handle))
                {
                    PED.SET_PED_CONFIG_FLAG(handle, CantWitnessCrimes, false);
                    restored++;
                }
            }
            flagged.Clear();
            Log.Write($"People can witness crimes again ({restored} unflagged)");
        }

        private static void SetLawIgnoring(int player, bool ignoring)
        {
            LAW._SET_LAW_DISABLED(ignoring);
            LAW.SET_DISABLE_DISTURBANCE_CRIMES(player, ignoring);
            PLAYER._SET_DISABLE_PLAYER_WANTED_LEVEL(player, ignoring);
        }
    }
}
