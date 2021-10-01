using PX.Data;
using PX.Data.WorkflowAPI;
using PX.Objects.AP;
using PX.Objects.AR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.CR.Workflows
{
	public class AccountLocationWorkflow : PXGraphExtension<AccountLocationMaint>
	{
		public static bool IsActive() => false;

		public override void Configure(PXScreenConfiguration configuration)
		{
			LocationWorkflow.Configure(configuration);
		}
	}

}
