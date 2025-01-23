using HarmonyLib;
using TMPro;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;
using UnityEngine;

namespace StackableItems.Patches
{
    [HarmonyPatch(typeof(ItemManager))]
    public class ItemManagerPatches // Make stackables possible
    {

		[HarmonyPatch("AddItem", [typeof(ItemObject)])]
		[HarmonyReversePatch(HarmonyReversePatchType.Original)]
		public static void AddItemOg(object instance, ItemObject item) =>
			throw new System.NotImplementedException("stub");

        [HarmonyPatch(typeof(HudManager), "SetItemSelect")]
        [HarmonyPostfix]
        private static void OverrideItemSelectionName(ref TMP_Text ___itemTitle)
        {
			if (___itemTitle == null) return;

			var pm = Singleton<CoreGameManager>.Instance.GetPlayer(0);
            if (pm && pm.itm.items[pm.itm.selectedItem].IsItemAllowed()) // Somehow pm.itm can be unassigned
               ___itemTitle.text += $"\n({pm.itm.GetStackFromSelItem()})"; // Just add the stack val
            
        }

		[HarmonyPatch("RemoveItemSlot", [typeof(int)])]
		[HarmonyPrefix]
		static void UpdateStackSize(int val, int ___maxItem)
		{
			overrideSetItemPatch = true;
			for (int i = val; i < ___maxItem; i++)
			{
				//Debug.Log($"Setting slot ({i}) to value ({StackData.i.itemStacks[i + 1]}) of index {i + 1}");
				StackData.i.itemStacks[i] = StackData.i.itemStacks[i + 1]; // Just follows the logic that the game uses to remove one slot
																			   // Adds two because the stack is decreased naturally, so it remains the same stack size
			}
			StackData.i.itemStacks[___maxItem] = 0;
		}

		[HarmonyPatch("RemoveItemSlot", [typeof(int)])]
		[HarmonyPostfix]
		static void RevertSetItemPatchOverride(int val, int ___maxItem) =>
			overrideSetItemPatch = false;
		

		[HarmonyPatch("RemoveItem", [typeof(int)])]
        [HarmonyPrefix]
        private static bool OverrideRemoveItemWithStacks(ItemManager __instance, int val)
        {
            if (!__instance.items[val].IsItemAllowed())
                return true;
			if (--StackData.i.itemStacks[val] <= 0)
            {
				StackData.i.itemStacks[val] = 0;

				return true;
            }
			__instance.UpdateSelect();
			__instance.UpdateStackOrganization(val);

            return false;
        }

        [HarmonyPatch("AddItem", [typeof(ItemObject)])]
        [HarmonyPrefix]
        private static bool OverrideItemAdd(ItemManager __instance, ItemObject item, out Items? __state)
        {
			if (StackableItemsPlugin.itemsToFullyIgnore.Contains(item))
			{
				__state = null;
				return true;
			}

			__state = __instance.items[__instance.selectedItem].itemType;
			int idx = __instance.HasStackableItem(item);
            if (idx != -1)
            {
				StackData.i.itemStacks[idx]++;
				__instance.UpdateSelect();
				return false;
            }

			int i = __instance.selectedItem;
			int max = __instance.maxItem + 1;

			for (int z = 0; z <= __instance.maxItem; z++) // Check for empty slots
			{
				if (__instance.items[i].itemType == Items.None)
				{
					__instance.SetItem(item, i);
					Singleton<CoreGameManager>.Instance.GetHud(__instance.pm.playerNumber).inventory.CollectItem(i, item);
					return false;
				}
				i = (i + 1) % max;
			}

			 i = __instance.selectedItem;
			 max = __instance.maxItem + 1;

			for (int z = 0; z <= __instance.maxItem; z++) // Check for non empty slots
			{
				if (StackData.i.itemStacks[i] <= 1)
				{
					__instance.SetItem(item, i);
					Singleton<CoreGameManager>.Instance.GetHud(__instance.pm.playerNumber).inventory.CollectItem(i, item);
					return false;
				}
				i = (i + 1) % max;
			}


			return true;
        }

		[HarmonyPatch("AddItem", [typeof(ItemObject)])]
		[HarmonyPostfix]
		private static void FixStackCountIfNeeded(ItemManager __instance, Items? __state)
		{
			if (__state == null) return;

			if (__instance.items[__instance.selectedItem].itemType != __state)
			{
				StackData.i.itemStacks[__instance.selectedItem] = 1; // Fix count
				__instance.UpdateSelect();
			}
		}

