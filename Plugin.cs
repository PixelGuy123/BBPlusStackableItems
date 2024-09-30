using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.OptionsAPI;
using MTM101BaldAPI.Registers;
using MTM101BaldAPI.SaveSystem;
using PixelInternalAPI;
using PixelInternalAPI.Classes;
using PixelInternalAPI.Components;
using PixelInternalAPI.Extensions;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

namespace StackableItems
{
	[BepInPlugin("pixelguy.pixelmodding.baldiplus.stackableitems", PluginInfo.PLUGIN_NAME, "1.0.5.3")]
	[BepInDependency("mtm101.rulerp.bbplus.baldidevapi", BepInDependency.DependencyFlags.HardDependency)]
	[BepInDependency("pixelguy.pixelmodding.baldiplus.pixelinternalapi", BepInDependency.DependencyFlags.HardDependency)]

	[BepInDependency("pixelguy.pixelmodding.baldiplus.bbpluslockers", BepInDependency.DependencyFlags.SoftDependency)]

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
						StackData.maximumStackAllowed = Mathf.Clamp(res, minStack, maxStack);
					else ResourceManager.RaiseLocalizedPopup(Info, "Er_StackConfigLoad");
					return;
				}
				StackData.maximumStackAllowed = minStack;
			});
		}

		const string stackFileName = "stackMaxSize.txt";

		List<ItemObject> PostLimitationLoad() => [];// For mods to patch and add their own items to be prohibited

		IEnumerator LoadLimitations()
		{
			yield return 1;
			yield return "Registering unstackable items...";
			prohibitedItemsForStack.AddRange(ItemMetaStorage.Instance.GetAllWithFlags(ItemFlags.MultipleUse) // Get all items with MultipleUse, so they can't be stackable
					.ToAllItmValues());
			prohibitedItemsForStack.AddRange(ItemMetaStorage.Instance.FindAll(x => x.id == Items.None).ToAllItmValues());
			itemsToFullyIgnore.AddRange(ItemMetaStorage.Instance.GetAllWithFlags(ItemFlags.InstantUse) // Get all items with InstantUse, so they can't be stackable (neither just not work)
				.ToAllItmValues());
			itemsToFullyIgnore.AddRange(PostLimitationLoad());

			if (Chainloader.PluginInfos.ContainsKey("pixelguy.pixelmodding.baldiplus.bbpluslockers")) // BBPlusLockers support
			{
				var inf = Chainloader.PluginInfos["pixelguy.pixelmodding.baldiplus.bbpluslockers"].Instance.Info;
				prohibitedItemsForStack.Add(ItemMetaStorage.Instance.FindByEnumFromMod(EnumExtensions.GetFromExtendedName<Items>("Lockpick"), inf).value);
			}

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
			trashHolder.gameObject.ConvertToPrefab(true);

			var trashCol = new GameObject("TrashCollider");
			trashCol.transform.SetParent(trashHolder);
			trashCol.transform.localPosition = Vector3.zero;

			var collider = trashCol.gameObject.AddComponent<BoxCollider>();
			collider.size = new(horizontalSize, 1f, horizontalSize);

			var colliderAI = trashCol.gameObject.AddComponent<NavMeshObstacle>();
			colliderAI.size = collider.size + (Vector3.up * 5f);
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

			TrashCanSpawnFunction.trashCan = trashHolder.gameObject;
			List<RoomCategory> allowedCats = [RoomCategory.Class, RoomCategory.Office, RoomCategory.Faculty];

			GenericExtensions.FindResourceObjects<RoomAsset>().DoIf(x => x.type == RoomType.Room && allowedCats.Contains(x.category),
				x => { x.AddRoomFunctionToContainer<TrashCanSpawnFunction>(); allowedCats.Remove(x.category); });

			yield break;
		}

		void OnMen(OptionsMenu instance)
		{
			if (Singleton<CoreGameManager>.Instance != null) return; // No settings in-game
			GameObject ob = CustomOptionsCore.CreateNewCategory(instance, "Opt_StackItm");
			TextLocalizer TL = CustomOptionsCore.CreateText(instance, new Vector2(-5.45f, 65.03f), "Opt_StackSize");

			var text = CustomOptionsCore.CreateText(instance, new Vector2(-14.83f, -16.41f), $"{Singleton<LocalizationManager>.Instance.GetLocalizedText("Opt_StackSizeDisplay")} {StackData.maximumStackAllowed}");
			stackBar = CustomOptionsCore.CreateAdjustmentBar(instance, new Vector2(GenericExtensions.LinearEquation(maxStack - minStack, -7.425f, 68.725f), -61.2f), "StackSize", maxStack - minStack, "Tip_StackSize", StackData.maximumStackAllowed, () =>
			{
				StackData.maximumStackAllowed = Mathf.Clamp(stackBar.GetRaw() + minStack, minStack, maxStack);
				text.textBox.text = $"{Singleton<LocalizationManager>.Instance.GetLocalizedText("Opt_StackSizeDisplay")} {StackData.maximumStackAllowed}";
			});
			// attach everything to the options menu
			stackBar.transform.SetParent(ob.transform, false);
			text.transform.SetParent(ob.transform, false);
			TL.transform.SetParent(ob.transform, false);
		}

		static AdjustmentBars stackBar;
		public static HashSet<ItemObject> NonStackableItems => prohibitedItemsForStack;

		internal static HashSet<ItemObject> prohibitedItemsForStack = []; // Ignore only for stacks, the mod still does its checks in the item

		internal static HashSet<ItemObject> itemsToFullyIgnore = []; // Fully ignore means no mod check with the item at all (like points, for example)

		internal static string ModPath = string.Empty;

		internal static bool hasAnimationsMod = false;

		const float horizontalSize = 1.5f;

		internal const int minStack = 2, maxStack = 9;
	}
}
