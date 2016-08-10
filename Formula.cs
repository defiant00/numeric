using Newtonsoft.Json;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Text;

namespace Numeric
{
	public interface IValue
	{
		int Complexity { get; }
		decimal GetValue(Record r);
		void Mutate();
		IValue Clone();
	}

	public enum Operator
	{
		Add,
		Subtract,
		Multiply,
		Divide,
		Count
	}

	public class Formula
	{
		private static Random random = new Random();

		public const double ChanceAdd = 0.05;
		public const double ChanceChange = 0.02;
		public const double ChanceDelete = 0.01;

		public const double ChanceChangeOp = 0.2;
		public const double ChanceChangeLeft = 0.5;
		public const double ChanceChangeType = 0.1;

		public const double NumericChangeMin = -100;
		public const double NumericChangeMax = 100;

		public const decimal Clamp = 1;     // Clamp at 1 to force the program to use the Multiply Op to multiply.

		public List<Fragment> Parts = new List<Fragment>();

		[JsonIgnore]
		public int Complexity
		{
			get
			{
				int c = 0;
				foreach (var p in Parts) { c += p.Complexity; }
				return c;
			}
		}

		public decimal Apply()
		{
			decimal fitness = 0;
			foreach (var record in Solver.Records.Records)
			{
				decimal val = 0;
				foreach (var p in Parts)
				{
					decimal pv = p.Val.GetValue(record);
					switch (p.Op)
					{
						case Operator.Add:
							val += pv;
							break;
						case Operator.Subtract:
							val -= pv;
							break;
						case Operator.Multiply:
							val *= pv;
							break;
						case Operator.Divide:
							if (Math.Abs(pv) < Clamp) { pv = (pv < 0) ? (-Clamp) : Clamp; }
							val /= pv;
							break;
					}
				}
				fitness += Math.Abs(val - record[Solver.Records.Target]);
			}
			return fitness;
		}

		public void Mutate()
		{
			for (int i = Parts.Count - 1; i >= 0; i--)
			{
				if (Occurred(ChanceDelete)) { Parts.RemoveAt(i); }
				else if (Occurred(ChanceChange)) { Parts[i].Mutate(); }
			}

			if (Occurred(ChanceAdd)) { Parts.Add(new Fragment()); }
		}

		public static bool Occurred(double chance)
		{
			return random.NextDouble() < chance;
		}

		public static double Random(double min, double max)
		{
			return random.NextDouble() * (max - min) + min;
		}

		public static int Random(int max) { return random.Next(max); }

		public Formula Clone()
		{
			var f = new Formula();
			foreach (var p in Parts)
			{
				f.Parts.Add(p.Clone());
			}
			return f;
		}

		public string Serialize()
		{
			return JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });
		}

		public static Formula Deserialize(string val)
		{
			return JsonConvert.DeserializeObject<Formula>(val, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });
		}

		public override string ToString()
		{
			var sb = new StringBuilder("0");
			foreach (var p in Parts) { sb.Append(p.ToString()); }
			return sb.ToString();
		}

		public static IValue GenValue()
		{
			switch (Random(3))
			{
				case 0:
					return new Number();
				case 1:
					return new Property();
				case 2:
					return new BinOp();
			}
			return null;
		}

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
	}

	public class Fragment
	{
		public Operator Op;
		public IValue Val;

		[JsonIgnore]
		public int Complexity
		{
			get { return Val.Complexity; }
		}

		public Fragment()
		{
			GenOp();
			GenVal();
		}

		public void Mutate()
		{
			if (Formula.Occurred(Formula.ChanceChangeOp)) { GenOp(); }
			else { Val.Mutate(); }
		}

		private void GenOp() { Op = (Operator)Formula.Random((int)Operator.Count); }

		private void GenVal() { Val = Formula.GenValue(); }

		public Fragment Clone()
		{
			return new Fragment { Op = Op, Val = Val.Clone() };
		}

		public override string ToString()
		{
			return Formula.OpString(Op) + Val.ToString();
		}
	}

	public class Number : IValue
	{
		[JsonIgnore]
		public int Complexity { get { return 1; } }

		public decimal Value;

		public decimal GetValue(Record r) { return Value; }

		public void Mutate()
		{
			Value += (decimal)Formula.Random(Formula.NumericChangeMin, Formula.NumericChangeMax);
		}

		public IValue Clone() { return new Number { Value = Value }; }

		public override string ToString() { return Value.ToString(); }
	}

	public class Property : IValue
	{
		[JsonIgnore]
		public int Complexity { get { return 1; } }

		public string Name;

		public Property(string name) { Name = name; }

		public Property() { GenName(); }

		private void GenName()
		{
			var rs = Solver.Records;
			var rec = rs.Records[Formula.Random(rs.Records.Count)];
			int ind = Formula.Random(rec.Values.Count);
			Name = rec.Values.Keys.ElementAt(ind);
			if (Name == Solver.Records.Target)
			{
				ind = (ind + 1) % rs.Records.Count;
				Name = rec.Values.Keys.ElementAt(ind);
			}
		}

		public decimal GetValue(Record r) { return r[Name]; }

		public void Mutate() { GenName(); }

		public IValue Clone() { return new Property(Name); }

		public override string ToString() { return "{" + Name + "}"; }
	}

	public class BinOp : IValue
	{
		[JsonIgnore]
		public int Complexity
		{
			get { return Left.Complexity + Right.Complexity + 1; }
		}

		public Operator Op;
		public IValue Left, Right;

		public BinOp()
		{
			GenOp();
			GenLeft();
			GenRight();
		}

		private void GenOp() { Op = (Operator)Formula.Random((int)Operator.Count); }

		private void GenLeft() { Left = Formula.GenValue(); }

		private void GenRight() { Right = Formula.GenValue(); }

		public decimal GetValue(Record r)
		{
			decimal left = Left.GetValue(r);
			decimal right = Right.GetValue(r);
			switch (Op)
			{
				case Operator.Add:
					return left + right;
				case Operator.Subtract:
					return left - right;
				case Operator.Multiply:
					return left * right;
				case Operator.Divide:
					if (Math.Abs(right) < Formula.Clamp) { right = (right < 0) ? (-Formula.Clamp) : Formula.Clamp; }
					return left / right;
			}
			return 0;
		}

		public void Mutate()
		{
			if (Formula.Occurred(Formula.ChanceChangeOp))
			{
				GenOp();
			}
			else if (Formula.Occurred(Formula.ChanceChangeLeft))
			{
				if (Formula.Occurred(Formula.ChanceChangeType))
				{
					GenLeft();
				}
				else { Left.Mutate(); }
			}
			else
			{
				if (Formula.Occurred(Formula.ChanceChangeType))
				{
					GenRight();
				}
				else { Right.Mutate(); }
			}
		}

		public IValue Clone()
		{
			return new BinOp { Op = Op, Left = Left.Clone(), Right = Right.Clone() };
		}

		public override string ToString()
		{
			var sb = new StringBuilder("(");
			sb.Append(Left.ToString());
			sb.Append(Formula.OpString(Op));
			sb.Append(Right.ToString());
			sb.Append(")");
			return sb.ToString();
		}
	}
}
