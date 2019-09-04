using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using System;
using System.Collections.Generic;
using System.Text;
using TextDifferenceBenchmarking.DiffEngines;

namespace TextDifferenceBenchmarking
{
	[Config(typeof(Config))]
	public class TextDiffBenchmark
	{
		[Params(16,256,1024,4096)]
		public int NumberOfChars;

		private string ComparisonStringA;

		private string ComparisonStringB;

		private class Config : ManualConfig
		{
			public Config()
			{
				Add(Job.Core);
				Add(MemoryDiagnoser.Default);
				Add(TargetMethodColumn.Method, StatisticColumn.Max);
			}
		}

		[GlobalSetup]
		public void Setup()
		{
			var baseStringA = "aabbccddee";
			var baseStringB = "abcdeabcde";
			var builderA = new StringBuilder(NumberOfChars);
			var builderB = new StringBuilder(NumberOfChars);

			var charBlocks = (int)Math.Floor((double)NumberOfChars / 10);
			for (int i = 0, l = charBlocks; i < l; i++)
			{
				builderA.Append(baseStringA);
				builderB.Append(baseStringB);
			}

			var remainder = (int)((double)NumberOfChars / 10 % 1 * 10);
			builderA.Append(baseStringA.Substring(0, remainder));
			builderB.Append(baseStringB.Substring(0, remainder));

			ComparisonStringA = builderA.ToString();
			ComparisonStringB = builderB.ToString();
		}

		[Benchmark(Baseline = true)]
		public void DmitryBychenko()
		{
			new DmitryBychenko().EditSequence(
				ComparisonStringA,
				ComparisonStringB
			);
		}
		[Benchmark]
		public void DmitryBest()
		{
			new DmitryBest().EditSequence(
				ComparisonStringA,
				ComparisonStringB
			);
		}
		[Benchmark]
		public void DmitryBestParallel()
		{
			new DmitryBestParallel().EditSequence(
				ComparisonStringA,
				ComparisonStringB
			);
		}
	}
}
