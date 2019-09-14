using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace TextDifferenceBenchmarking.DiffEngines
{
	/// <summary>
	/// Dmitry Bychenko's solution but with both cost discarding and operation discarding, optimised with all the fast things
	/// </summary>
	public class DmitryOperationDiscardFast : ITextDiff
	{
		private int NumberOfOperationRows { get; }

		public DmitryOperationDiscardFast() : this(128) { }
		public DmitryOperationDiscardFast(int numberOfOperationRows)
		{
			if ((numberOfOperationRows & 1) != 0)
			{
				throw new ArgumentException("Value must be a power of 2", nameof(numberOfOperationRows));
			}

			NumberOfOperationRows = numberOfOperationRows;
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

			var maxSize = sourceLength + targetLength;
			var operationResultsHandle = Marshal.AllocHGlobal(Unsafe.SizeOf<EditOperation>() * maxSize);
			var operations = new Span<EditOperation>(operationResultsHandle.ToPointer(), maxSize);
			var operationIndex = maxSize;

			var operationDataCount = (NumberOfOperationRows + 1) * columns;
			var operationHandle = Marshal.AllocHGlobal(Unsafe.SizeOf<EditOperationKind>() * operationDataCount);
			var M = new Span<EditOperationKind>(operationHandle.ToPointer(), operationDataCount);

			// Minimum cost so far
			var costDataCount = 3 * columns;
			var costHandle = Marshal.AllocHGlobal(Unsafe.SizeOf<int>() * costDataCount);
			var D = new Span<int>(costHandle.ToPointer(), costDataCount);

			M[0] = EditOperationKind.None;
			D[0] = 0;

			// Having fit N - 1, K - 1 characters let's fit N, K
			for (int x = targetLength, y = sourceLength; (x > 0) || (y > 0);)
			{
				// Edge: all removes
				D[1 * columns] = removeCost;
				for (var i = 1; i <= NumberOfOperationRows; ++i)
				{
					M[i * columns] = EditOperationKind.Remove;
				}

				// Edge: all inserts 
				M.Slice(1, targetLength).Fill(EditOperationKind.Add);
				for (var i = 1; i <= targetLength; ++i)
				{
					D[i] = insertCost * i;
				}

				for (var i = 1; i <= y; ++i)
				{
					var mCurrentRow = M.Slice(Mod(i, NumberOfOperationRows) * columns);
					var dCurrentRow = D.Slice(Mod(i, 2) * columns);
					var dPrevRow = D.Slice(Mod(i - 1, 2) * columns);
					var sourcePrevChar = source[i - 1];
					for (var j = 1; j <= x; ++j)
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

				var outerBreak = false;
				for (var opRowIndex = 0; opRowIndex < NumberOfOperationRows && (x > 0 || y > 0); opRowIndex++)
				{
					operationIndex--;
					var op = M[Mod(y, NumberOfOperationRows) * columns + x];

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
					{
						outerBreak = true;
						break;
					}
				}

				if (outerBreak)
				{
					break;
				}
			}

			Marshal.FreeHGlobal(costHandle);
			Marshal.FreeHGlobal(operationHandle);
			var result = operations.Slice(operationIndex).ToArray();
			Marshal.FreeHGlobal(operationResultsHandle);
			return result;
		}
	}
}
