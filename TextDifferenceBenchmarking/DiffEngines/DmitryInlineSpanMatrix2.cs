using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using TextDifferenceBenchmarking.Utilities;

namespace TextDifferenceBenchmarking.DiffEngines
{
	/// <summary>
	/// Dmitry Bychenko's solution but with an inline span matrix (based on DenseMatrix) - v2
	/// </summary>
	public class DmitryInlineSpanMatrix2 : ITextDiff
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

			List<EditOperation> result =
			  new List<EditOperation>(source.Length + target.Length);

			unsafe
			{

				var columns = target.Length + 1;
				var rows = source.Length + 1;

				// Best operation (among insert, update, delete) to perform 
				var underlyingType = Enum.GetUnderlyingType(typeof(EditOperationKind));
				var operationHandle = Marshal.AllocHGlobal(Marshal.SizeOf(underlyingType) * columns * rows);
				var M = new Span<EditOperationKind>(operationHandle.ToPointer(), columns * rows);

				// Minimum cost so far
				var costHandle = Marshal.AllocHGlobal(Marshal.SizeOf<int>() * columns * rows);
				var D = new Span<int>(costHandle.ToPointer(), columns * rows);

				M[0] = EditOperationKind.None;
				D[0] = 0;

				// Edge: all removes
				for (int i = 1; i <= source.Length; ++i)
				{
					M[i * columns] = EditOperationKind.Remove;
					D[i * columns] = removeCost * i;
				}

				// Edge: all inserts 
				for (int i = 1; i <= target.Length; ++i)
				{
					M[i] = EditOperationKind.Add;
					D[i] = insertCost * i;
				}

				// Having fit N - 1, K - 1 characters let's fit N, K
				for (int i = 1; i <= source.Length; ++i)
				{
					var mCurrentRow = M.Slice(i * columns);
					var dCurrentRow = D.Slice(i * columns);
					var dPrevRow = D.Slice((i - 1) * columns);
					var sourcePrevChar = source[i - 1];
					for (int j = 1; j <= target.Length; ++j)
					{
						// here we choose the operation with the least cost
						int insert = dCurrentRow[j - 1] + insertCost;
						int delete = dPrevRow[j] + removeCost;
						int edit = dPrevRow[j - 1] + (sourcePrevChar == target[j - 1] ? 0 : editCost);

						int min = Math.Min(Math.Min(insert, delete), edit);

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

				for (int x = target.Length, y = source.Length; (x > 0) || (y > 0);)
				{
					EditOperationKind op = M[y * columns + x];

					if (op == EditOperationKind.Add)
					{
						x -= 1;
						result.Add(new EditOperation('\0', target[x], op));
					}
					else if (op == EditOperationKind.Remove)
					{
						y -= 1;
						result.Add(new EditOperation(source[y], '\0', op));
					}
					else if (op == EditOperationKind.Edit)
					{
						x -= 1;
						y -= 1;
						result.Add(new EditOperation(source[y], target[x], op));
					}
					else // Start of the matching (EditOperationKind.None)
						break;
				}

				Marshal.FreeHGlobal(operationHandle);
				Marshal.FreeHGlobal(costHandle);
			}

			result.Reverse();

			return result.ToArray();
		}
	}
}
