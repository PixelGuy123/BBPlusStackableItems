using HarmonyLib;
using TMPro;
using UnityEngine;
using MTM101BaldAPI;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine.UI;

namespace StackableItems.Patches
{
	[ConditionalPatchNoMod("mtm101.rulerp.baldiplus.endlessfloors")] // No stack feature on Jhonny's Shop, since it's Upgrade Shop now
	[HarmonyPatch(typeof(StoreScreen))]
	internal static class StoreScreenPatch
	{
		[HarmonyPatch("Start")]
		[HarmonyPostfix]
		static void SetupShop(StoreScreen __instance, ItemObject[] ___inventory)
		{
			guis = new TextMeshProUGUI[StackData.i.itemStacks.Length];

			var slots = __instance.transform.Find("Canvas").Find("ItemSlots");
			AddTextToSlot(slots.Find("ItemSlot"), StackData.i.itemStacks[0], 0);

			for (int i = 1; i <= 4; i++)
				AddTextToSlot(slots.Find($"ItemSlot ({i})"), StackData.i.itemStacks[i], i);
			itemStacks = new int[___inventory.Length];

			for (int i = 0; i < StackData.i.itemStacks.Length; i++)
				itemStacks[i] = StackData.i.itemStacks[i];


			static void AddTextToSlot(Transform slot, int stackVal, int stackIdx)
			{
				var mesh = new GameObject("StackIndicator").AddComponent<TextMeshProUGUI>();
				mesh.alignment = TextAlignmentOptions.Center;
				mesh.color = Color.black;
				mesh.gameObject.layer = LayerMask.NameToLayer("UI");


				mesh.transform.SetParent(slot);
				mesh.transform.localPosition = new(10f, -7.9455f);
				mesh.transform.localScale = Vector3.one * 0.7f;
				mesh.text = stackVal < 1 ? string.Empty : stackVal.ToString();
				guis[stackIdx] = mesh;
			}
		}

		[HarmonyPatch("ClickInventory")]
		[HarmonyPostfix]
		static void SwitchItemStackDisplay(int val, bool ___dragging, int ___slotDragging)
		{
			if (___dragging)
				draggingStack = itemStacks[val];
			else if (draggingStack >= 0)
			{
				itemStacks[___slotDragging] = itemStacks[val];
				itemStacks[val] = draggingStack;
				draggingStack = -1;
			}
			if (val < guis.Length)
				guis[val].Text(___dragging || itemStacks[val] <= 0 ? string.Empty : itemStacks[val].ToString());
			if (___slotDragging < guis.Length)
				guis[___slotDragging].Text(___dragging || itemStacks[___slotDragging] <= 0 ? string.Empty : itemStacks[___slotDragging].ToString());
		}

		[HarmonyPatch("Exit")]
		[HarmonyPostfix]
		static void UpdateStackInfo()
		{
			StackData.i.itemStacks = itemStacks.NewStack(StackData.i.itemStacks.Length);
			StackData.i.SaveItemStack();
		}

		[HarmonyPatch("BuyItem")]
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> ReplaceBaseItemAdditionMethod(IEnumerable<CodeInstruction> i) =>
			new CodeMatcher(i)
			.MatchForward(false, 
				new(OpCodes.Ldc_I4_0),
				new(OpCodes.Stloc_0)
				)
			.RemoveInstructions(59) // remove a huge ass while loop
			.InsertAndAdvance(
				new(OpCodes.Ldarg_0),
				new(OpCodes.Ldarg_1),
				new(OpCodes.Ldarg_0),
				CodeInstruction.LoadField(typeof(StoreScreen), "inventory"),
				new(OpCodes.Ldarg_0),
				CodeInstruction.LoadField(typeof(StoreScreen), "itemForSale"),
				new(OpCodes.Ldarg_0),
				CodeInstruction.LoadField(typeof(StoreScreen), "inventoryImage"),
				new(OpCodes.Ldarg_0),
				CodeInstruction.LoadField(typeof(StoreScreen), "counterHotSpots"),
				Transpilers.EmitDelegate<System.Action<StoreScreen, int, ItemObject[], ItemObject[], Image[], GameObject[]>>((store, val, inventory, sale, images, counterspots) =>
				{
					// Basically add the item to the inventory if there's already an item in there for stacking
					if (StackableItemsPlugin.prohibitedItemsForStack.Contains(sale[val].itemType)) // Just reimplement the while loop from the store here
					{
						StoreAddItem(val, inventory, sale, images, counterspots);
						return;
					}

					for (int i = 0; i < inventory.Length; i++) // First for loop will just try to find a stackable item to stack
					{
						if (i != 5 && inventory[i].itemType == sale[val].itemType && itemStacks[i] < StackData.maximumStackAllowed)
						{
							itemStacks[i]++;
							if (i < guis.Length)
								guis[i].Text(itemStacks[i].ToString());
							return;
							
						}
					}

					StoreAddItem(val, inventory, sale, images, counterspots);

				})
				)

			.InstructionEnumeration();
		

		static void StoreAddItem(int val, ItemObject[] inventory, ItemObject[] sale, Image[] images, GameObject[] counterspots)
		{
			int i = 0;
			while (i < inventory.Length)
			{
				if (i != 5 && inventory[i].itemType == Items.None)
				{
					inventory[i] = sale[val];
					images[i].sprite = inventory[i].itemSpriteSmall;
					itemStacks[i] = StackableItemsPlugin.prohibitedItemsForStack.Contains(sale[val].itemType) ? 0 : 1;
					if (i > 5)
					{
						counterspots[i - 6].SetActive(true);
						images[i].gameObject.SetActive(true);
						break;
					}
					else if (itemStacks[i] > 0)
						guis[i].Text(itemStacks[i].ToString());

					break;
				}
				else
				{
					i++;
				}
			}
		}

		static void Text(this TextMeshProUGUI mesh, string text)
		{
			mesh.text = text;
			mesh.autoSizeTextContainer = false;
			mesh.autoSizeTextContainer = true; // Just like that to update the container >:(
		}

		static TextMeshProUGUI[] guis;

		static int[] itemStacks;

		static int draggingStack = -1;
	}
}
