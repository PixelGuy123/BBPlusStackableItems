﻿using HarmonyLib;
using MTM101BaldAPI.SaveSystem;
using System.IO;
using UnityEngine;

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
			int[] nums = new int[reader.ReadInt32()];
			for (int i = 0; i < nums.Length; i++)
				nums[i] = Mathf.Min(StackData.maximumStackAllowed, reader.ReadInt32());
			StackData.i.itemStacks = nums;
		}

		public override void Reset()
		{
		}

		public override void OnCGMCreated(CoreGameManager instance, bool isFromSavedGame)
		{
			base.OnCGMCreated(instance, isFromSavedGame);
			instance.gameObject.AddComponent<StackData>().itemStacks = StackData.cacheFromSaveItems == null || !isFromSavedGame ? new int[templateSize] : StackData.cacheFromSaveItems;
			StackData.cacheFromSaveItems = null;
		}

		const int templateSize = 5;
	}
}
