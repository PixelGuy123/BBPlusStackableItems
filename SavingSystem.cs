using HarmonyLib;
using MTM101BaldAPI.SaveSystem;
using System.IO;
using UnityEngine;
using PixelInternalAPI;

namespace StackableItems
{
	public class SaveItemStack(BepInEx.PluginInfo info) : ModdedSaveGameIOBinary
	{
		readonly BepInEx.PluginInfo Info = info;
		public override BepInEx.PluginInfo pluginInfo => Info;
		public override void Save(BinaryWriter writer)
		{ 
			writer.Write(StackData.i.itemStacks.Length);
			StackData.i.itemStacks.Do(writer.Write);
		}

		public override void Load(BinaryReader reader)
		{
			try
			{
				int[] nums = new int[reader.ReadInt32()];
				for (int i = 0; i < nums.Length; i++)
					nums[i] = Mathf.Min(StackData.maximumStackAllowed, reader.ReadInt32());
				StackData.cacheFromSaveItems = nums;
			}
			catch
			{
				ResourceManager.RaiseLocalizedPopup(Info, "Er_StackLoad");
				StackData.cacheFromSaveItems = null;
			}
		}

		public override void Reset()
		{
		}

		public override void OnCGMCreated(CoreGameManager instance, bool isFromSavedGame)
		{
			base.OnCGMCreated(instance, isFromSavedGame);
			var comp = instance.gameObject.AddComponent<StackData>();
			if (StackData.cacheFromSaveItems != null && isFromSavedGame)
			{
				comp.itemStacks = StackData.cacheFromSaveItems.NewStack();
				StackData.cacheFromSaveItems = null;
				return;
			}
			comp.itemStacks = new int[99]; // There can't be more than 99 slots in a freaking screen, right?
			StackData.cacheFromSaveItems = null;
		}
	}
}
