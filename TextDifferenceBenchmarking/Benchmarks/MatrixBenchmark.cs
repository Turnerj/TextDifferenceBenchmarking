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
	public class MatrixBenchmark : TextBenchmarkBase
	{
		[Params(16, 256, 1024, 4096, 8192)]
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
		public void MultiDimension()
		{
			new DmitryMultiDimension().EditSequence(
				ComparisonStringA,
				ComparisonStringB
			);
		}
		[Benchmark]
		public void DenseMatrix()
		{
			new DmitryMultiDimension().EditSequence(
				ComparisonStringA,
				ComparisonStringB
			);
		}
		[Benchmark]
		public void InlineMatrix()
		{
			new DmitryInlineMatrix().EditSequence(
				ComparisonStringA,
				ComparisonStringB
			);
		}
		[Benchmark]
		public void InlineArrayPoolMatrix()
		{
			new DmitryLargeArrayPoolMatrix().EditSequence(
				ComparisonStringA,
				ComparisonStringB
			);
		}
		[Benchmark]
		public void LargeArrayPoolMatrix()
		{
			new DmitryLargeArrayPoolMatrix().EditSequence(
				ComparisonStringA,
				ComparisonStringB
			);
		}
		[Benchmark]
		public void InlineSpanPointerMatrix()
		{
			new DmitryInlineSpanMatrix().EditSequence(
				ComparisonStringA,
				ComparisonStringB
			);
		}
	}
}
