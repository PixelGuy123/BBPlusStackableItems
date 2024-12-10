using HarmonyLib;
using MTM101BaldAPI.SaveSystem;
using System.IO;
using UnityEngine;
using PixelInternalAPI;
using MTM101BaldAPI.OptionsAPI;

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
			Debug.Log("StackableItems loaded on CoreGameManager");
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

	public class StackableOptionsCat : CustomOptionsCategory
	{
		
		public override void Build()
		{
			if (Singleton<CoreGameManager>.Instance != null)
			{
				CreateText("NoChangingSize", "Opt_StackSizeCheat", Vector3.down * 38f, MTM101BaldAPI.UI.BaldiFonts.ComicSans36, TMPro.TextAlignmentOptions.Center, new(300f, 250f), Color.red);
				return; // No settings in-game
			}

			CreateText("StackSizeIndicator", "Tip_StackSize", Vector3.up * 25f, MTM101BaldAPI.UI.BaldiFonts.ComicSans24, TMPro.TextAlignmentOptions.Center, new(300f, 75f), Color.black);
			var displayText = CreateText("StackSizeDisplay", "Opt_StackSizeDisplay", Vector3.down * 25f, MTM101BaldAPI.UI.BaldiFonts.ComicSans24, TMPro.TextAlignmentOptions.Center, new(300f, 75f), Color.black);
			string localizedText = displayText.text;
			

			AdjustmentBars bar = null; // Workaround to make C# Compiler happy
			bar = CreateBars(() =>
			{
				StackData.maximumStackAllowed = Mathf.Clamp(bar.GetRaw() + StackableItemsPlugin.minStack, StackableItemsPlugin.minStack, StackableItemsPlugin.maxStack);
				displayText.text = $"{localizedText} {StackData.maximumStackAllowed}";
			}, "StackSize", new Vector2(-57f, -66f), StackableItemsPlugin.maxStack - StackableItemsPlugin.minStack);
			bar.SetVal(StackData.maximumStackAllowed);

			displayText.text = $"{localizedText} {StackData.maximumStackAllowed}";
		}
	}
}
