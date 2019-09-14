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
	public class MicroBenchmark : TextBenchmarkBase
	{
		[Params(16, 256, 1024, 4096)]
		public int NumberOfCharacters;

		[GlobalSetup]
		public void Setup()
		{
			InitialiseComparisonString(NumberOfCharacters);
		}

		[Benchmark(Baseline = true)]
		public void Baseline()
		{
			new DmitryBychenko().EditSequence(
				ComparisonStringA,
				ComparisonStringB
			);
		}
		[Benchmark]
		public void ResultStack()
		{
			new DmitryStack().EditSequence(
				ComparisonStringA,
				ComparisonStringB
			);
		}
		[Benchmark]
		public void InlineLength()
		{
			new DmitryInlineLength().EditSequence(
				ComparisonStringA,
				ComparisonStringB
			);
		}
		[Benchmark]
		public void NoEditElseIf()
		{
			new DmitryNoEditElseIf().EditSequence(
				ComparisonStringA,
				ComparisonStringB
			);
		}
		[Benchmark]
		public void ResultArray()
		{
			new DmitryResultArray().EditSequence(
				ComparisonStringA,
				ComparisonStringB
			);
		}
		[Benchmark]
		public void ResultArrayPointer()
		{
			new DmitryResultArrayMarshal().EditSequence(
				ComparisonStringA,
				ComparisonStringB
			);
		}
		[Benchmark]
		public void RowCaching()
		{
			new DmitryRowCaching().EditSequence(
				ComparisonStringA,
				ComparisonStringB
			);
		}
		[Benchmark]
		public void ShortOverInt()
		{
			new DmitryShortCost().EditSequence(
				ComparisonStringA,
				ComparisonStringB
			);
		}
		[Benchmark]
		public void OperationDiscard()
		{
			new DmitryOperationDiscard().EditSequence(
				ComparisonStringA,
				ComparisonStringB
			);
		}
	}
}
