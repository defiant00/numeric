using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
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
		public List<Fragment> Parts = new List<Fragment>();

		[JsonIgnore]
		public int Complexity
		{
			get { return Parts.Sum(p => p.Complexity); }
		}

		public decimal Apply()
		{
			try
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
								if (Math.Abs(pv) < 1) { pv = pv < 0 ? -1 : 1; }
								val /= pv;
								break;
						}
					}
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
			for (int i = Parts.Count - 1; i >= 0; i--)
			{
				if (Helper.Occurred(Helper.ChanceDelete)) { Parts.RemoveAt(i); }
				else { Parts[i].Mutate(); }
			}

			if (Helper.Occurred(Helper.ChanceAdd)) { Parts.Add(new Fragment()); }

			if (Parts.Count > 1 && Helper.Occurred(Helper.ChanceSwap))
			{
				int ind1 = Helper.Random(Parts.Count);
				int ind2 = Helper.Random(Parts.Count - 1);

				var p = Parts[ind1];
				Parts.RemoveAt(ind1);
				Parts.Insert(ind2, p);
			}
		}

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
			switch (Helper.Random(4))
			{
				case 0:
					return new Number();
				case 1:
				case 2:
					return new Property();
				case 3:
					return new BinOp();
			}
			return null;
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
			if (Helper.Occurred(Helper.ChanceChangeOp)) { GenOp(); }
			Val = Val.Mutate();
		}

		private void GenOp()
		{
			Op = (Operator)Helper.Random((int)Operator.Count);
			EnsureClamp();
		}

		private void GenVal()
		{
			Val = Formula.GenValue();
			EnsureClamp();
		}

		private void EnsureClamp()
		{
			var v = Val as Number;
			if ((Op == Operator.Multiply || Op == Operator.Divide) && v != null && Math.Abs(v.Value) < 1)
			{
				v.Value = v.Value < 0 ? -1 : 1;
			}
		}

		public Fragment Clone()
		{
			return new Fragment { Op = Op, Val = Val.Clone() };
		}

		public override string ToString()
		{
			return Helper.OpString(Op) + Val.ToString();
		}
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
			return this;
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
			var rs = Solver.Records;
			var rec = rs.Records[0];
			int ind = Helper.Random(rec.Values.Count - 1);      // Subtract 1 since we have 1 extra value, the target.
			Name = rec.Values.Keys.ElementAt(ind);
			if (Name == Solver.Records.Target)
			{
				ind++;
				Name = rec.Values.Keys.ElementAt(ind);
			}
		}

		public decimal GetValue(Record r) { return r[Name]; }

		public IValue Mutate()
		{
			if (Helper.Occurred(Helper.ChanceChange)) { GenName(); }
			return this;
		}

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

		private void GenOp() { Op = (Operator)Helper.Random((int)Operator.Count); }

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

			return this;
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
