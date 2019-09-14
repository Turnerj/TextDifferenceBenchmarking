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
	public class TextDiffBenchmark : TextBenchmarkBase
	{
		[Params(16, 256, 1024, 4096)]
		public int NumberOfCharacters;

		[GlobalSetup]
		public void Setup()
		{
			InitialiseComparisonString(NumberOfCharacters);
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
		//[Benchmark]
		//public void DmitryBestParallel()
		//{
		//	new DmitryBestParallel().EditSequence(
		//		ComparisonStringA,
		//		ComparisonStringB
		//	);
		//}
	}
}