		[HarmonyPatch("AddItem", [typeof(ItemObject)])]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> IncludeStackUsage(IEnumerable<CodeInstruction> i) => // Basically just add 1 to the stack lmao
            new CodeMatcher(i)
            .MatchForward(true,
                new(OpCodes.Ldarg_0),
                new(CodeInstruction.LoadField(typeof(ItemManager), "selectedItem")),
                new(OpCodes.Stloc_0)
                )
            .Advance(1)

            .InsertAndAdvance( // add a stack adder here
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldloc_0),
                new(OpCodes.Call, _addtostack)
                )

            .MatchForward(true,
                new(OpCodes.Ldarg_0),
                new(CodeInstruction.LoadField(typeof(ItemManager), "items")),
                new(OpCodes.Ldloc_2),
                new(OpCodes.Ldarg_1),
                new(OpCodes.Stelem_Ref)
                )
            .Advance(1)

            .InsertAndAdvance( // add a stack adder here
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldloc_2),
                new(OpCodes.Call, _addtostack)
                )

            .MatchForward(false, // Goes to second num = something
                new(OpCodes.Ldarg_0),
                new(CodeInstruction.LoadField(typeof(ItemManager), "selectedItem")),
                new(OpCodes.Stloc_0)
                )

            .InsertAndAdvance( // add a stack adder here
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldarg_0),
                CodeInstruction.LoadField(typeof(ItemManager), "selectedItem"),
                new(OpCodes.Call, _addtostack)
                )

            .InstructionEnumeration();

        static void AddToStack(ItemManager man, int val)
        {
			if (man.items[val].IsItemAllowed() && StackData.i.IsStackWithinLimit(val))
				StackData.i.itemStacks[val]++;
		}

        readonly static MethodInfo _addtostack = AccessTools.Method(typeof(ItemManagerPatches), "AddToStack", [typeof(ItemManager), typeof(int)]);

        [HarmonyPatch("AddItem", [typeof(ItemObject), typeof(Pickup)])]
        [HarmonyPrefix]
        private static bool StopThisIfSelectingStackedSelection(ItemManager __instance, ItemObject item, Pickup pickup, ref bool __result)
        {
			if (StackableItemsPlugin.itemsToFullyIgnore.Contains(item)) return true;

			if (!__instance.InventoryFull())
                return true;

            int idx = __instance.HasStackableItem(item);

            if (idx != -1)
            {
				StackData.i.itemStacks[idx]++;
				__instance.UpdateSelect();
				__result = true;
				return false;
            }
            int i = __instance.selectedItem;
            int max = __instance.maxItem + 1;

            for (int z = 0; z <= __instance.maxItem; z++)
            {
                if (StackData.i.itemStacks[i] <= 1)
                {
                    if (__instance.items[i].itemType != Items.None)
                        pickup.AssignItem(__instance.items[i]);

                    __instance.selectedItem = i;
                    __instance.SetItem(item, i);
					Singleton<CoreGameManager>.Instance.GetHud(__instance.pm.playerNumber).inventory.CollectItem(i, item);
					__result = true;
					return false;
                }
                i = (i + 1) % max;
            }


            pickup.AssignItem(item); // Just assign itself if everything fails lol

            return false;
        }

		[HarmonyPatch("SetItem")]
		[HarmonyPrefix]
		private static void StopThisIfSelectingStackedSelection(ItemObject item, int slot)
		{
			if (!overrideSetItemPatch)
				StackData.i.itemStacks[slot] = item.IsItemAllowed() ? 1 : 0; // Reset it since it it being set
		}

		internal static bool overrideSetItemPatch = false;
    }

    [HarmonyPatch(typeof(HudManager), "SetItemSelect")]
    internal class FixItemSelectName
    {
        private static void Prefix(ref string key, out string __state)
        {
            string[] split = key.Split(' ');
            key = split[0]; // Keys can't have spaces, right?
            __state = split.Length > 1 ? split[1] : null;

        }

        private static void Postfix(ref TMP_Text ___itemTitle, string __state)
        {
            if (___itemTitle != null && __state != null)
                ___itemTitle.text += ' ' + __state; // After key thing, put back the text left
        }

    }
}
