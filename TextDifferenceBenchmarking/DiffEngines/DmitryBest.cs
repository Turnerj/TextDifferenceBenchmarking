using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using TextDifferenceBenchmarking.Utilities;

namespace TextDifferenceBenchmarking.DiffEngines
{
	/// <summary>
	/// Dmitry Bychenko's solution but with all techniques learnt
	/// </summary>
	public class DmitryBest : ITextDiff
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
				for (var i = 1; i <= sourceLength; ++i)
				{
					var mCurrentRow = M.Slice(i * columns);
					var dCurrentRow = D.Slice(i * columns);
					var dPrevRow = D.Slice((i - 1) * columns);
					var sourcePrevChar = source[i - 1];
					for (var j = 1; j <= targetLength; ++j)
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
				}

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
