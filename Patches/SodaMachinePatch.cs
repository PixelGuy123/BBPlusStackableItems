﻿using HarmonyLib;
using System.Collections;

namespace StackableItems.Patches
{
    [HarmonyPatch(typeof(SodaMachine))]
    internal class SodaMachinePatch
    {

        [HarmonyPatch("InsertItem")]
        private static bool Prefix(SodaMachine __instance, PlayerManager pm, ItemObject ___item) // This should fix the quarter issue
        {
            if (pm.itm.GetStackFromSelItem() > 1 && ((___item == null && pm.itm.InventoryFull()) || pm.itm.IsInventoryReallyFull(___item.itemType)))
            {
                __instance.StartCoroutine(Delay(pm.itm, pm.itm.items[pm.itm.selectedItem]));
                return false;
            }

            return true;
        }

        static IEnumerator Delay(ItemManager itm, ItemObject itb)
        {
            yield return null;
            itm.AddItem(itb);
            yield break;
        }
    }
}
