using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PX.Data;
using PX.Objects.Common;
using PX.Objects.Common.Extensions;
using PX.Objects.CS;

namespace PX.Objects.GL.DAC
{
	public class OrganizationTypes
	{

		public const string WithoutBranches = "WithoutBranches";
		public const string WithBranchesNotBalancing = "NotBalancing";
		public const string WithBranchesBalancing = "Balancing";
		public const string Group = "Group";

		public class withoutBranches : PX.Data.BQL.BqlString.Constant<withoutBranches>
		{
			public withoutBranches() : base(WithoutBranches) {; }
		}

		public class withBranchesNotBalancing : PX.Data.BQL.BqlString.Constant<withBranchesNotBalancing>
		{
			public withBranchesNotBalancing() : base(WithBranchesNotBalancing) {; }
		}

		public class withBranchesBalancing : PX.Data.BQL.BqlString.Constant<withBranchesBalancing>
		{
			public withBranchesBalancing() : base(WithBranchesBalancing) {; }
		}
		public class group : PX.Data.BQL.BqlString.Constant<group>
		{
			public group() : base(Group) {; }
		}

		public class ListAttribute : PXStringListAttribute
		{
			protected string[] ExcludedTypes { get; set; }

			public ListAttribute() : base() { }

			public ListAttribute(params string[] excludedTypes) : base() 
			{
				ExcludedTypes = excludedTypes;
			}

			public override void CacheAttached(PXCache sender)
			{
				List<string> orgTypesValues = new List<string>();
				List<string> orgTypesLabels = new List<string>();

				orgTypesValues.Add(WithoutBranches);
				orgTypesLabels.Add(Messages.WithoutBranches);
				

				if (PXAccess.FeatureInstalled<FeaturesSet.branch>())
				{
					orgTypesValues.Add(WithBranchesNotBalancing);
					orgTypesLabels.Add(Messages.WithBranchesNotRequiringBalancing);

					if (PXAccess.FeatureInstalled<FeaturesSet.interBranch>())
					{
						orgTypesValues.Add(WithBranchesBalancing);
						orgTypesLabels.Add(Messages.WithBranchesRequiringBalancing);
					}
				}

				if (PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>())
				{
					orgTypesValues.Add(Group);
					orgTypesLabels.Add(Messages.Group);
				}

				if (ExcludedTypes != null)
				{
					foreach(var toRemove in ExcludedTypes)
					{
						if (orgTypesValues.Contains(toRemove))
						{
							var index = orgTypesValues.IndexOf(toRemove);
							orgTypesValues.RemoveAt(index);
							orgTypesLabels.RemoveAt(index);
						}
					}
				}

				_AllowedValues = orgTypesValues.ToArray();
				_AllowedLabels = orgTypesLabels.ToArray();
				_NeutralAllowedLabels = null;

				base.CacheAttached(sender);
			}
		}
	}
}
