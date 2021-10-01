using System;
using PX.Data;
using PX.Data.EP;
using PX.Objects.CS;
using PX.Objects.CT;

namespace SP.Objects.CR
{
    public class ContractTemplateExt : PXCacheExtension<ContractTemplate>
    {
        [PXDimensionSelector(ContractTemplateAttribute.DimensionName, typeof(Search<ContractTemplate.contractCD, Where<ContractTemplate.isTemplate, Equal<boolTrue>, And<ContractTemplate.baseType, Equal<Contract.ContractBaseType>>>>), typeof(ContractTemplate.contractCD), DescriptionField = typeof(ContractTemplate.description))]
        [PXDBString(IsUnicode = true, IsKey = true, InputMask = "")]
        [PXDefault]
        [PXUIField(DisplayName = "Contract Template", Visibility = PXUIVisibility.SelectorVisible)]
        [PXFieldDescription]
        public virtual String ContractCD
        {
            get;
            set;
        }
    }
}
