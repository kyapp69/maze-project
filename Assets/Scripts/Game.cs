using TMPro;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

using static Unity.Mathematics.math;

using Random = UnityEngine.Random;

public class Game : MonoBehaviour
{
	[SerializeField]
	MazeVisualization visualization;

	[SerializeField]
	int2 mazeSize = int2(20, 20);

	[SerializeField, Tooltip("Use zero for random seed.")]
	int seed;

	[SerializeField, Range(0f, 1f)]
	float
		pickLastProbability = 0.5f,
		openDeadEndProbability = 0.5f,
		openArbitraryProbability = 0.5f;

	[SerializeField]
	Player player;

	[SerializeField]
	Agent[] agents;

	[SerializeField]
	TextMeshPro displayText;

	[SerializeField, Range(0, 0.5f)]
	float wallExtents = 0.25f;

	Maze maze;

	Scent scent;

	bool isPlaying;

	MazeCellObject[] cellObjects;

	MazeFlags visibilityMask;

	void StartNewGame()
	{
		isPlaying = true;
		displayText.gameObject.SetActive(false);
		maze = new(mazeSize);
		scent = new(maze);
		new FindDiagonalPassagesJob
		{
			maze = maze
		}.ScheduleParallel(
			maze.Length, maze.SizeEW, new GenerateMazeJob
			{
				maze = maze,
				seed = seed != 0 ? seed : Random.Range(1, int.MaxValue),
				pickLastProbability = pickLastProbability,
				openDeadEndProbability = openDeadEndProbability,
				openArbitraryProbability = openArbitraryProbability
			}.Schedule()).Complete();

		if (cellObjects == null || cellObjects.Length != maze.Length)
		{
			cellObjects = new MazeCellObject[maze.Length];
		}
		visualization.Visualize(maze, cellObjects);

		if (seed != 0)
		{
			Random.InitState(seed);
		}

		player.StartNewGame(maze.CoordinatesToWorldPosition(
			int2(Random.Range(0, mazeSize.x / 4), Random.Range(0, mazeSize.y / 4))));

		int2 halfSize = mazeSize / 2;
		for (int i = 0; i < agents.Length; i++)
		{
			var coordinates =
				int2(Random.Range(0, mazeSize.x), Random.Range(0, mazeSize.y));
			if (coordinates.x < halfSize.x && coordinates.y < halfSize.y)
			{
				if (Random.value < 0.5f)
				{
					coordinates.x += halfSize.x;
				}
				else
				{
					coordinates.y += halfSize.y;
				}
			}
			agents[i].StartNewGame(maze, coordinates);
		}
	}

	void Update()
	{
		if (isPlaying)
		{
			UpdateGame();
		}
		else if (Input.GetKeyDown(KeyCode.Space))
		{
			StartNewGame();
			UpdateGame();
		}
	}

	void UpdateGame()
	{
		Vector3 playerPosition = player.Move();
		NativeArray<float> currentScent = scent.Disperse(maze, playerPosition);
		for (int i = 0; i < agents.Length; i++)
		{
			Vector3 agentPosition = agents[i].Move(currentScent);
			if (
				new Vector2(
					agentPosition.x - playerPosition.x,
					agentPosition.z - playerPosition.z).sqrMagnitude < 1f)
			{
				EndGame(agents[i].TriggerMessage);
				return;
			}
		}
		UpdateOcclusion(playerPosition);
	}

	void UpdateOcclusion(Vector3 playerPosition)
	{
		var isVisibleToPlayer = new NativeArray<bool>(agents.Length, Allocator.TempJob);
		JobHandle handle = new ClearOcclusionJob
		{
			maze = maze
		}.ScheduleParallel(maze.Length, maze.Length / 4, default);

		float wallExtentsInMazeSpace = maze.WorldToMazeDistance(wallExtents);
		handle = new OcclusionJob
		{
			isVisibleToPlayer = isVisibleToPlayer,
			maze = maze,
			position = maze.WorldToMazePosition(playerPosition),
			fieldOfView = player.Vision,
			wallExtents = wallExtentsInMazeSpace,
			visibilityFlag = MazeFlags.VisibleToPlayer
		}.ScheduleParallel(4, 1, handle);

		for (int i = 0; i < agents.Length; i++)
		{
			Agent agent = agents[i];
			handle = new OcclusionJob
			{
				isVisibleToPlayer = isVisibleToPlayer,
				isVisibleToPlayerIndex = i,
				maze = maze,
				position = maze.WorldToMazePosition(agent.transform.localPosition),
				fieldOfView = new FieldOfView
				{
					range = maze.WorldToMazeDistance(agent.LightRange),
					omnidirectional = true
				},
				wallExtents = wallExtentsInMazeSpace,
				visibilityFlag = (MazeFlags)((int)MazeFlags.VisbleToAgentA << i)
			}.ScheduleParallel(4, 1, handle);
		}

		handle.Complete();

		visibilityMask = MazeFlags.VisibleToPlayer;
		for (int i = 0; i < agents.Length; i++)
		{
			bool isVisible = isVisibleToPlayer[i];
			agents[i].SetLightEnabled(isVisible);
			if (isVisible)
			{
				visibilityMask |= (MazeFlags)((int)MazeFlags.VisbleToAgentA << i);
			}
		}
		isVisibleToPlayer.Dispose();

		for (int i = 0; i < cellObjects.Length; i++)
		{
			cellObjects[i].gameObject.SetActive(maze[i].HasAny(visibilityMask));
		}
	}

	void EndGame(string message)
	{
		isPlaying = false;
		displayText.text = message;
		displayText.gameObject.SetActive(true);
		for (int i = 0; i < agents.Length; i++)
		{
			agents[i].EndGame();
		}

		for (int i = 0; i < cellObjects.Length; i++)
		{
			cellObjects[i].Recycle();
		}

		OnDestroy();
	}

	void OnDestroy()
	{
		maze.Dispose();
		scent.Dispose();
	}

	void OnDrawGizmosSelected()
	{
		if (!isPlaying)
		{
			return;
		}
		
		var size = new Vector3(1.75f, 0.01f, 1.75f);
		for (int i = 0; i < cellObjects.Length; i++)
		{
			MazeFlags flags = maze[i];
			if (flags.HasAny(MazeFlags.VisibleToAll))
			{
				Gizmos.color = flags.Has(MazeFlags.VisibleToPlayer) ?
					flags.HasAny(MazeFlags.VisibleToAllAgents) ?
						Color.yellow : Color.green :
					flags.HasAny(visibilityMask) ?
						Color.red : Color.blue;
				Vector3 position = cellObjects[i].transform.localPosition;
				position.y = 0f;
				Gizmos.DrawCube(position, size);
			}
		}
	}
}
