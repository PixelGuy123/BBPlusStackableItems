using PixelInternalAPI.Extensions;

namespace StackableItems
{
	public static class Extensions
	{
		public static int HasStackableItem(this ItemManager itm, ItemObject itb)
		{
			if (!itb.itemType.IsItemAllowed())
				return -1;

			int i = itm.selectedItem;
			int max = itm.maxItem + 1;
			//Debug.Log("----- item to add: " + itb.itemType + " << at starting slot: " + i);
			for (int z = 0; z <= itm.maxItem; z++)
			{
				//Debug.Log("current slot: " + i + " >> " + itm.items[i].itemType);
				if (!itm.IsSlotLocked(i) && StackData.i.IsStackWithinLimit(i) && itm.items[i].itemType == itb.itemType)
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
	}
}
