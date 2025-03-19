using MTM101BaldAPI.Registers;
using UnityEngine;

namespace StackableItems
{
	public class StackData : MonoBehaviour
	{
		void Awake()
		{
			i = this;
		}

		public bool IsStackWithinLimit(int index) =>
			itemStacks[index] < maximumStackAllowed;
		
		void OnDestroy()
		{
			cacheFromSaveItems = itemStacks.NewStack();
		}

		public void SaveItemStack()
		{
			if (itemStacks != null)
				previousItemStacks = itemStacks.NewStack();
			
		}
		public void TryLoadPrevItemStack()
		{
			if (previousItemStacks != null)
				itemStacks = previousItemStacks.NewStack();
		}


		[SerializeField]
		public int[] itemStacks;

		int[] previousItemStacks = null;

		public static StackData i;

		public static int maximumStackAllowed;

		internal static int[] cacheFromSaveItems = null;
	}

	public static class StackDataExtensions
	{
		public static int GetStackFromSelItem(this ItemManager man) =>
			StackData.i.itemStacks[man.selectedItem];

		public static bool IsItemAllowed(this ItemObject item)
		{
			if (item.itemType == Items.None || StackableItemsPlugin.prohibitedItemsForStack.Contains(item))
				return false;
			var meta = item.GetMeta();
			return meta == null || !meta.flags.HasFlag(ItemFlags.MultipleUse) || !meta.tags.Contains(StackableItemsPlugin.notAllowStackTag);
		}

		public static bool IsItemFullyIgnored(this ItemObject item)
		{
			var meta = item.GetMeta();
			return meta != null && (meta.flags.HasFlag(ItemFlags.InstantUse) || meta.tags.Contains(StackableItemsPlugin.fullyIgnoreItemTag));
		}
	}
}
