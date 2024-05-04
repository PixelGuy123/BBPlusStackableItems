using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

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
			int max = rng.Next(1, roomAmount[room.category] + 1);

			for (int i = 0; i < max; i++)
			{
				if (cells.Count == 0) return;

				int idx = rng.Next(cells.Count);
				var cell = cells[idx];

				var t = builder.InstatiateEnvironmentObject(trashCan, cell, Direction.North);
				room.entitySafeCells.Remove(cell.position);
				t.GetComponents<Renderer>().Do(cell.AddRenderer);

				cells.RemoveAt(idx);
			}
		}

		const float randomChance = 0.9f;

		internal static GameObject trashCan;

		readonly static Dictionary<RoomCategory, int> roomAmount = new() { { RoomCategory.Class, 1}, { RoomCategory.Faculty, 4}, { RoomCategory.Office, 2 } };
	}
}
