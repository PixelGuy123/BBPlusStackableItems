using System.Collections;
using UnityEngine;
using PixelInternalAPI.Components;
using TMPro;

namespace StackableItems
{
	public class TrashcanComponent : ModdedEnvironmentObject, IClickable<int>
	{
		public void Clicked(int player) // Intentionally not very readable lmao
		{
			if (uses <= 0 && !infiniteUses) return;

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
				if (!infiniteUses)
				{
					if (--uses <= 0)
					{
						usesRender.text = "X";
						usesRender.color = Color.red;
						usesRender.fontWeight = FontWeight.Bold;
					}
					else usesRender.text = uses.ToString();
				}
			}
		}

		public void ClickableSighted(int player) { }
		public void ClickableUnsighted(int player) { }
		public bool ClickableHidden() => uses <= 0 && !infiniteUses;
		public bool ClickableRequiresNormalHeight() => true;

		IEnumerator Animation()
		{
			Vector3 startsize = renderer.localScale;
			Vector3 targetsize = startsize + Vector3.up * 0.2f;

			Vector3 startpos = renderer.localPosition;
			Vector3 targetPos = startpos + Vector3.up * 0.3f;
			float time = 0f;
			while (true)
			{
				time += ec.EnvironmentTimeScale * 15f * Time.deltaTime;
				if (time >= 1f)
				{
					renderer.localScale = targetsize;
					renderer.localPosition = targetPos;
					break;
				}
				renderer.localScale = Vector3.Lerp(startsize, targetsize, time);
				renderer.localPosition = Vector3.Lerp(startpos, targetPos, time);
				yield return null;
			}

			targetsize = startsize;
			startsize = renderer.localScale;

			targetPos = startpos;
			startpos = renderer.localPosition;

			time = 0f;
			while (true)
			{
				time += ec.EnvironmentTimeScale * 15f *  Time.deltaTime;
				if (time >= 1f)
				{
					renderer.localScale = targetsize;
					renderer.localPosition = targetPos;
					break;
				}
				renderer.localScale = Vector3.Lerp(startsize, targetsize, time);
				renderer.localPosition = Vector3.Lerp(startpos, targetPos, time);
				yield return null;
			}

			yield break;
		}
		public override void LoadingFinished()
		{
			base.LoadingFinished();
			uses = Random.Range(minUses, maxUses + 1);
			usesRender.text = uses.ToString();
			if (infiniteUses)
				usesRender.gameObject.SetActive(false);
		}

		void Update()
		{
			if (infiniteUses)
				return;
			
			usesRender.transform.localPosition = new(0f, 7f + (Mathf.Sin(Time.fixedTime * 4.5f) / 2f), 0f);
		}
		

		int uses = 0; // Yes, random use value

		[SerializeField]
		internal SoundObject audThrow;

		[SerializeField]
		internal bool infiniteUses = false;

		[SerializeField]
		internal int minUses = 1, maxUses = 3;

		[SerializeField]
		internal PropagatedAudioManager audMan;

		[SerializeField]
		internal Transform renderer;

		[SerializeField]
		internal TextMeshPro usesRender;

		Coroutine animation;
	}
}
