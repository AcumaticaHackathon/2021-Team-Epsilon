using PX.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PX.Objects.PR.AUF
{
	public class EfbRecord : AufRecord
	{
		public EfbRecord(string benefitCode) : base(AufRecordType.Efb)
		{
			BenefitID = PimRecord.GetPimIDFromName(benefitCode);
		}

		public override string ToString()
		{
			object[] lineData =
			{
				BenefitID,
				HourlyRate,
				'E' // Payment method
			};

			return FormatLine(lineData);
		}
		
		public virtual int BenefitID { get; set; }
		public virtual decimal? HourlyRate { get; set; }
	}
}
