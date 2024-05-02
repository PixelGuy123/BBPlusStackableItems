using BepInEx;
using HarmonyLib;
using MTM101BaldAPI.Registers;
using System.Collections.Generic;
using MTM101BaldAPI;
using System.Linq;
using BepInEx.Bootstrap;
using MTM101BaldAPI.SaveSystem;
using PixelInternalAPI.Extensions;
using PixelInternalAPI;

namespace StackableItems
{
    [BepInPlugin("pixelguy.pixelmodding.baldiplus.stackableitems", PluginInfo.PLUGIN_NAME, "1.0.0")]
	[BepInDependency("mtm101.rulerp.bbplus.baldidevapi", BepInDependency.DependencyFlags.HardDependency)]
	[BepInDependency("pixelguy.pixelmodding.baldiplus.pixelinternalapi", BepInDependency.DependencyFlags.HardDependency)]
	public class StackableItemsPlugin : BaseUnityPlugin
    {
        private void Awake()
        {
			Harmony h = new("pixelguy.pixelmodding.baldiplus.stackableitems");
			h.PatchAllConditionals();

			LoadingEvents.RegisterOnAssetsLoaded(() =>
			{
				prohibitedItemsForStack.AddRange(MTM101BaldiDevAPI.itemMetadata.GetAllWithFlags(ItemFlags.MultipleUse) // Get all items with MultipleUse, so they can't be stackable
					.ToValues()
					.Select(x => x.itemType));

				prohibitedItemsForStack.AddRange(MTM101BaldiDevAPI.itemMetadata.GetAllWithFlags(ItemFlags.NoInventory) // Get all items with NoInventory, so they can't be stackable
					.ToValues()
					.Select(x => x.itemType));

				if (Chainloader.PluginInfos.ContainsKey("pixelguy.pixelmodding.baldiplus.bbpluslockers")) // BBPlusLockers support
					TryAddProhibitedItem("Lockpick");

				if (Chainloader.PluginInfos.ContainsKey("pixelguy.pixelmodding.baldiplus.bbextracontent")) // BBTimesSupport support
					TryAddProhibitedItem("Present");
			}
			, true);

			ModdedSaveGame.AddSaveHandler(new SaveItemStack(Info)); // Save stack properly
        }

		

		void TryAddProhibitedItem(string moddedItem)
		{
			try
			{
				prohibitedItemsForStack.Add(EnumExtensions.GetFromExtendedName<Items>(moddedItem));
			}
			catch { }
		}

		public static void AddModdedProhibitedItem(Items item) => // If mods wanna add items to be non stackable (in very specific cases, such as quarter pouch that convert some items into its own currency)
			prohibitedItemsForStack.Add(item);

		internal static HashSet<Items> prohibitedItemsForStack = [Items.None];
    }
}
