using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace TextDifferenceBenchmarking.Benchmarks
{
	public abstract class TextBenchmarkBase
	{
		protected string ComparisonStringA;

		protected string ComparisonStringB;

		protected void InitialiseComparisonString(int numberOfCharacters)
		{
			var baseStringA = "aabbccddee";
			var baseStringB = "abcdeabcde";
			var builderA = new StringBuilder(numberOfCharacters);
			var builderB = new StringBuilder(numberOfCharacters);

			var charBlocks = (int)Math.Floor((double)numberOfCharacters / 10);
			for (int i = 0, l = charBlocks; i < l; i++)
			{
				builderA.Append(baseStringA);
				builderB.Append(baseStringB);
			}

			var remainder = (int)((double)numberOfCharacters / 10 % 1 * 10);
			builderA.Append(baseStringA.Substring(0, remainder));
			builderB.Append(baseStringB.Substring(0, remainder));

			ComparisonStringA = builderA.ToString();
			ComparisonStringB = builderB.ToString();
		}
	}
}
