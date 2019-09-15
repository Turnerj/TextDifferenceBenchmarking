using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using System;
using System.Collections.Generic;
using System.Text;
using TextDifferenceBenchmarking.DiffEngines;

namespace TextDifferenceBenchmarking.Benchmarks
{
	[CoreJob, MemoryDiagnoser, MaxColumn]
	public class HighCapacityBenchmark : TextBenchmarkBase
	{
		[Params(65536)]
		public int NumberOfCharacters;

		[GlobalSetup]
		public void Setup()
		{
			InitialiseComparisonString(NumberOfCharacters);
		}

		[Benchmark(Baseline = true)]
		public void CostDiscard()
		{
			new DmitryCostDiscard().EditSequence(
				ComparisonStringA,
				ComparisonStringB
			);
		}
		[Benchmark]
		public void Best()
		{
			new DmitryBest().EditSequence(
				ComparisonStringA,
				ComparisonStringB
			);
		}
		[Benchmark]
		public void BestParallel()
		{
			new DmitryBestParallel().EditSequence(
				ComparisonStringA,
				ComparisonStringB
			);
		}
	}
}
