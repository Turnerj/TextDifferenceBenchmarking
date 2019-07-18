using System;
using System.Collections.Generic;
using System.Linq;
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
		public EditOperation[] EditSequence(
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
			
			var result = new Stack<EditOperation>(sourceLength + targetLength);

			unsafe
			{
				// Best operation (among insert, update, delete) to perform 
				var underlyingType = Enum.GetUnderlyingType(typeof(EditOperationKind));
				var operationHandle = Marshal.AllocHGlobal(Marshal.SizeOf(underlyingType) * totalSize);
				var M = new Span<EditOperationKind>(operationHandle.ToPointer(), totalSize);

				// Minimum cost so far
				var costHandle = Marshal.AllocHGlobal(Marshal.SizeOf<int>() * totalSize);
				var D = new Span<int>(costHandle.ToPointer(), totalSize);

				M[0] = EditOperationKind.None;
				D[0] = 0;

				// Edge: all removes
				for (var i = 1; i <= sourceLength; ++i)
				{
					M[i * columns] = EditOperationKind.Remove;
					D[i * columns] = removeCost * i;
				}

				// Edge: all inserts 
				for (var i = 1; i <= targetLength; ++i)
				{
					M[i] = EditOperationKind.Add;
					D[i] = insertCost * i;
				}

				// Having fit N - 1, K - 1 characters let's fit N, K
				var maxDegreeOfParallelism = Environment.ProcessorCount;
				var minColumnsPerThread = 10;
				var columnsPerParallel = Math.Max(minColumnsPerThread, (int)Math.Ceiling((double)columns / maxDegreeOfParallelism));
				var columnsLeft = columns;
				var degreeOfParallelism = 0;
				for (; columnsLeft >= columnsPerParallel && degreeOfParallelism <= maxDegreeOfParallelism; columnsLeft -= columnsPerParallel, degreeOfParallelism++) ;
				if (columnsLeft > 0)
				{
					degreeOfParallelism++;
				}

				var rowProgress = new int[degreeOfParallelism];

				Parallel.For(0, degreeOfParallelism, parallelIndex => {
					var localM = new Span<EditOperationKind>(operationHandle.ToPointer(), totalSize);
					var localD = new Span<int>(costHandle.ToPointer(), totalSize);

					var columnStartIndex = columnsPerParallel * parallelIndex + 1;
					var localColumnSize = columnsPerParallel;

					if (parallelIndex + 1 == degreeOfParallelism)
					{
						localColumnSize = columnsLeft;
					}

					for (var i = 1; i <= sourceLength; ++i)
					{
						while (parallelIndex != 0 && rowProgress[parallelIndex - 1] <= i) ;
						
						var mCurrentRow = localM.Slice(i * columns);
						var dCurrentRow = localD.Slice(i * columns);
						var dPrevRow = localD.Slice((i - 1) * columns);
						var sourcePrevChar = source[i - 1];
						var columnTravel = 0;
						for (var j = columnStartIndex; j <= targetLength && columnTravel < localColumnSize; ++j, columnTravel++)
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
						}

						rowProgress[parallelIndex] = i;
					}

					rowProgress[parallelIndex]++;
				});

				// Backward: knowing scores (D) and actions (M) let's building edit sequence

				for (int x = targetLength, y = sourceLength; (x > 0) || (y > 0);)
				{
					var op = M[y * columns + x];

					if (op == EditOperationKind.Add)
					{
						x -= 1;
						result.Push(new EditOperation('\0', target[x], op));
					}
					else if (op == EditOperationKind.Remove)
					{
						y -= 1;
						result.Push(new EditOperation(source[y], '\0', op));
					}
					else if (op == EditOperationKind.Edit)
					{
						x -= 1;
						y -= 1;
						result.Push(new EditOperation(source[y], target[x], op));
					}
					else // Start of the matching (EditOperationKind.None)
						break;
				}

				Marshal.FreeHGlobal(operationHandle);
				Marshal.FreeHGlobal(costHandle);
			}

			return result.ToArray();
		}
	}
}
