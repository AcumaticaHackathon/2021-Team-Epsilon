using PX.Data;
using PX.Objects.CS;
using System;

namespace PX.Objects.PR.Standalone
{
    [Serializable]
	[PXCacheName("Payroll Preferences")]
	public partial class PRSetup : PX.Data.IBqlTable
    {
        #region BatchNumberingID
        public abstract class batchNumberingID : IBqlField
        {
        }
        protected String _BatchNumberingID;
        [PXDBString(10, IsUnicode = true)]
        [PXDefault]
        [PXUIField(DisplayName = "Batch Numbering Sequence")]
        [PXSelector(typeof(Numbering.numberingID), DescriptionField = typeof(Numbering.descr))]
        public virtual String BatchNumberingID
        {
            get
            {
                return this._BatchNumberingID;
            }
            set
            {
                this._BatchNumberingID = value;
            }
        }
        #endregion

        #region BatchNumberingCD
        public abstract class batchNumberingCD : PX.Data.IBqlField { }
        [PXDBString(10, IsUnicode = true, InputMask = "")]
        [PXDefault]
        [PXUIField(DisplayName = "Payroll Batch Numbering Sequence")]
        [PXSelector(typeof(Numbering.numberingID), DescriptionField = typeof(Numbering.descr))]
        public virtual string BatchNumberingCD { get; set; }
        #endregion

        #region HideEmployeeInfo
        public abstract class hideEmployeeInfo : PX.Data.BQL.BqlBool.Field<hideEmployeeInfo> { }
        [PXDBBool]
        [PXUIField(DisplayName = "Hide Employee Name on Transactions")]
        [PXDefault(false)]
        public virtual bool? HideEmployeeInfo { get; set; }
        #endregion HideEmployeeInfo

        #region ProjectCostAssignment
        public abstract class projectCostAssignment : PX.Data.BQL.BqlString.Field<projectCostAssignment> { }
        [PXDBString(3, IsFixed = true)]
        public virtual string ProjectCostAssignment { get; set; }
        #endregion

        #region TimePostingOption
        public abstract class timePostingOption : PX.Data.BQL.BqlString.Field<timePostingOption> { }
        [PXDBString(1, IsUnicode = false, IsFixed = true)]
        public virtual string TimePostingOption { get; set; }
        #endregion

        #region OffBalanceAccountGroupID
        public abstract class offBalanceAccountGroupID : PX.Data.BQL.BqlInt.Field<offBalanceAccountGroupID> { }
        [PXDBInt]
        public virtual int? OffBalanceAccountGroupID { get; set; }
        #endregion

    }
}