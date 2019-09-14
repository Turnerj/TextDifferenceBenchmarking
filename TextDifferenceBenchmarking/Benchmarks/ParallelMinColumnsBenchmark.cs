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
	public class ParallelMinColumnsBenchmark : TextBenchmarkBase
	{
		[Params(16, 32, 64, 1024)]
		public int NumberOfCharacters;

		[Params(4,8,16,32)]
		public int NumberOfColumnsPerThread;

		[GlobalSetup]
		public void Setup()
		{
			InitialiseComparisonString(NumberOfCharacters);
		}

		[Benchmark]
		public void Benchmark()
		{
			new DmitryBestParallel(NumberOfColumnsPerThread).EditSequence(
				ComparisonStringA,
				ComparisonStringB
			);
		}
	}
}
