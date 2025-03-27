using MTM101BaldAPI.Registers;
using PixelInternalAPI.Extensions;
using System.Collections.Generic;

namespace StackableItems
{
	public static class Extensions
	{
		public static int HasStackableItem(this ItemManager itm, ItemObject itb)
		{ 
			if (!itb.IsItemAllowed())
				return -1;

			int i = itm.selectedItem;
			int max = itm.maxItem + 1;
			//Debug.Log("----- item to add: " + itb.itemType + " << at starting slot: " + i);
			for (int z = 0; z <= itm.maxItem; z++)
			{
				//Debug.Log("current slot: " + i + " >> " + itm.items[i].itemType);
				if (!itm.IsSlotLocked(i) && StackData.i.IsStackWithinLimit(i) && itm.items[i] == itb)
					return i;
				i = (i + 1) % max;
			}
			return -1;
		}

		public static int[] NewStack(this int[] targetStack) => //System.Array.Copy DOESN'T WORK, WTF
			targetStack.NewStack(targetStack.Length);

		public static int[] NewStack(this int[] targetStack, int length)
		{
			var stack = new int[length];
			for (int i = 0; i < length; i++)
				stack[i] = targetStack[i];
			return stack;
		}

		public static void UpdateStackOrganization(this ItemManager itm, int idx)
		{
			ItemObject item = itm.items[idx];
			if (item.itemType == Items.None) return; // Skip if nothing there

			List<KeyValuePair<int, int>> foundSize = [];
			int max = itm.maxItem + 1;
			int index = idx;
			for (int i = 0; i <= max; i++)
			{
				index = (index + 1) % max;
				if (idx != index && itm.items[index].itemType == item.itemType)
					foundSize.Add(new(index, StackData.i.itemStacks[index]));

			}
			if (foundSize.Count == 0) return;

			foreach (var size in foundSize)
			{

				int addition = size.Value + StackData.i.itemStacks[idx];
				if (addition <= StackData.maximumStackAllowed)
				{
					itm.SetItem(itm.nothing, size.Key);
					StackData.i.itemStacks[idx] = addition;
				}
				else
				{
					int required = StackData.maximumStackAllowed - StackData.i.itemStacks[idx];
					StackData.i.itemStacks[size.Key] -= required;
					StackData.i.itemStacks[idx] += required;
				}

			}
			itm.UpdateSelect();

		}

		public static bool IsInventoryReallyFull(this ItemManager itm, Items item)
		{
			if (!itm.InventoryFull()) return false;
			for (int i = 0; i <= itm.maxItem; i++)
			{
				if (itm.items[i].itemType == item && StackData.i.IsStackWithinLimit(i))
					return false;
			}
				
			
			return true;
		}

		public static ItemObject[] ToAllItmValues(this ItemMetaData[] metas)
		{
			List<ItemObject> itms = [];
			for (int i = 0; i < metas.Length; i++)
				itms.AddRange(metas[i].itemObjects);
			return [.. itms];
		}
	}
}
