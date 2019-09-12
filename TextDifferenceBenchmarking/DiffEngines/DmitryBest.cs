using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
			var costDataCount = 3 * columns;
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
			for (var i = 1; i <= sourceLength; ++i)
			{
				var mCurrentRow = M.Slice(i * columns);
				var dCurrentRow = D.Slice(Mod(i, 2) * columns);
				var dPrevRow = D.Slice(Mod(i - 1, 2) * columns);
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
