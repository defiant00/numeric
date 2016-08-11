using System.Collections.Generic;

namespace Numeric
{
	public class RecordSet
	{
		public List<Record> Records = new List<Record>();
		public List<string> Keys = new List<string>();
		public string Target;

		public void Add(Record r) { Records.Add(r); }

		public void LoadKeys()
		{
			Keys.Clear();
			foreach (string key in Records[0].Values.Keys)
			{
				if (key != Target) { Keys.Add(key); }
			}
		}
	}

	public class Record
	{
		public Dictionary<string, decimal> Values = new Dictionary<string, decimal>();

		public decimal this[string key]
		{
			get { return Values.ContainsKey(key) ? Values[key] : 0; }
			set { Values[key] = value; }
		}
	}
}
