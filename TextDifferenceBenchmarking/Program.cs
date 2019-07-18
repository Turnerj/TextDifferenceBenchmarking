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
			ValidateEngines();
			BenchmarkRunner.Run<TextDiffBenchmark>();
			//Console.ReadLine();
		}

		private static void ValidateEngines()
		{
			var standard = new DmitryBychenko();
			var engineTypes = Assembly.GetExecutingAssembly().GetTypes()
				.Where(t => t.IsClass && typeof(ITextDiff).IsAssignableFrom(t) && t != typeof(DmitryBychenko))
				.ToArray();

			foreach (var engineType in engineTypes)
			{
				var engine = Activator.CreateInstance(engineType) as ITextDiff;
				CompareEngine(standard, engine);
			}
		}

		private static void CompareEngine(ITextDiff standard, ITextDiff tested)
		{
			static void AreEqual(EditOperation[] expected, EditOperation[] actual)
			{
				if (expected.Length != actual.Length)
				{
					throw new ArgumentException($"Invalid number of operations! Expected {expected.Length} but received {actual.Length}");
				}

				for (int i = 0, l = expected.Length; i < l; i++)
				{
					var expectedItem = expected[i];
					var actualItem = actual[i];

					if (expectedItem.ValueFrom != actualItem.ValueFrom || expectedItem.ValueTo != actualItem.ValueTo || expectedItem.Operation != actualItem.Operation)
					{
						throw new ArgumentException("Invalid operation!");
						throw new Exception();
					}
				}
			}
			
			var testA1 = "Hello World!";
			var testA2 = "HeLLo Wolrd!";
			AreEqual(standard.EditSequence(testA1, testA2), tested.EditSequence(testA1, testA2));
			var testB1 = "Nulla nec ipsum sit amet enim malesuada dapibus vel quis mi.";
			var testB2 = "Nulla nec ipsum sit amet - Hello - enim malesuada dapibus vel quis mi.";
			AreEqual(standard.EditSequence(testB1, testB2), tested.EditSequence(testB1, testB2));
			var testC1 = "Nulla nec ipsum sit amet enim malesuada dapibus vel quis mi. Proin lacinia arcu non blandit mattis.";
			var testC2 = "Nulla nec ipsum sit amet enim malesuada dapibus vel quis mi. Proin lacinia arcu non blandit mattis.";
			AreEqual(standard.EditSequence(testC1, testC2), tested.EditSequence(testC1, testC2));
			var testD1 = "Hello World!";
			var testD2 = "";
			AreEqual(standard.EditSequence(testD1, testD2), tested.EditSequence(testD1, testD2));

			//Extra long string checks (same as used in benchmark)
			var baseString = "abcdefghij";
			var counts = new[] { 8, 32, 64, 128, 256, 512 };
			for (int i = 0, l = counts.Length; i < l; i++)
			{
				var builder = new StringBuilder();
				for (int i2 = 0, l2 = counts[i]; i2 < l2; i2++)
				{
					builder.Append(baseString);
				}
				
				var comparisonString = builder.ToString();
				AreEqual(
					standard.EditSequence(comparisonString, comparisonString), 
					tested.EditSequence(comparisonString, comparisonString)
				);
			}
		}
	}
}
