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
		[Params(1, 32, 128, 512, 1024)]
		public int N;

		private string ComparisonString;

		private class Config : ManualConfig
		{
			public Config()
			{
				//Add(Job.DryCore.WithInvocationCount(200).WithIterationCount(10));
				Add(Job.Core);
				Add(MemoryDiagnoser.Default);
				Add(TargetMethodColumn.Method, StatisticColumn.Max);
			}
		}

		[GlobalSetup]
		public void Setup()
		{
			var baseString = "abcdefghij";
			var builder = new StringBuilder(baseString.Length * N);
			for (int i = 0, l = N; i < l; i++)
			{
				builder.Append(baseString);
			}
			ComparisonString = builder.ToString();
		}

		[Benchmark(Baseline = true)]
		public void DmitryBychenko()
		{
			new DmitryBychenko().EditSequence(
				ComparisonString,
				ComparisonString
			);
		}
		[Benchmark]
		public void DmitryBest()
		{
			new DmitryBest().EditSequence(
				ComparisonString,
				ComparisonString
			);
		}
		[Benchmark]
		public void DmitryBestParallel()
		{
			new DmitryBestParallel().EditSequence(
				ComparisonString,
				ComparisonString
			);
		}
	}
}
