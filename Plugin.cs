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
using UnityEngine;
using MTM101BaldAPI.AssetTools;
using System.IO;
using PixelInternalAPI.Classes;
using UnityEngine.AI;

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

			hasAnimationsMod = Chainloader.PluginInfos.ContainsKey("pixelguy.pixelmodding.baldiplus.newanimations");
			ModPath = AssetLoader.GetModPath(this);

			LoadingEvents.RegisterOnAssetsLoaded(AddTrashCansInEverything, false);

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
			ResourceManager.AddReloadLevelCallback((man, nextlevel) =>
			{
				if (nextlevel) StackData.i.SaveItemStack();
				else StackData.i.TryLoadPrevItemStack();
			});
        }

		void TryAddProhibitedItem(string moddedItem)
		{
			try
			{
				prohibitedItemsForStack.Add(EnumExtensions.GetFromExtendedName<Items>(moddedItem));
			}
			catch { }
		}

		void AddTrashCansInEverything()
		{
			// setup for trash can
			var trash = ObjectCreationExtensions.CreateSpriteBillboard(AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromFile(Path.Combine(ModPath, "trashcan.png")), 75f)).AddSpriteHolder(3f, 0);
			var trashHolder = trash.transform.parent;
			trashHolder.name = "TrashCan";

			var collider = trashHolder.gameObject.AddComponent<BoxCollider>();
			collider.size = new(horizontalSize, 1f, horizontalSize);

			var colliderAI = trashHolder.gameObject.AddComponent<NavMeshObstacle>();
			colliderAI.size = collider.size + Vector3.up * 5f;

			var trashAcceptor = new GameObject("TrashcanAcceptor").AddComponent<TrashcanComponent>();
			trashAcceptor.gameObject.layer = LayerStorage.iClickableLayer;
			collider = trashAcceptor.gameObject.AddComponent<BoxCollider>();
			collider.size = new Vector3(horizontalSize, 14f, horizontalSize);

			trashAcceptor.audMan = trashHolder.gameObject.CreatePropagatedAudioManager(55f, 75f).SetAudioManagerAsPrefab();

			trashAcceptor.audThrow = ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromFile(Path.Combine(ModPath, "throwTrash.wav")), string.Empty, SoundType.Voice, Color.white);
			trashAcceptor.audThrow.subtitle = false; // no subs

			trashAcceptor.transform.SetParent(trashHolder);
			trashAcceptor.transform.localPosition = Vector3.up * 5f;

			trashAcceptor.renderer = trash.transform;

			trashHolder.gameObject.SetAsPrefab().AddAsGeneratorPrefab();

			TrashCanSpawnFunction.trashCan = trashHolder.gameObject;

			GenericExtensions.FindResourceObjects<RoomAsset>().DoIf(x => x.category == RoomCategory.Class || x.category == RoomCategory.Office || x.category == RoomCategory.Faculty, 
				x => x.AddRoomFunction<TrashCanSpawnFunction>());
		}

		public static void AddModdedProhibitedItem(Items item) => // If mods wanna add items to be non stackable (in very specific cases, such as quarter pouch that convert some items into its own currency)
			prohibitedItemsForStack.Add(item);

		internal static HashSet<Items> prohibitedItemsForStack = [Items.None];

		internal static string ModPath = string.Empty;

		internal static bool hasAnimationsMod = false;

		const float horizontalSize = 1.5f;
    }
}
