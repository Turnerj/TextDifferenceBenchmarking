using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TextDifferenceBenchmarking.Utilities;

namespace TextDifferenceBenchmarking.DiffEngines
{
	/// <summary>
	/// Dmitry Bychenko's solution but with all techniques learnt but run in parallel
	/// </summary>
	public class DmitryBestParallel : ITextDiff
	{
		private class TaskData
		{
			public int Row;
			public bool ReadFirstChar;
			public EventWaitHandle WaitHandle;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private int Mod(int baseValue, int divValue)
		{
			return baseValue & (divValue - 1);
		}

		public unsafe EditOperation[] EditSequence(
			string source, string target,
			int insertCost = 1, int removeCost = 1, int editCost = 1)
		{
			if (null == source)
				throw new ArgumentNullException("source");
			else if (null == target)
				throw new ArgumentNullException("target");

			// Forward: building score matrix

			var sourceLength = source.Length;
			var targetLength = target.Length;
			var columns = targetLength + 1;
			var rows = sourceLength + 1;
			var totalSize = columns * rows;

			// Best operation (among insert, update, delete) to perform 
			var operationHandle = Marshal.AllocHGlobal(Unsafe.SizeOf<EditOperationKind>() * totalSize);
			var M = new Span<EditOperationKind>(operationHandle.ToPointer(), totalSize);

			// Minimum cost so far
			var costDataCount = 2 * columns;
			var costHandle = Marshal.AllocHGlobal(Unsafe.SizeOf<int>() * costDataCount);
			var D = new Span<int>(costHandle.ToPointer(), costDataCount);

			M[0] = EditOperationKind.None;
			D[0] = 0;

			// Edge: all removes
			D[1 * columns] = removeCost;
			for (var i = 1; i <= sourceLength; ++i)
			{
				M[i * columns] = EditOperationKind.Remove;
			}

			// Edge: all inserts 
			M.Slice(1, targetLength).Fill(EditOperationKind.Add);
			for (var i = 1; i <= targetLength; ++i)
			{
				D[i] = insertCost * i;
			}

			// Having fit N - 1, K - 1 characters let's fit N, K
			var maxDegreeOfParallelism = 6;//Environment.ProcessorCount;
			var columnsPerParallel = (int)Math.Ceiling((double)columns / maxDegreeOfParallelism);
			var columnsLeft = columns;
			var degreeOfParallelism = 0;
			for (; columnsLeft >= columnsPerParallel && degreeOfParallelism < maxDegreeOfParallelism; columnsLeft -= columnsPerParallel, degreeOfParallelism++) ;
			if (columnsLeft > 0)
			{
				degreeOfParallelism++;
			}

			var parallelData = new TaskData[degreeOfParallelism + 1];
			for (var i = 0; i <= degreeOfParallelism; i++)
			{
				parallelData[i] = new TaskData
				{
					Row = 0,
					WaitHandle = new AutoResetEvent(false),
					ReadFirstChar = false
				};
			}

			Debug.WriteLine($"Parallel Process Starting: {degreeOfParallelism} threads, {columnsPerParallel} columns each");

			Parallel.For(0, degreeOfParallelism, parallelIndex => {
				var localM = new Span<EditOperationKind>(operationHandle.ToPointer(), totalSize);
				var localD = new Span<int>(costHandle.ToPointer(), costDataCount);

				var columnStartIndex = columnsPerParallel * parallelIndex + 1;
				var currentTaskData = parallelData[parallelIndex];

				if (parallelIndex > 0)
				{
					currentTaskData.WaitHandle.WaitOne();
					Debug.WriteLine($"{parallelIndex} starts");
				}

				var debugStr2 = "";
				for (var i = 1; i <= sourceLength; ++i)
				{
					var mCurrentRow = localM.Slice(i * columns);
					var dCurrentRow = localD.Slice(Mod(i, 2) * columns);

					if (parallelIndex == 0)
					{
						dCurrentRow[0] = i * removeCost;
					}

					if (degreeOfParallelism > 0)
					{
						var hadToWait = false;
						while (true)
						{
							var waitingOnPrevious = false;
							if (parallelIndex > 0)
							{
								var prevTaskData = parallelData[parallelIndex - 1];
								waitingOnPrevious = prevTaskData.Row <= currentTaskData.Row;
								if (waitingOnPrevious)
								{
									Debug.WriteLine($"{parallelIndex} waiting on {parallelIndex - 1}");
									currentTaskData.WaitHandle.WaitOne();
								}
							}

							var waitingOnNext = false;
							if (parallelIndex + 1 < degreeOfParallelism)
							{
								var nextTaskData = parallelData[parallelIndex + 1];

								waitingOnNext = (nextTaskData.Row == currentTaskData.Row - 1 && !nextTaskData.ReadFirstChar) ||
									nextTaskData.Row < currentTaskData.Row - 1;
								if (waitingOnNext)
								{
									Debug.WriteLine($"{parallelIndex} waiting on {parallelIndex + 1}");
									currentTaskData.WaitHandle.WaitOne();
								}
							}

							if (!waitingOnPrevious && !waitingOnNext)
							{
								break;
							}
							else
							{
								hadToWait = true;
							}
						}

						if (hadToWait)
						{
							Debug.WriteLine($"{parallelIndex} starts");
						}
						else if (i > 1)
						{
							Debug.WriteLine($"{parallelIndex} didn't have to wait");
						}
					}

					var dPrevRow = localD.Slice(Mod(i - 1, 2) * columns);
					var sourcePrevChar = source[i - 1];
					var columnTravel = 0;
					var debugStr = "";
					debugStr2 = "";

					if (parallelIndex == 0)
					{
						debugStr += $"{dPrevRow[0]:00},";
						debugStr2 += $"{dCurrentRow[0]:00},";
					}

					for (var j = columnStartIndex; j <= targetLength && columnTravel < columnsPerParallel; ++j, columnTravel++)
					{
						// here we choose the operation with the least cost
						var insert = dCurrentRow[j - 1] + insertCost;
						var delete = dPrevRow[j] + removeCost;
						var edit = dPrevRow[j - 1] + (sourcePrevChar == target[j - 1] ? 0 : editCost);

						var min = Math.Min(Math.Min(insert, delete), edit);

						if (min == insert)
							mCurrentRow[j] = EditOperationKind.Add;
						else if (min == delete)
							mCurrentRow[j] = EditOperationKind.Remove;
						else if (min == edit)
							mCurrentRow[j] = EditOperationKind.Edit;

						dCurrentRow[j] = min;

						debugStr += $"{dPrevRow[j]:00},";
						debugStr2 += $"{dCurrentRow[j]:00},";

						if (!currentTaskData.ReadFirstChar)
						{
							currentTaskData.ReadFirstChar = true;
							if (parallelIndex > 0)
							{
								var prevTaskData = parallelData[parallelIndex - 1];
								Debug.WriteLine($"{parallelIndex} (row {currentTaskData.Row}) read first character (tells {parallelIndex - 1} (row {prevTaskData.Row}) to start)");
								prevTaskData.WaitHandle.Set();
							}
						}
					}

					Debug.WriteLine($"{i - 1:00}-{parallelIndex:00}: {debugStr}");

					currentTaskData.ReadFirstChar = false;
					currentTaskData.Row++;

					if (degreeOfParallelism > 1)
					{
						if (parallelIndex + 1 < degreeOfParallelism)
						{
							var nextTaskData = parallelData[parallelIndex + 1];
							Debug.WriteLine($"{parallelIndex} (formerly row {currentTaskData.Row - 1}) finishes row (tells {parallelIndex + 1} (row {nextTaskData.Row}) to start)");
							nextTaskData.WaitHandle.Set();
						}
					}
				}

				Debug.WriteLine($"{sourceLength:00}-{parallelIndex:00}: {debugStr2}");
			});

			for (int i = 0, l = parallelData.Length; i < l; i++)
			{
				parallelData[i].WaitHandle.Close();
			}

			Debug.WriteLine("All Complete");

			Marshal.FreeHGlobal(costHandle);

			// Backward: knowing scores (D) and actions (M) let's building edit sequence
			var maxSize = sourceLength + targetLength;
			var operationResultsHandle = Marshal.AllocHGlobal(Unsafe.SizeOf<EditOperation>() * maxSize);
			var operations = new Span<EditOperation>(operationResultsHandle.ToPointer(), maxSize);
			var operationIndex = maxSize;

			for (int x = targetLength, y = sourceLength; (x > 0) || (y > 0);)
			{
				operationIndex--;
				var op = M[y * columns + x];

				if (op == EditOperationKind.Add)
				{
					x -= 1;
					operations[operationIndex] = new EditOperation('\0', target[x], op);
				}
				else if (op == EditOperationKind.Remove)
				{
					y -= 1;
					operations[operationIndex] = new EditOperation(source[y], '\0', op);
				}
				else if (op == EditOperationKind.Edit)
				{
					x -= 1;
					y -= 1;
					operations[operationIndex] = new EditOperation(source[y], target[x], op);
				}
				else // Start of the matching (EditOperationKind.None)
					break;
			}

			Marshal.FreeHGlobal(operationHandle);
			var result = operations.Slice(operationIndex).ToArray();
			Marshal.FreeHGlobal(operationResultsHandle);
			return result;
		}
	}
}
