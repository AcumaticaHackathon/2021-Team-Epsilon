using PX.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PX.Objects.PR.AUF
{
	public class EffRecord : AufRecord
	{
		public EffRecord(int ffdID, string fieldValue) : base(AufRecordType.Eff)
		{
			FfdID = ffdID;

			if (bool.TryParse(fieldValue, out bool boolValue))
			{
				FieldValue = boolValue ? AufConstants.SelectedBox : string.Empty;
			}
			else
			{
				FieldValue = fieldValue;
			}
		}

		public override string ToString()
		{
			object[] lineData =
			{
				FfdID,
				FieldValue
			};

			return FormatLine(lineData);
		}

		public virtual int FfdID { get; set; }
		public virtual string FieldValue { get; set; }
	}

	public class EffIDComparer : IEqualityComparer<EffRecord>
	{
		public bool Equals(EffRecord x, EffRecord y)
		{
			return x.FfdID == y.FfdID;
		}

		public int GetHashCode(EffRecord obj)
		{
			return obj.FfdID.GetHashCode();
		}
	}
}
