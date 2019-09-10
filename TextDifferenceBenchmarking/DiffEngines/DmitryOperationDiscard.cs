using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TextDifferenceBenchmarking.DiffEngines
{
	/// <summary>
	/// Dmitry Bychenko's solution but with both cost discarding and operation discarding
	/// </summary>
	public class DmitryOperationDiscard : ITextDiff
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
			var opModValue = 128;

			EditOperationKind[][] M = Enumerable
			  .Range(0, opModValue + 1)
			  .Select(line => new EditOperationKind[target.Length + 1])
			  .ToArray();

			List<EditOperation> result =
			  new List<EditOperation>(source.Length + target.Length);

			// Minimum cost so far
			int[][] D = Enumerable
			  .Range(0, 2)
			  .Select(line => new int[target.Length + 1])
			  .ToArray();

			// Having fit N - 1, K - 1 characters let's fit N, K
			for (int x = target.Length, y = source.Length; (x > 0) || (y > 0);)
			{
				// Edge: all removes
				D[1][0] = removeCost;
				for (int i = 1; i <= opModValue; ++i)
				{
					M[i][0] = EditOperationKind.Remove;
				}
				
				// Edge: all inserts 
				for (int i = 1; i <= target.Length; ++i)
				{
					M[0][i] = EditOperationKind.Add;
					D[0][i] = insertCost * i;
				}

				for (int i = 1; i <= y; ++i)
					for (int j = 1; j <= x; ++j)
					{
						// here we choose the operation with the least cost
						int insert = D[i % 2][j - 1] + insertCost;
						int delete = D[(i - 1) % 2][j] + removeCost;
						int edit = D[(i - 1) % 2][j - 1] + (source[i - 1] == target[j - 1] ? 0 : editCost);

						int min = Math.Min(Math.Min(insert, delete), edit);

						if (min == insert)
							M[i % opModValue][j] = EditOperationKind.Add;
						else if (min == delete)
							M[i % opModValue][j] = EditOperationKind.Remove;
						else if (min == edit)
							M[i % opModValue][j] = EditOperationKind.Edit;

						D[i % 2][j] = min;
					}

				var outerBreak = false;
				for (int opCount = 0; opCount < opModValue && (x > 0 || y > 0); opCount++)
				{
					EditOperationKind op = M[y % opModValue][x];

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

			result.Reverse();

			return result.ToArray();
		}
	}
}
