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
			cacheFromSaveItems = itemStacks;
		}

		[SerializeField]
		public int[] itemStacks;

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
