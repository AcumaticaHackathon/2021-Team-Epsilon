using PX.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PX.Objects.PR.AUF
{
	public class FfdRecord : AufRecord
	{
		public FfdRecord(int ffdID) : base(AufRecordType.Ffd)
		{
			FfdID = ffdID;
		}

		public override string ToString()
		{
			object[] lineData =
			{
				AufConstants.UnusedField, // Form Name
				AufConstants.UnusedField, // Field Name
				AufConstants.UnusedField, // Item Number
				AufConstants.UnusedField, // Field Value
				AufConstants.UnusedField, // State
				FfdID
			};

			return FormatLine(lineData);
		}

		public virtual int FfdID { get; set; }
	}
}
