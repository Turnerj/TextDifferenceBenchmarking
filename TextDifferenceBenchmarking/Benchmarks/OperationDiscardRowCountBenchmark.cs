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
	public class OperationDiscardRowCountBenchmark : TextBenchmarkBase
	{
		[Params(128, 1024, 4096)]
		public int NumberOfCharacters;

		[Params(64, 128, 256)]
		public int NumberOfOperationRows;

		[GlobalSetup]
		public void Setup()
		{
			InitialiseComparisonString(NumberOfCharacters);
		}

		[Benchmark]
		public void Benchmark()
		{
			new DmitryOperationDiscardFast(NumberOfOperationRows).EditSequence(
				ComparisonStringA,
				ComparisonStringB
			);
		}
	}
}
