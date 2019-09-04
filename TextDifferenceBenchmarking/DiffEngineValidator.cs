using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using TextDifferenceBenchmarking.DiffEngines;

namespace TextDifferenceBenchmarking
{
	public class DiffEngineValidator
	{
		private ITextDiff[] EnginesToValidate;
		
		public DiffEngineValidator()
		{
			var standard = new DmitryBychenko();

			var engineTypes = Assembly.GetExecutingAssembly().GetTypes()
				.Where(t => t.IsClass && typeof(ITextDiff).IsAssignableFrom(t) && t != typeof(DmitryBychenko))
				.ToArray();

			EnginesToValidate = engineTypes.Select(t => Activator.CreateInstance(t) as ITextDiff).ToArray();
		}

		public void Validate()
		{
			var standard = new DmitryBychenko();
			var baselineResults = GetTestResults(standard);

			foreach (var engine in EnginesToValidate)
			{
				var engineResult = GetTestResults(engine);
				for (int i = 0, l = engineResult.Count; i < l; i++)
				{
					var errorMessage = AssertAreEqual(baselineResults[i], engineResult[i]);
					if (errorMessage != null)
					{
						throw new InvalidOperationException($"{engine.GetType().Name} on test {i + 1} failed. {errorMessage}");
					}
				}
			}
		}

		private string AssertAreEqual(EditOperation[] expected, EditOperation[] actual)
		{
			if (expected.Length != actual.Length)
			{
				return $"Operation Count Mismatch: Expected {expected.Length} but received {actual.Length}";
			}

			for (int i = 0, l = expected.Length; i < l; i++)
			{
				var expectedItem = expected[i];
				var actualItem = actual[i];

				if (expectedItem.ValueFrom != actualItem.ValueFrom || expectedItem.ValueTo != actualItem.ValueTo || expectedItem.Operation != actualItem.Operation)
				{
					return "Invalid Operation";
				}
			}

			return null;
		}

		private List<EditOperation[]> GetTestResults(ITextDiff textDiff)
		{
			var results = new List<EditOperation[]>();

			var testA1 = "Hello World!";
			var testA2 = "HeLLo Wolrd!";
			results.Add(textDiff.EditSequence(testA1, testA2));

			var testB1 = "Nulla nec ipsum sit amet enim malesuada dapibus vel quis mi.";
			var testB2 = "Nulla nec ipsum sit amet - Hello - enim malesuada dapibus vel quis mi.";
			results.Add(textDiff.EditSequence(testB1, testB2));

			var testC1 = "Nulla nec ipsum sit amet enim malesuada dapibus vel quis mi. Proin lacinia arcu non blandit mattis.";
			var testC2 = "Nulla nec ipsum sit amet enim malesuada dapibus vel quis mi. Proin lacinia arcu non blandit mattis.";
			results.Add(textDiff.EditSequence(testC1, testC2));

			var testD1 = "Hello World!";
			var testD2 = "";
			results.Add(textDiff.EditSequence(testD1, testD2));

			//Extra long string checks
			var baseString = "abcdefghij";
			var counts = new[] { 128, 512 };
			for (int i = 0, l = counts.Length; i < l; i++)
			{
				var builder = new StringBuilder();
				for (int i2 = 0, l2 = counts[i]; i2 < l2; i2++)
				{
					builder.Append(baseString);
				}

				var comparisonString = builder.ToString();
				results.Add(textDiff.EditSequence(comparisonString, comparisonString));
			}

			return results;
		}
	}
}
