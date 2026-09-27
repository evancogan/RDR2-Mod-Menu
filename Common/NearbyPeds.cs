using System;
using System.Collections.Generic;
using RDR2.Math;
using RDR2.Native;

namespace RDR2ModMenu
{
    // Finds the people (and horses and other animals) near a point with the game's own query, instead of V2's
    // World.GetAllPeds.
    //
    // World.GetAllPeds relies on ScriptHookRDR2's worldGetAllPeds, which can start failing partway through a session
    // and keep failing; V2 then quietly returns an empty array (Halen84/ScriptHookRDR2DotNet-V2 issue #2). It happened
    // here: from one reload on, every mod found nobody, in the middle of Saint Denis, so Never Wanted stopped working.
    //
    // This uses _GET_ENTITIES_NEAR_POINT with entity type 1 (people, as the game's own scripts use it) into an itemset,
    // a list the game fills in. Each user keeps one NearbyPeds and must Dispose it when it stops, so itemsets aren't
    // leaked across reloads.
    public sealed class NearbyPeds : IDisposable
    {
        private const int PedType = 1;

        private int itemset;

        public List<int> Find(Vector3 center, float radius)
        {
            if (itemset == 0 || !ITEMSET.IS_ITEMSET_VALID(itemset))
            {
                itemset = ITEMSET.CREATE_ITEMSET(true);
            }

            var peds = new List<int>();
            int count = ENTITY._GET_ENTITIES_NEAR_POINT(center, radius, itemset, PedType);
            for (int i = 0; i < count; i++)
            {
                int ped = ITEMSET.GET_INDEXED_ITEM_IN_ITEMSET(i, itemset);
                if (ped != 0)
                {
                    peds.Add(ped);
                }
            }
            ITEMSET._CLEAR_ITEMSET(itemset);
            return peds;
        }

        public void Dispose()
        {
            if (itemset != 0 && ITEMSET.IS_ITEMSET_VALID(itemset))
            {
                ITEMSET.DESTROY_ITEMSET(itemset);
            }
            itemset = 0;
        }
    }
}
