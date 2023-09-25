using Unity.Burst;
using Unity.Jobs;

[BurstCompile]
public struct ClearOcclusionJob : IJobFor
{
	public Maze maze;

	public void Execute(int i) => maze.Unset(i, MazeFlags.VisibleToAll);
}
