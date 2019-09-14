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
			var maxDegreeOfParallelism = Environment.ProcessorCount;
			var columnsPerParallel = (int)Math.Ceiling((double)columns / maxDegreeOfParallelism);
			columnsPerParallel = Math.Max(columnsPerParallel, 16);
			var columnsLeft = columns;
			var degreeOfParallelism = 0;
			for (; columnsLeft >= columnsPerParallel && degreeOfParallelism < maxDegreeOfParallelism; columnsLeft -= columnsPerParallel, degreeOfParallelism++) ;
			if (columnsLeft > 0)
			{
				degreeOfParallelism++;
			}

			var parallelData = new TaskData[degreeOfParallelism];
			for (var i = 0; i < degreeOfParallelism; i++)
			{
				parallelData[i] = new TaskData
				{
					Row = 0,
					ReadFirstChar = false
				};
			}

			Parallel.For(0, degreeOfParallelism, parallelIndex => {
				var localM = new Span<EditOperationKind>(operationHandle.ToPointer(), totalSize);
				var localD = new Span<int>(costHandle.ToPointer(), costDataCount);

				var columnStartIndex = columnsPerParallel * parallelIndex + 1;
				var currentTaskData = parallelData[parallelIndex];

				for (var i = 1; i <= sourceLength; ++i)
				{
					var mCurrentRow = localM.Slice(i * columns);
					var dCurrentRow = localD.Slice(Mod(i, 2) * columns);

					if (parallelIndex == 0)
					{
						//All removes on edge
						dCurrentRow[0] = i * removeCost;
					}

					if (degreeOfParallelism > 0)
					{
						var isNotFirstThread = parallelIndex > 0;
						var isNotLastThread = parallelIndex + 1 < degreeOfParallelism;
						var prevTaskData = isNotFirstThread ? parallelData[parallelIndex - 1] : null;
						var nextTaskData = isNotLastThread ? parallelData[parallelIndex + 1] : null;

						var currentRow = currentTaskData.Row;
						var previousRow = currentRow - 1;

						while (
							//Previous task isn't ready for current task to continue
							(
								isNotFirstThread &&
								prevTaskData.Row <= currentRow
							) ||
							//Next task isn't ready for current task to continue
							(
								isNotLastThread &&
								(
									(
										nextTaskData.Row == previousRow &&
										!nextTaskData.ReadFirstChar
									) ||
									nextTaskData.Row < previousRow
								)
							)
						) ;
					}

					var dPrevRow = localD.Slice(Mod(i - 1, 2) * columns);
					var sourcePrevChar = source[i - 1];
					var columnTravel = 0;

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
						else
							mCurrentRow[j] = EditOperationKind.Edit;

						dCurrentRow[j] = min;

						if (!currentTaskData.ReadFirstChar)
						{
							currentTaskData.ReadFirstChar = true;
						}
					}

					currentTaskData.ReadFirstChar = false;
					currentTaskData.Row++;
				}
			});

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
