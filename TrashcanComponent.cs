using System.Collections;
using UnityEngine;

namespace StackableItems
{
	public class TrashcanComponent : EnvironmentObject, IClickable<int>
	{
		public void Clicked(int player) // Intentionally not very readable lmao
		{
			if (Singleton<CoreGameManager>.Instance.GetPlayer(player).itm.items[Singleton<CoreGameManager>.Instance.GetPlayer(player).itm.selectedItem].itemType != Items.None)
			{
				Singleton<CoreGameManager>.Instance.GetPlayer(player).itm
				.RemoveItem(Singleton<CoreGameManager>.Instance.GetPlayer(player).itm.selectedItem);
				audMan.PlaySingle(audThrow);
				if (StackableItemsPlugin.hasAnimationsMod) // Easter egg ;)
				{
					if (animation != null)
						StopCoroutine(animation);
					animation = StartCoroutine(Animation());
				}
			}
		}

		public void ClickableSighted(int player) { }
		public void ClickableUnsighted(int player) { }
		public bool ClickableHidden() => false;
		public bool ClickableRequiresNormalHeight() => true;

		IEnumerator Animation()
		{
			Vector3 scale = Vector3.one;
			Vector3 target = Vector3.one + Vector3.up * 1.4f;
			float time = 0f;
			while (time < 1f)
			{
				time += ec.EnvironmentTimeScale * 15f;
				scale = Vector3.Lerp(scale, target, time);
				renderer.localScale = scale;
				yield return null;
			}
			scale = target;
			target = Vector3.one;
			time = 0f;
			while (time < 1f)
			{
				time += ec.EnvironmentTimeScale * 15f;
				scale = Vector3.Lerp(scale, target, time);
				renderer.localScale = scale;
				yield return null;
			}
			renderer.localScale = target;

			yield break;
		}

		[SerializeField]
		internal SoundObject audThrow;

		[SerializeField]
		internal PropagatedAudioManager audMan;

		[SerializeField]
		internal Transform renderer;

		Coroutine animation;
	}
}
