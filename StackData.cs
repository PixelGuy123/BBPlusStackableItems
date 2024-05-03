using UnityEngine;
using System;

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

		public static int maximumStackAllowed = 5;

		internal static int[] cacheFromSaveItems = null;
	}

	public static class StackDataExtensions
	{
		public static int GetStackFromSelItem(this ItemManager man) =>
			StackData.i.itemStacks[man.selectedItem];
			
		public static bool IsItemAllowed(this Items item) =>
			!StackableItemsPlugin.prohibitedItemsForStack.Contains(item);
	}
}
