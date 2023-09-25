using Unity.Collections;

partial struct OcclusionJob
{
	struct ScanStack
	{
		NativeArray<Scan> stack;

		int stackSize;

		public ScanStack(int capacity, Scan firstScan)
		{
			stack = new(capacity, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
			stack[0] = firstScan;
			stackSize = 1;
		}

		public void Push(Scan scan) => stack[stackSize++] = scan;

		public bool TryPop(out Scan scan)
		{
			if (stackSize > 0)
			{
				scan = stack[--stackSize];
				return true;
			}
			scan = default;
			return false;
		}
	}
}
