using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

using static Unity.Mathematics.math;

[BurstCompile]
public partial struct OcclusionJob : IJobFor
{
	[NativeDisableParallelForRestriction]
	public NativeArray<bool> isVisibleToPlayer;

	public int isVisibleToPlayerIndex;

	public Maze maze;

	public float wallExtents;

	public float2 position;

	public FieldOfView fieldOfView;

	public MazeFlags visibilityFlag;

	public void Execute(int i)
	{
		if (!TryGetFirstScan(i, out Scan scan, out Quadrant quadrant, out float2 origin))
		{
			return;
		}

		int stepEast = quadrant.flipEW ? maze.StepW : maze.StepE;
		int stepNorth = quadrant.flipNS ? maze.StepS : maze.StepN;
		var spottedFlags = MazeFlags.Empty;

		var stack = new ScanStack(maze.SizeEW, scan);
		while (stack.TryPop(out scan))
		{
			var data = new CellData(
				scan.offset - origin, wallExtents, scan.leftSlope, scan.rightSlope);
			int currentIndex = scan.index;
			float currentXOffset = scan.offset.x;
			bool isPreviousNorthVisible = false;
			float previousRightSlopeForNorthNeighbor = 0f;

			while (true)
			{
				MazeFlags cell = maze.Set(currentIndex, visibilityFlag);
				data.UpdateForNextCell(cell, quadrant, fieldOfView.range);
				spottedFlags |= cell;

				if (data.IsNorthVisible)
				{
					if (!isPreviousNorthVisible)
					{
						scan.ShiftEast(currentIndex, currentXOffset, data.LeftSlope);
					}
					else if (cell.HasNot(quadrant.northwest))
					{
						stack.Push(scan.ShiftedNorth(
							stepNorth, previousRightSlopeForNorthNeighbor));
						scan.ShiftEast(currentIndex, currentXOffset, data.LeftSlope);
					}
				}
				else if (isPreviousNorthVisible)
				{
					stack.Push(scan.ShiftedNorth(
						stepNorth, previousRightSlopeForNorthNeighbor));
				}

				if (data.IsEastVisible)
				{
					currentIndex += stepEast;
					currentXOffset += 1f;
					isPreviousNorthVisible = data.IsNorthVisible;
					previousRightSlopeForNorthNeighbor = data.RightSlopeForNorthNeighbor;
					data.StepEast();
				}
				else
				{
					if (data.IsNorthVisible)
					{
						stack.Push(scan.ShiftedNorth(
							stepNorth, data.RightSlopeForNorthNeighbor));
					}
					break;
				}
			}
		}

		if (visibilityFlag != MazeFlags.VisibleToPlayer &&
			spottedFlags.Has(MazeFlags.VisibleToPlayer))
		{
			isVisibleToPlayer[isVisibleToPlayerIndex] = true;
		}
	}

	bool TryGetFirstScan(int i, out Scan scan, out Quadrant quadrant, out float2 origin)
	{
		quadrant = quadrants[i];
		origin = frac(position);

		float2 leftLine, rightLine;
		if (quadrant.flipEW != quadrant.flipNS)
		{
			leftLine = fieldOfView.rightLine;
			rightLine = fieldOfView.leftLine;
		}
		else
		{
			leftLine = fieldOfView.leftLine;
			rightLine = fieldOfView.rightLine;
		}

		if (quadrant.flipEW)
		{
			leftLine.x = -leftLine.x;
			rightLine.x = -rightLine.x;
			origin.x = min(1f - origin.x, 0.999999f);
		}

		if (quadrant.flipNS)
		{
			leftLine.y = -leftLine.y;
			rightLine.y = -rightLine.y;
			origin.y = min(1f - origin.y, 0.999999f);
		}

		scan = new Scan
		{
			index = maze.CoordinatesToIndex((int2)position),
			rightSlope = float.MaxValue
		};

		if (fieldOfView.omnidirectional)
		{
			return true;
		}

		if (
			leftLine.x >= 0f && leftLine.y <= 0f ||
			rightLine.x <= 0f && rightLine.y >= 0f ||
			leftLine.y <= 0f && rightLine.x <= 0f)
		{
			return false;
		}

		scan.leftSlope = leftLine.x <= 0f ? 0f : leftLine.x / leftLine.y;
		scan.rightSlope = rightLine.y <= 0f ? float.MaxValue : rightLine.x / rightLine.y;
		return true;
	}
}
