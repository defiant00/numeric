using System;
using System.IO;

namespace Numeric
{
	class Program
	{
		static void Main(string[] args)
		{
			var config = new Config(args);

			Console.Clear();
			Console.WriteLine("Numeric v0.8");

			var records = new RecordSet { Target = "target" };
			var r = new Record();
			r["first"] = 1;
			r["second"] = 500;
			r["third"] = -12;
			r["target"] = 17;
			records.Add(r);
			r = new Record();
			r["first"] = 1024;
			r["second"] = -12;
			r["third"] = 45;
			r["target"] = 125;
			records.Add(r);

			Formula formula = null;
			if (config.IsSet("start"))
			{
			}

			formula = Solver.Solve(config.IsSet("start") ? config["start"] : null, records);

			File.WriteAllText("formula.json", formula.Serialize());
			File.WriteAllText("formula.txt", formula.ToString());
		}
	}
}
