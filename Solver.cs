using System;
using System.IO;

namespace Numeric
{
	public class Solver
	{
		public static RecordSet Records;

		public static Formula Solve(string startFile, RecordSet records)
		{
			Records = records;

			var formula = new Formula();
			if (!string.IsNullOrEmpty(startFile) && File.Exists(startFile))
			{
				formula = Formula.Deserialize(File.ReadAllText(startFile));
			}

			decimal fitness = formula.Apply();
			int complexity = formula.Complexity;

			long generation = 0;
			long successfulGeneration = 0;

			do
			{
				while (!Console.KeyAvailable)
				{
					generation++;
					var newFormula = formula.Clone();
					newFormula.Mutate();
					decimal newFitness = newFormula.Apply();
					int newComplexity = newFormula.Complexity;
					if (newFitness < fitness || (newFitness == fitness && newComplexity < complexity))
					{
						successfulGeneration++;
						formula = newFormula;
						fitness = newFitness;
						complexity = newComplexity;
						WriteStatus(generation, successfulGeneration, fitness, complexity);
					}
					else if (generation % 10000 == 0)
					{
						WriteStatus(generation, successfulGeneration, fitness, complexity);
					}
				}
			} while (Console.ReadKey(true).Key != ConsoleKey.Escape);

			Console.WriteLine();
			return formula;
		}

		private static void WriteStatus(long generation, long successfulGeneration, decimal fitness, int complexity)
		{
			WriteLine(2, "Generation: " + generation);
			WriteLine(3, "Successful: " + successfulGeneration);
			WriteLine(4, "Fitness   : " + fitness);
			WriteLine(5, "Complexity: " + complexity);
		}

		private static void WriteLine(int y, string val)
		{
			Console.SetCursorPosition(0, y);
			Console.Write("".PadLeft(Console.BufferWidth, ' '));
			Console.SetCursorPosition(0, y);
			Console.Write(val);
		}
	}
}
