using UnityEngine;
using HarmonyLib;

namespace StackableItems
{
	public class TrashCanSpawnFunction : RoomFunction
	{
		public override void Build(LevelBuilder builder, System.Random rng)
		{
			base.Build(builder, rng);
			if (rng.NextDouble() > randomChance)
				return;

			var cells = room.GetTilesOfShape([TileShape.Corner], true);
			for (int i = 0; i < cells.Count; i++)
				if (!room.entitySafeCells.Contains(cells[i].position))
					cells.RemoveAt(i--);

			if (cells.Count == 0) return;

			var cell = cells[rng.Next(cells.Count)];
			var transform = builder.InstatiateEnvironmentObject(trashCan, cell, Direction.North);
			room.entitySafeCells.Remove(cell.position);

			trashCan.GetComponent<RendererContainer>().renderers.Do(cell.AddRenderer);

		}

		const float randomChance = 0.7f;

		internal static GameObject trashCan;
	}
}
