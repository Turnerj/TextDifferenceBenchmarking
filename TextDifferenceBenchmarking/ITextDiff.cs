using System;
using System.Collections.Generic;
using System.Text;

namespace TextDifferenceBenchmarking
{
	public interface ITextDiff
	{
		EditOperation[] EditSequence(
			string source, string target, 
			int insertCost = 1, int removeCost = 1, int editCost = 1);
	}
}
