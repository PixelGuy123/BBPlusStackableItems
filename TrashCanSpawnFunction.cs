using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace StackableItems
{
	public class TrashCanSpawnFunction : RoomFunction
	{
		public override void Build(LevelBuilder builder, System.Random rng)
		{
			base.Build(builder, rng);
			this.builder = builder;
			if (rng.NextDouble() > randomChance)
				return;

			var cells = room.GetTilesOfShape(TileShapeMask.Corner, true);
			for (int i = 0; i < cells.Count; i++)
				if (!room.entitySafeCells.Contains(cells[i].position))
					cells.RemoveAt(i--);

			if (cells.Count == 0) return;
			int max = rng.Next(1, roomAmount.ContainsKey(room.category) ? roomAmount[room.category] + 1 : 3);

			for (int i = 0; i < max; i++)
			{
				if (cells.Count == 0) return;

				int idx = rng.Next(cells.Count);
				structData.Add(new(trashCan, cells[idx].position, Direction.Null));
				cells.RemoveAt(idx);
			}
		}

		public override void OnGenerationFinished()
		{
			base.OnGenerationFinished();
			if (!builder)
				return;

			foreach (var data in structData)
			{
				var cell = room.ec.CellFromPosition(data.position);
				builder.InstatiateEnvironmentObject(trashCan, cell, Direction.North);
				room.entitySafeCells.Remove(data.position);
			}
		}

		LevelBuilder builder; // small workaround to instantiate environment objects
		readonly List<StructureData> structData = [];

		const float randomChance = 0.9f;

		internal static GameObject trashCan;

		public static void AddTrashAmountToCategory(RoomCategory cat, int amountOfTrashes) =>
			roomAmount.Add(cat, amountOfTrashes);

		readonly static Dictionary<RoomCategory, int> roomAmount = new() { { RoomCategory.Class, 1}, { RoomCategory.Faculty, 4}, { RoomCategory.Office, 2 } };
	}

	public class Structure_GenericEnvironmentSpawner : StructureBuilder
	{
		public override void Load(List<StructureData> data)
		{
			base.Load(data);
			foreach (var dat in data)
			{
				var cell = ec.CellFromPosition(dat.position);
				var obj = Instantiate(dat.prefab, cell.ObjectBase);
				obj.transform.position = cell.FloorWorldPosition;
				obj.transform.rotation = dat.direction.ToRotation();
				var ecObj = obj.GetComponent<EnvironmentObject>();

				if (ecObj)
				{
					ecObj.Ec = ec;
					ecObj.LoadingFinished();
				}
			}
		}
	}
}
