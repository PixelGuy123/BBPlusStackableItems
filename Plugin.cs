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
using TMPro;
using PixelInternalAPI.Components;
using MTM101BaldAPI.OptionsAPI;
using System.Collections;
using StackableItems.Patches;

namespace StackableItems
{
    [BepInPlugin("pixelguy.pixelmodding.baldiplus.stackableitems", PluginInfo.PLUGIN_NAME, "1.0.3")]
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

			LoadingEvents.RegisterOnAssetsLoaded(Info, AddTrashCansInEverything(), false);

			LoadingEvents.RegisterOnAssetsLoaded(Info, LoadLimitations(), true);

			ModdedSaveGame.AddSaveHandler(new SaveItemStack(Info)); // Save stack properly
			ResourceManager.AddReloadLevelCallback((man, nextlevel) =>
			{
				if (nextlevel) StackData.i.SaveItemStack();
				else StackData.i.TryLoadPrevItemStack();
			});

			CustomOptionsCore.OnMenuInitialize += OnMen; // Copypaste from QuarterPouch lmao. I can't figure out the OptionsMenuAPI rn.
			ModdedSaveSystem.AddSaveLoadAction(this, (bool isSave, string myPath) =>
			{
				string p = Path.Combine(myPath, stackFileName);
				if (isSave)
				{
					File.WriteAllText(p, StackData.maximumStackAllowed.ToString());
					return;
				}
				else if (File.Exists(p))
				{
					if (int.TryParse(File.ReadAllText(p), out int res))
						StackData.maximumStackAllowed = res;
					else ResourceManager.RaiseLocalizedPopup(Info, "Er_StackConfigLoad");
					return;
				}
				StackData.maximumStackAllowed = 3;
			});
        }

		const string stackFileName = "stackMaxSize.txt";

		void TryAddProhibitedItem(string moddedItem)
		{
			try
			{
				prohibitedItemsForStack.Add(EnumExtensions.GetFromExtendedName<Items>(moddedItem));
			}
			catch { }
		}

		IEnumerator LoadLimitations()
		{
			yield return 1;
			yield return "Registering unstackable items...";
			prohibitedItemsForStack.AddRange(MTM101BaldiDevAPI.itemMetadata.GetAllWithFlags(ItemFlags.MultipleUse) // Get all items with MultipleUse, so they can't be stackable
					.ToValues()
					.Select(x => x.itemType));
			itemsToFullyIgnore.AddRange(MTM101BaldiDevAPI.itemMetadata.GetAllWithFlags(ItemFlags.InstantUse) // Get all items with InstantUse, so they can't be stackable (neither just not work)
				.ToValues()
				.Select(x => x.itemType));

			if (Chainloader.PluginInfos.ContainsKey("pixelguy.pixelmodding.baldiplus.bbpluslockers")) // BBPlusLockers support
				TryAddProhibitedItem("Lockpick");

			if (Chainloader.PluginInfos.ContainsKey("pixelguy.pixelmodding.baldiplus.bbextracontent")) // BBTimesSupport support
				TryAddProhibitedItem("Present");

			yield break;
		}

		IEnumerator AddTrashCansInEverything()
		{
			yield return 2;
			// setup for trash can
			yield return "Creating trash can prefab...";
			var trash = ObjectCreationExtensions.CreateSpriteBillboard(AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromFile(Path.Combine(ModPath, "trashcan.png")), 75f)).AddSpriteHolder(3f, LayerStorage.iClickableLayer);
			var trashHolder = trash.transform.parent;
			trashHolder.name = "TrashCan";

			var trashCol = new GameObject("TrashCollider");
			trashCol.transform.SetParent(trashHolder);
			trashCol.transform.localPosition = Vector3.zero;

			var collider = trashCol.gameObject.AddComponent<BoxCollider>();
			collider.size = new(horizontalSize, 1f, horizontalSize);

			var colliderAI = trashCol.gameObject.AddComponent<NavMeshObstacle>();
			colliderAI.size = collider.size + Vector3.up * 5f;
			colliderAI.carving = true;

			var trashAcceptor = trashHolder.gameObject.AddComponent<TrashcanComponent>();
			collider = trashAcceptor.gameObject.AddComponent<BoxCollider>();
			collider.size = new Vector3(horizontalSize, 14f, horizontalSize);

			trashAcceptor.audMan = trashHolder.gameObject.CreatePropagatedAudioManager(55f, 75f);

			trashAcceptor.audThrow = ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromFile(Path.Combine(ModPath, "throwTrash.wav")), string.Empty, SoundType.Voice, Color.white);
			trashAcceptor.audThrow.subtitle = false; // no subs

			trashAcceptor.renderer = trash.transform;

			var trashIndicator = new GameObject("TrashUsesIndicator").AddComponent<TextMeshPro>();
			trashIndicator.alignment = TextAlignmentOptions.Center;
			trashIndicator.gameObject.layer = LayerStorage.billboardLayer;
			trashIndicator.transform.SetParent(trashHolder);

			trashIndicator.gameObject.AddComponent<BillboardRotator>();

			trashAcceptor.usesRender = trashIndicator;

			trashHolder.gameObject.ConvertToPrefab(true);

			TrashCanSpawnFunction.trashCan = trashHolder.gameObject;
			List<RoomCategory> allowedCats = [RoomCategory.Class, RoomCategory.Office, RoomCategory.Faculty];

			GenericExtensions.FindResourceObjects<RoomAsset>().DoIf(x => x.type == RoomType.Room && allowedCats.Contains(x.category), 
				x => { x.AddRoomFunctionToContainer<TrashCanSpawnFunction>(); allowedCats.Remove(x.category); });

			// load slot
			yield return "Loading slot sprite...";
			StoreScreenPatch.slot = AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromFile(Path.Combine(ModPath, "cyanSlot.png")), 35f);

			yield break;
		}

		void OnMen(OptionsMenu instance)
		{
			if (Singleton<CoreGameManager>.Instance != null) return; // No settings in-game
			GameObject ob = CustomOptionsCore.CreateNewCategory(instance, "Opt_StackItm");
			TextLocalizer TL = CustomOptionsCore.CreateText(instance, new Vector2(-5.45f, 65.03f), "Opt_StackSize");

			var text = CustomOptionsCore.CreateText(instance, new Vector2(-14.83f, -16.41f), $"{Singleton<LocalizationManager>.Instance.GetLocalizedText("Opt_StackSizeDisplay")} {StackData.maximumStackAllowed}");
			stackBar = CustomOptionsCore.CreateAdjustmentBar(instance, new Vector2(31.6f, -61.2f), "StackSize", 5, "Tip_StackSize", StackData.maximumStackAllowed, () =>
			{
				StackData.maximumStackAllowed = Mathf.Clamp(stackBar.GetRaw() + 2, 2, 7);
				text.textBox.text = $"{Singleton<LocalizationManager>.Instance.GetLocalizedText("Opt_StackSizeDisplay")} {StackData.maximumStackAllowed}";
			});
			// attach everything to the options menu
			stackBar.transform.SetParent(ob.transform, false);
			text.transform.SetParent(ob.transform, false);
			TL.transform.SetParent(ob.transform, false);
		}

		static AdjustmentBars stackBar;
		public static HashSet<Items> NonStackableItems => prohibitedItemsForStack;

		internal static HashSet<Items> prohibitedItemsForStack = [Items.None];

		internal static HashSet<Items> itemsToFullyIgnore = [];

		internal static string ModPath = string.Empty;

		internal static bool hasAnimationsMod = false;

		const float horizontalSize = 1.5f;
    }
}
