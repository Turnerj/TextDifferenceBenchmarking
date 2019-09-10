using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TextDifferenceBenchmarking.DiffEngines
{
	/// <summary>
	/// Dmitry Bychenko's solution but using a short instead of an int for costs
	/// </summary>
	public class DmitryShortCost : ITextDiff
	{
		public EditOperation[] EditSequence(
			string source, string target,
			int insertCost = 1, int removeCost = 1, int editCost = 1)
		{
			var localInsertCost = (short)insertCost;
			var localRemoveCost = (short)removeCost;
			var localEditCost = (short)editCost;

			if (null == source)
				throw new ArgumentNullException("source");
			else if (null == target)
				throw new ArgumentNullException("target");

			// Forward: building score matrix

			// Best operation (among insert, update, delete) to perform 
			EditOperationKind[][] M = Enumerable
			  .Range(0, source.Length + 1)
			  .Select(line => new EditOperationKind[target.Length + 1])
			  .ToArray();

			// Minimum cost so far
			short[][] D = Enumerable
			  .Range(0, source.Length + 1)
			  .Select(line => new short[target.Length + 1])
			  .ToArray();

			// Edge: all removes
			for (short i = 1; i <= source.Length; ++i)
			{
				M[i][0] = EditOperationKind.Remove;
				D[i][0] = (short)(localRemoveCost * i);
			}

			// Edge: all inserts 
			for (short i = 1; i <= target.Length; ++i)
			{
				M[0][i] = EditOperationKind.Add;
				D[0][i] = (short)(localInsertCost * i);
			}

			// Having fit N - 1, K - 1 characters let's fit N, K
			for (short i = 1; i <= source.Length; ++i)
				for (short j = 1; j <= target.Length; ++j)
				{
					// here we choose the operation with the least cost
					var insert = (short)(D[i][j - 1] + localInsertCost);
					var delete = (short)(D[i - 1][j] + localRemoveCost);
					var edit = (short)(D[i - 1][j - 1] + (source[i - 1] == target[j - 1] ? 0 : localEditCost));

					var min = Math.Min(Math.Min(insert, delete), edit);

					if (min == insert)
						M[i][j] = EditOperationKind.Add;
					else if (min == delete)
						M[i][j] = EditOperationKind.Remove;
					else if (min == edit)
						M[i][j] = EditOperationKind.Edit;

					D[i][j] = min;
				}

			// Backward: knowing scores (D) and actions (M) let's building edit sequence
			List<EditOperation> result =
			  new List<EditOperation>(source.Length + target.Length);

			for (int x = target.Length, y = source.Length; (x > 0) || (y > 0);)
			{
				EditOperationKind op = M[y][x];

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

			result.Reverse();

			return result.ToArray();
		}
	}
}
