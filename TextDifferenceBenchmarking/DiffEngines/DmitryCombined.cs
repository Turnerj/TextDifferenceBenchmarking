﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TextDifferenceBenchmarking.DiffEngines
{
	/// <summary>
	/// All variations of Dmitry's solution combined (Inline length, multi-dimension, stack)
	/// </summary>
	public class DmitryCombined : ITextDiff
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

			// Best operation (among insert, update, delete) to perform 
			var M = new EditOperationKind[sourceLength + 1, targetLength + 1];

			// Minimum cost so far
			var D = new int[sourceLength + 1, targetLength + 1];

			// Edge: all removes
			for (int i = 1; i <= source.Length; ++i)
			{
				M[i,0] = EditOperationKind.Remove;
				D[i,0] = removeCost * i;
			}

			// Edge: all inserts 
			for (int i = 1; i <= target.Length; ++i)
			{
				M[0,i] = EditOperationKind.Add;
				D[0,i] = insertCost * i;
			}

			// Having fit N - 1, K - 1 characters let's fit N, K
			for (int i = 1; i <= sourceLength; ++i)
				for (int j = 1; j <= targetLength; ++j)
				{
					// here we choose the operation with the least cost
					int insert = D[i,j - 1] + insertCost;
					int delete = D[i - 1,j] + removeCost;
					int edit = D[i - 1,j - 1] + (source[i - 1] == target[j - 1] ? 0 : editCost);

					int min = Math.Min(Math.Min(insert, delete), edit);

					if (min == insert)
						M[i,j] = EditOperationKind.Add;
					else if (min == delete)
						M[i,j] = EditOperationKind.Remove;
					else if (min == edit)
						M[i,j] = EditOperationKind.Edit;

					D[i,j] = min;
				}

			// Backward: knowing scores (D) and actions (M) let's building edit sequence
			var result =
			  new Stack<EditOperation>(sourceLength + targetLength);

			for (int x = targetLength, y = sourceLength; (x > 0) || (y > 0);)
			{
				EditOperationKind op = M[y,x];

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

			return result.ToArray();
		}
	}
}
