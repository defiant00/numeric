using Newtonsoft.Json;
using System;
using System.Text;

namespace Numeric
{
	public interface IValue
	{
		int Complexity { get; }
		decimal GetValue(Record r);
		IValue Mutate();
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
		public IValue Part = Helper.GenValue();

		[JsonIgnore]
		public int Complexity { get { return Part.Complexity; } }

		public decimal Apply()
		{
			try
			{
				decimal fitness = 0;
				foreach (var record in Solver.Records.Records)
				{
					decimal val = Part.GetValue(record);
					fitness += Math.Abs(val - record[Solver.Records.Target]);
				}
				return fitness;
			}
			catch { }

			// If something has gone wrong, return the worst (max) fitness value so it is not treated as a solution.
			return decimal.MaxValue;
		}

		public void Mutate()
		{
			if (Helper.Occurred(Helper.ChanceChangeType)) { Part = Helper.GenValue(); }
			else { Part.Mutate(); }
		}

		public Formula Clone()
		{
			return new Formula { Part = Part.Clone() };
		}

		public string Serialize()
		{
			return JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });
		}

		public static Formula Deserialize(string val)
		{
			return JsonConvert.DeserializeObject<Formula>(val, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });
		}

		public override string ToString() { return Part.ToString(); }
	}

	public class Number : IValue
	{
		[JsonIgnore]
		public int Complexity { get { return 1; } }

		public decimal Value;

		public Number() { UpdateVal(); }

		public decimal GetValue(Record r) { return Value; }

		public IValue Mutate()
		{
			if (Helper.Occurred(Helper.ChanceChange)) { UpdateVal(); }
			return Helper.DoAddInsert(this);
		}

		private void UpdateVal()
		{
			Value += (decimal)Helper.Random(-Helper.NumericChangeMax, Helper.NumericChangeMax);
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
			int ind = Helper.Random(Solver.Records.Keys.Count);
			Name = Solver.Records.Keys[ind];
		}

		public decimal GetValue(Record r) { return r[Name]; }

		public IValue Mutate()
		{
			if (Helper.Occurred(Helper.ChanceChange)) { GenName(); }
			return Helper.DoAddInsert(this);
		}

		public IValue Clone() { return new Property(Name); }

		public override string ToString() { return "{" + Name + "}"; }
	}

	public class BinOp : IValue
	{
		[JsonIgnore]
		public int Complexity
		{
			get { return Left.Complexity + Right.Complexity; }
		}

		public Operator Op;
		public IValue Left, Right;

		public BinOp()
		{
			GenOp();
			GenLeft();
			GenRight();
		}

		public BinOp(IValue left, IValue right)
		{
			Left = left;
			Right = right;
			GenOp();
			if (left == null) { GenLeft(); }
			if (right == null) { GenRight(); }
		}

		private void GenOp() { Op = (Operator)Helper.Random((int)Operator.Count); }

		private void GenLeft() { Left = Helper.GenValue(); }

		private void GenRight() { Right = Helper.GenValue(); }

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
					if (Math.Abs(right) < 1) { right = right < 0 ? -1 : 1; }
					return left / right;
			}
			return 0;
		}

		public IValue Mutate()
		{
			// Return left or right side.
			if (Helper.Occurred(Helper.ChanceTakeChild))
			{
				return Helper.Occurred(0.5) ? Left : Right;
			}

			// Op
			if (Helper.Occurred(Helper.ChanceChangeOp))
			{
				GenOp();
			}
			// Left
			if (Helper.Occurred(Helper.ChanceChangeType))
			{
				GenLeft();
			}
			else { Left = Left.Mutate(); }
			// Right
			if (Helper.Occurred(Helper.ChanceChangeType))
			{
				GenRight();
			}
			else { Right = Right.Mutate(); }

			return Helper.DoAddInsert(this);
		}

		public IValue Clone()
		{
			return new BinOp { Op = Op, Left = Left.Clone(), Right = Right.Clone() };
		}

		public override string ToString()
		{
			var sb = new StringBuilder("(");
			sb.Append(Left.ToString());
			sb.Append(Helper.OpString(Op));
			sb.Append(Right.ToString());
			sb.Append(")");
			return sb.ToString();
		}
	}
}
