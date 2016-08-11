using System;

namespace Numeric
{
	public class Helper
	{
		private static Random random = new Random();

		public const double ChanceAdd = 0.02;
		public const double ChanceChange = 0.05;
		public const double ChanceChangeOp = 0.01;
		public const double ChanceChangeType = 0.01;
		public const double ChanceTakeChild = 0.01;

		public const double NumericChangeMax = 100;

		public static bool Occurred(double chance)
		{
			return random.NextDouble() < chance;
		}

		public static double Random(double min, double max)
		{
			return random.NextDouble() * (max - min) + min;
		}

		public static double Random() { return random.NextDouble(); }

		public static int Random(int max) { return random.Next(max); }

		public static string OpString(Operator op)
		{
			switch (op)
			{
				case Operator.Add:
					return " + ";
				case Operator.Subtract:
					return " - ";
				case Operator.Multiply:
					return " * ";
				case Operator.Divide:
					return " / ";
			}
			return " (unknown op) ";
		}

		public static IValue DoAddInsert(IValue current)
		{
			if (Occurred(ChanceAdd))
			{
				IValue left = null;
				IValue right = null;

				if (Occurred(0.5))
				{
					left = current;
				}
				else
				{
					right = current;
				}

				return new BinOp(left, right);
			}
			return current;
		}

		public static IValue GenValue()
		{
			double r = Helper.Random();
			if (r < 0.40)
			{
				return new Number();
			}
			else if (r < 0.90)
			{
				return new Property();
			}
			return new BinOp();
		}
	}
}
