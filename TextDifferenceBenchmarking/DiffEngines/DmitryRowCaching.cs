using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TextDifferenceBenchmarking.DiffEngines
{
	/// <summary>
	/// Dmitry Bychenko's solution but with row caching
	/// </summary>
	public class DmitryRowCaching : ITextDiff
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

			// Best operation (among insert, update, delete) to perform 
			EditOperationKind[][] M = Enumerable
			  .Range(0, source.Length + 1)
			  .Select(line => new EditOperationKind[target.Length + 1])
			  .ToArray();

			// Minimum cost so far
			int[][] D = Enumerable
			  .Range(0, source.Length + 1)
			  .Select(line => new int[target.Length + 1])
			  .ToArray();

			// Edge: all removes
			for (int i = 1; i <= source.Length; ++i)
			{
				M[i][0] = EditOperationKind.Remove;
				D[i][0] = removeCost * i;
			}

			// Edge: all inserts 
			for (int i = 1; i <= target.Length; ++i)
			{
				M[0][i] = EditOperationKind.Add;
				D[0][i] = insertCost * i;
			}

			// Having fit N - 1, K - 1 characters let's fit N, K
			for (int i = 1; i <= source.Length; ++i)
			{
				var currentRow = D[i];
				var prevRow = D[i - 1];
				for (int j = 1; j <= target.Length; ++j)
				{
					// here we choose the operation with the least cost
					int insert = currentRow[j - 1] + insertCost;
					int delete = prevRow[j] + removeCost;
					int edit = prevRow[j - 1] + (source[i - 1] == target[j - 1] ? 0 : editCost);

					int min = Math.Min(Math.Min(insert, delete), edit);

					if (min == insert)
						M[i][j] = EditOperationKind.Add;
					else if (min == delete)
						M[i][j] = EditOperationKind.Remove;
					else if (min == edit)
						M[i][j] = EditOperationKind.Edit;

					currentRow[j] = min;
				}
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
