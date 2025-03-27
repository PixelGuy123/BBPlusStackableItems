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
	[BepInPlugin("pixelguy.pixelmodding.baldiplus.stackableitems", PluginInfo.PLUGIN_NAME, "1.0.6.5")]
	[BepInDependency("mtm101.rulerp.bbplus.baldidevapi", BepInDependency.DependencyFlags.HardDependency)]
	[BepInDependency("pixelguy.pixelmodding.baldiplus.pixelinternalapi", BepInDependency.DependencyFlags.HardDependency)]

	[BepInDependency("pixelguy.pixelmodding.baldiplus.bbpluslockers", BepInDependency.DependencyFlags.SoftDependency)]

	public class StackableItemsPlugin : BaseUnityPlugin
	{
		public const string notAllowStackTag = "StackableItems_NotAllowStacking", fullyIgnoreItemTag = "StackableItems_FullyIgnoreItem";

		private void Awake()
		{
			Harmony h = new("pixelguy.pixelmodding.baldiplus.stackableitems");
			h.PatchAllConditionals();

			hasAnimationsMod = Chainloader.PluginInfos.ContainsKey("pixelguy.pixelmodding.baldiplus.newanimations");
			ModPath = AssetLoader.GetModPath(this);

			LoadingEvents.RegisterOnAssetsLoaded(Info, AddTrashCansInEverything(), false);

			LoadingEvents.RegisterOnAssetsLoaded(Info, LoadLimitations(), true);

			AssetLoader.LoadLocalizationFolder(Path.Combine(ModPath, "Language", "English"), Language.English);

			ModdedSaveGame.AddSaveHandler(new SaveItemStack(Info)); // Save stack properly
			ResourceManager.AddReloadLevelCallback((man, nextlevel) =>
			{
				if (nextlevel) StackData.i.SaveItemStack();
				else StackData.i.TryLoadPrevItemStack();
			});

			CustomOptionsCore.OnMenuInitialize += (optInstance, handler) => handler.AddCategory<StackableOptionsCat>("Stack Config"); // Copypaste from QuarterPouch lmao. I can't figure out the OptionsMenuAPI rn.
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

		IEnumerator LoadLimitations()
		{
			yield return 1;
			yield return "Registering unstackable items...";

			if (Chainloader.PluginInfos.ContainsKey("pixelguy.pixelmodding.baldiplus.bbpluslockers")) // The Legacy purpose in question lol
			{
				var inf = Chainloader.PluginInfos["pixelguy.pixelmodding.baldiplus.bbpluslockers"].Instance.Info;
				prohibitedItemsForStack.Add(ItemMetaStorage.Instance.FindByEnumFromMod(EnumExtensions.GetFromExtendedName<Items>("Lockpick"), inf).value);
			}

			yield break;
		}

		IEnumerator AddTrashCansInEverything()
		{
			yield return 3;
			// setup for trash can
			yield return "Creating trash can prefab...";
			var trash = ObjectCreationExtensions.CreateSpriteBillboard(AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromFile(Path.Combine(ModPath, "trashcan.png")), 75f)).AddSpriteHolder(out var trashCanRenderer, 3f, LayerStorage.iClickableLayer);
			trash.name = "TrashCan";
			trashCanRenderer.name = "TrashCanRenderer";
			trash.gameObject.ConvertToPrefab(true);

			var trashCol = new GameObject("TrashCollider");
			trashCol.transform.SetParent(trash.transform);
			trashCol.transform.localPosition = Vector3.zero;

			var collider = trashCol.gameObject.AddComponent<BoxCollider>();
			collider.size = new(horizontalSize, 1f, horizontalSize);

			var colliderAI = trashCol.gameObject.AddComponent<NavMeshObstacle>();
			colliderAI.size = collider.size + (Vector3.up * 5f);
			colliderAI.carving = true;

			var trashAcceptor = trash.gameObject.AddComponent<TrashcanComponent>();
			collider = trashAcceptor.gameObject.AddComponent<BoxCollider>();
			collider.size = new Vector3(horizontalSize, 14f, horizontalSize);

			trashAcceptor.audMan = trash.gameObject.CreatePropagatedAudioManager(55f, 75f);

			trashAcceptor.audThrow = ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromFile(Path.Combine(ModPath, "throwTrash.wav")), string.Empty, SoundType.Voice, Color.white);
			trashAcceptor.audThrow.subtitle = false; // no subs

			trashAcceptor.renderer = trash.transform;

			var trashIndicator = new GameObject("TrashUsesIndicator").AddComponent<TextMeshPro>();
			trashIndicator.alignment = TextAlignmentOptions.Center;
			trashIndicator.gameObject.layer = LayerStorage.billboardLayer;
			trashIndicator.transform.SetParent(trash.transform);

			trashIndicator.gameObject.AddComponent<BillboardRotator>();

			trashAcceptor.usesRender = trashIndicator;

			TrashCanSpawnFunction.trashCan = trash.gameObject;
			List<RoomCategory> allowedCats = [RoomCategory.Class, RoomCategory.Office, RoomCategory.Faculty];

			yield return "Adding Trash Can prefab to the room assets...";

			GenericExtensions.FindResourceObjects<RoomAsset>().DoIf(x => x.type == RoomType.Room && allowedCats.Contains(x.category),
				x => { x.AddRoomFunctionToContainer<TrashCanSpawnFunction>(); allowedCats.Remove(x.category); });

			yield return "Adding a Trash Can to the pitstop...";

			trash.gameObject.SetActive(false);
			var infiniteUseTrashCan = Instantiate(trash);
			trash.gameObject.SetActive(true);

			infiniteUseTrashCan.gameObject.ConvertToPrefab(true);
			infiniteUseTrashCan.name = "TrashCan_InfiniteUses";
			infiniteUseTrashCan.GetComponent<TrashcanComponent>().infiniteUses = true;

			var genericEnvBuilder = new GameObject("Structure_GenericEnvironmentBuilder").AddComponent<Structure_GenericEnvironmentSpawner>();
			genericEnvBuilder.gameObject.ConvertToPrefab(true);

			GenericExtensions.FindResourceObjectByName<LevelAsset>("Pitstop").structures.Add(new()
			{
				prefab = genericEnvBuilder,
				data = [new(infiniteUseTrashCan.gameObject, new(36, 10), Direction.North)]
			});

			yield break;
		}
		[System.Obsolete($"Include the \"{notAllowStackTag}\" tag inside of your item meta data.")]
		public static HashSet<ItemObject> NonStackableItems => prohibitedItemsForStack;

		internal static HashSet<ItemObject> prohibitedItemsForStack = []; // Left here for legacy purposes

		internal static string ModPath = string.Empty;

		internal static bool hasAnimationsMod = false;

		const float horizontalSize = 1.5f;

		internal const int minStack = 2, maxStack = 9;
	}
}
