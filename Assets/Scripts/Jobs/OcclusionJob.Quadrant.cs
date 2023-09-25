partial struct OcclusionJob
{
	readonly struct Quadrant
	{
		public readonly MazeFlags north, east, south, northwest, northeast, southeast;

		public readonly bool flipNS, flipEW;

		public Quadrant(bool flipNS, bool flipEW)
		{
			this.flipNS = flipNS;
			this.flipEW = flipEW;

			north = flipNS ? MazeFlags.PassageS : MazeFlags.PassageN;
			south = flipNS ? MazeFlags.PassageN : MazeFlags.PassageS;
			east = flipEW ? MazeFlags.PassageW : MazeFlags.PassageE;

			if (flipEW)
			{
				if (flipNS)
				{
					northwest = MazeFlags.PassageSE;
					northeast = MazeFlags.PassageSW;
					southeast = MazeFlags.PassageNW;
				}
				else
				{
					northwest = MazeFlags.PassageNE;
					northeast = MazeFlags.PassageNW;
					southeast = MazeFlags.PassageSW;
				}
			}
			else if (flipNS)
			{
				northwest = MazeFlags.PassageSW;
				northeast = MazeFlags.PassageSE;
				southeast = MazeFlags.PassageNE;
			}
			else
			{
				northwest = MazeFlags.PassageNW;
				northeast = MazeFlags.PassageNE;
				southeast = MazeFlags.PassageSE;
			}
		}
	}

	readonly static Quadrant[] quadrants = {
		new(false, false), // NE
		new(true, false), // SE
		new(true, true), // SW
		new(false, true), // NW
	};
}
