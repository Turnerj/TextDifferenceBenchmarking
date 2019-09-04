using BenchmarkDotNet.Running;
using System;
using System.Linq;
using System.Reflection;
using System.Text;
using TextDifferenceBenchmarking.DiffEngines;

namespace TextDifferenceBenchmarking
{
	class Program
	{
		static void Main(string[] args)
		{
			new DiffEngineValidator().Validate();
			BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
		}
	}
}
