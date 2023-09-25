using Unity.Mathematics;

using static Unity.Mathematics.math;

partial struct OcclusionJob
{
	struct Scan
	{
		public int index;

		public float2 offset;

		public float leftSlope, rightSlope;

		public void ShiftEast(int index, float xOffset, float leftSlope)
		{
			this.index = index;
			offset.x = xOffset;
			this.leftSlope = leftSlope;
		}

		public readonly Scan ShiftedNorth(int indexOffset, float rightSlope) => new()
		{
			index = index + indexOffset,
			offset = float2(offset.x, offset.y + 1f),
			leftSlope = leftSlope,
			rightSlope = rightSlope
		};
	}
}
