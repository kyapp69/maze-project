using Unity.Mathematics;

using static Unity.Mathematics.math;

partial struct OcclusionJob
{
	struct CellData
	{
		readonly float inset;

		readonly float south, north;

		float west, east;

		public bool IsNorthVisible
		{ get; private set; }

		public bool IsEastVisible
		{ get; private set; }

		public float LeftSlope
		{ get; private set; }

		public float RightSlope
		{ get; private set; }

		public float RightSlopeForNorthNeighbor
		{ get; private set; }

		readonly float WestInset => west + inset;

		readonly float EastInset => east - inset;

		readonly float SouthInset => south + inset;

		readonly float NorthInset => north - inset;

		public CellData(float2 offset, float inset, float leftSlope, float rightSlope)
		{
			this = default;
			this.inset = inset;
			west = offset.x;
			east = west + 1f;
			south = offset.y;
			north = south + 1f;
			LeftSlope = leftSlope;
			RightSlope = rightSlope;
		}

		public void StepEast()
		{
			west += 1f;
			east += 1f;
		}

		public void UpdateForNextCell(MazeFlags cell, Quadrant quadrant, float range) {
			if (cell.Has(quadrant.south) &&
				cell.HasNot(quadrant.southeast) &&
				SouthInset > 0f)
			{
				RightSlope = min(RightSlope, EastInset / SouthInset);
			}

			if (cell.Has(quadrant.north) && IsInRange(max(0f, west), north, range))
			{
				if (cell.HasNot(quadrant.northwest) && WestInset > 0f)
				{
					LeftSlope = max(LeftSlope, WestInset / NorthInset);
				}

				RightSlopeForNorthNeighbor = min(RightSlope,
					(cell.Has(quadrant.northeast) ? east : EastInset) / north);

				IsNorthVisible = LeftSlope < RightSlopeForNorthNeighbor;
			}
			else
			{
				IsNorthVisible = false;
			}

			IsEastVisible =
				cell.Has(quadrant.east) &&
				LeftSlope < RightSlope &&
				IsInRange(east, max(0f, south), range) &&
				(cell.Has(quadrant.northeast) ?
					east / north < RightSlope :
					NorthInset > 0f && east / NorthInset < RightSlope);
		}

		static bool IsInRange(float x, float y, float range) =>
			(x * x + y * y) < range * range;
	}
}
