using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PX.Common;
using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects;
using PX.Objects.AP;
using PX.Objects.AR;

namespace PX.Objects.CA.Light
{
	[Serializable]
	[PXTable]
	[PXHidden]
	public partial class CAAdj : IBqlTable
	{
		#region AdjRefNbr
		public abstract class adjRefNbr : PX.Data.BQL.BqlString.Field<adjRefNbr> { }

		[PXDBString(15, IsUnicode = true, IsKey = true, InputMask = ">CCCCCCCCCCCCCCC")]
		public virtual string AdjRefNbr
		{
			get;
			set;
		}
		#endregion
		#region AdjTranType
		public abstract class adjTranType : PX.Data.BQL.BqlString.Field<adjTranType> { }

		[PXDBString(3, IsFixed = true)]
		public virtual string AdjTranType
		{
			get;
			set;
		}
		#endregion
		#region CashAccountID
		public abstract class cashAccountID : PX.Data.BQL.BqlInt.Field<cashAccountID> { }

		[PXDBInt]
		public virtual int? CashAccountID
		{
			get;
			set;
		}
		#endregion
		#region DrCr
		public abstract class drCr : PX.Data.BQL.BqlString.Field<drCr> { }

		[PXDBString(1, IsFixed = true)]
		public virtual string DrCr
		{
			get;
			set;
		}
		#endregion
		#region Released
		public abstract class released : PX.Data.BQL.BqlBool.Field<released> { }

		[PXDBBool]
		public virtual bool? Released
		{
			get;
			set;
		}
		#endregion
		#region ExtRefNbr
		public abstract class extRefNbr : PX.Data.BQL.BqlString.Field<extRefNbr> { }

		[PXDBString(40, IsUnicode = true)]
		public virtual string ExtRefNbr
		{
			get;
			set;
		}
		#endregion
		#region Status
		public abstract class status : PX.Data.BQL.BqlString.Field<status> { }

		[PXDBString(1, IsFixed = true)]
		public virtual string Status
		{
			get;
			set;
		}
		#endregion
		#region CuryInfoID
		public abstract class curyInfoID : PX.Data.BQL.BqlLong.Field<curyInfoID> { }

		[PXDBLong]
		public virtual long? CuryInfoID
		{
			get;
			set;
		}
		#endregion
		#region CuryID
		public abstract class curyID : PX.Data.BQL.BqlString.Field<curyID> { }

		[PXDBString(5, IsUnicode = true, InputMask = ">LLLLL")]
		public virtual string CuryID
		{
			get;
			set;
		}
		#endregion
		#region TranAmt
		public abstract class tranAmt : PX.Data.BQL.BqlDecimal.Field<tranAmt> { }

		[PXDBDecimal(4)]
		public virtual decimal? TranAmt
		{
			get;
			set;
		}
		#endregion
		#region CuryTranAmt
		public abstract class curyTranAmt : PX.Data.BQL.BqlDecimal.Field<curyTranAmt> { }

		[PXDBDecimal(4)]
		public virtual decimal? CuryTranAmt
		{
			get;
			set;
		}
		#endregion
		#region TranDate
		public abstract class tranDate : PX.Data.BQL.BqlDateTime.Field<tranDate> { }

		[PXDBDate]
		public virtual DateTime? TranDate
		{
			get;
			set;
		}
		#endregion
		#region DepositAsBatch
		public abstract class depositAsBatch : PX.Data.BQL.BqlBool.Field<depositAsBatch> { }

		[PXDBBool]
		public virtual Boolean? DepositAsBatch
		{
			get;
			set;
		}
		#endregion
		#region DepositAfter
		public abstract class depositAfter : PX.Data.BQL.BqlDateTime.Field<depositAfter> { }

		[PXDBDate]
		public virtual DateTime? DepositAfter
		{
			get;
			set;
		}
		#endregion
		#region DepositDate
		public abstract class depositDate : PX.Data.BQL.BqlDateTime.Field<depositDate> { }

		[PXDBDate]
		public virtual DateTime? DepositDate
		{
			get;
			set;
		}
		#endregion
		#region Deposited
		public abstract class deposited : PX.Data.BQL.BqlBool.Field<deposited> { }

		[PXDBBool]
		public virtual Boolean? Deposited
		{
			get;
			set;
		}
		#endregion
		#region DepositType
		public abstract class depositType : PX.Data.BQL.BqlString.Field<depositType> { }

		[PXDBString(3, IsFixed = true)]
		public virtual String DepositType
		{
			get;
			set;
		}
		#endregion
		#region DepositNbr
		public abstract class depositNbr : PX.Data.BQL.BqlString.Field<depositNbr> { }

		[PXDBString(15, IsUnicode = true)]
		public virtual String DepositNbr
		{
			get;
			set;
		}
		#endregion
	}

	[Serializable]
	[PXTable]
	public partial class ARPayment : AR.ARRegister
	{
		#region Keys
		public new class PK : PrimaryKeyOf<ARPayment>.By<docType, refNbr>
		{
			public static ARPayment Find(PXGraph graph, string docType, string refNbr) => FindBy(graph, docType, refNbr);
		}
		#endregion
		#region ExtRefNbr
		public abstract class extRefNbr : PX.Data.BQL.BqlString.Field<extRefNbr> { }
		[PXDBString(40, IsUnicode = true)]
		public virtual string ExtRefNbr
		{
			get;
			set;
		}
		#endregion
		#region DocType
		public new abstract class docType : PX.Data.BQL.BqlString.Field<docType> { }
		#endregion
		#region RefNbr
		public new abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr> { }
		#endregion

		#region PaymentMethodID
		public abstract class paymentMethodID : PX.Data.BQL.BqlString.Field<paymentMethodID> { }
		[PXDBString(10, IsUnicode = true)]
		public virtual string PaymentMethodID
		{
			get;
			set;
		}
		#endregion
		#region PMInstanceID
		public abstract class pMInstanceID : PX.Data.BQL.BqlInt.Field<pMInstanceID> { }
		[PXDBInt]
		public virtual int? PMInstanceID
		{
			get;
			set;
		}
		#endregion

		#region CashAccountID
		public abstract class cashAccountID : PX.Data.BQL.BqlInt.Field<cashAccountID> { }
		[PXDBInt]
		public virtual int? CashAccountID
		{
			get;
			set;
		}
		#endregion

		#region BatchNbr
		public new abstract class batchNbr : PX.Data.BQL.BqlString.Field<batchNbr> { }
		#endregion
		#region Voided
		public new abstract class voided : PX.Data.BQL.BqlBool.Field<voided> { }
		#endregion

		#region Released
		public new abstract class released : PX.Data.BQL.BqlBool.Field<released> { }
		#endregion
		#region CATranID
		public abstract class cATranID : PX.Data.BQL.BqlLong.Field<cATranID> { }
		[PXDBLong]
		public virtual long? CATranID
		{
			get;
			set;
		}
		#endregion
		#region CuryOrigDocAmt
		public new abstract class curyOrigDocAmt : PX.Data.BQL.BqlDecimal.Field<curyOrigDocAmt> { }
		#endregion

		#region ARDepositAsBatch
		public abstract class depositAsBatch : PX.Data.BQL.BqlBool.Field<depositAsBatch> { }
		[PXDBBool]
		public virtual bool? DepositAsBatch
		{
			get;
			set;
		}
		#endregion
		#region DepositAfter
		public abstract class depositAfter : PX.Data.BQL.BqlDateTime.Field<depositAfter> { }
		[PXDBDate]
		public virtual DateTime? DepositAfter
		{
			get;
			set;
		}
		#endregion
		#region DepositDate
		public abstract class depositDate : PX.Data.BQL.BqlDateTime.Field<depositDate> { }
		[PXDBDate]
		public virtual DateTime? DepositDate
		{
			get;
			set;
		}
		#endregion
		#region Deposited
		public abstract class deposited : PX.Data.BQL.BqlBool.Field<deposited> { }
		[PXDBBool]
		public virtual bool? Deposited
		{
			get;
			set;
		}
		#endregion
		#region DepositType
		public abstract class depositType : PX.Data.BQL.BqlString.Field<depositType> { }
		[PXDBString(3, IsFixed = true)]

		public virtual string DepositType
		{
			get;
			set;
		}
		#endregion
		#region DepositNbr
		public abstract class depositNbr : PX.Data.BQL.BqlString.Field<depositNbr> { }
		[PXDBString(15, IsUnicode = true)]
		public virtual string DepositNbr
		{
			get;
			set;
		}
		#endregion
	}
	
	[Serializable]
	[PXTable]
	public partial class APPayment : AP.APRegister
	{
		#region Keys
		public new class PK : PrimaryKeyOf<APPayment>.By<docType, refNbr>
		{
			public static APPayment Find(PXGraph graph, string docType, string refNbr) => FindBy(graph, docType, refNbr);
		}
		#endregion
		#region ExtRefNbr
		public abstract class extRefNbr : PX.Data.BQL.BqlString.Field<extRefNbr> { }
		[PXDBString(40, IsUnicode = true)]
		public virtual string ExtRefNbr
		{
			get;
			set;
		}
		#endregion
		#region DocType
		public new abstract class docType : PX.Data.BQL.BqlString.Field<docType> { }
		#endregion
		#region RefNbr
		public new abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr> { }
		#endregion

		#region BranchID
		public new abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID> { }
		#endregion
		#region PaymentMethodID
		public abstract class paymentMethodID : PX.Data.BQL.BqlString.Field<paymentMethodID> { }
		[PXDBString(10, IsUnicode = true)]
		public virtual string PaymentMethodID
		{
			get;
			set;
		}
		#endregion
		#region CashAccountID
		public abstract class cashAccountID : PX.Data.BQL.BqlInt.Field<cashAccountID> { }
		[PXDBInt]
		public virtual int? CashAccountID
		{
			get;
			set;
		}
		#endregion

		#region BatchNbr
		public new abstract class batchNbr : PX.Data.BQL.BqlString.Field<batchNbr> { }
		#endregion
		#region Released
		public new abstract class released : PX.Data.BQL.BqlBool.Field<released> { }
		#endregion
		#region OpenDoc
		public new abstract class openDoc : PX.Data.BQL.BqlBool.Field<openDoc> { }
		#endregion
		#region Hold
		public new abstract class hold : PX.Data.BQL.BqlBool.Field<hold> { }
		#endregion
		#region Voided
		public new abstract class voided : PX.Data.BQL.BqlBool.Field<voided> { }
		#endregion

		#region CATranID
		public abstract class cATranID : PX.Data.BQL.BqlLong.Field<cATranID> { }

		[PXDBLong]
		public virtual long? CATranID
		{
			get;
			set;
		}
		#endregion
		#region CuryOrigDocAmt
		public new abstract class curyOrigDocAmt : PX.Data.BQL.BqlDecimal.Field<curyOrigDocAmt> { }
		#endregion
		#region DepositAsBatch
		public abstract class depositAsBatch : PX.Data.BQL.BqlBool.Field<depositAsBatch> { }
		[PXDBBool]
		public virtual bool? DepositAsBatch
		{
			get;
			set;
		}
		#endregion
		#region DepositAfter
		public abstract class depositAfter : PX.Data.BQL.BqlDateTime.Field<depositAfter> { }
		[PXDBDate]
		public virtual DateTime? DepositAfter
		{
			get;
			set;
		}
		#endregion
		#region Deposited
		public abstract class deposited : PX.Data.BQL.BqlBool.Field<deposited> { }
		[PXDBBool]
		public virtual bool? Deposited
		{
			get;
			set;
		}
		#endregion
		#region DepositDate
		public abstract class depositDate : PX.Data.BQL.BqlDateTime.Field<depositDate> { }
		[PXDBDate]
		public virtual DateTime? DepositDate
		{
			get;
			set;
		}
		#endregion
		#region DepositType
		public abstract class depositType : PX.Data.BQL.BqlString.Field<depositType> { }
		[PXDBString(3, IsFixed = true)]
		public virtual string DepositType
		{
			get;
			set;
		}
		#endregion
		#region DepositNbr
		public abstract class depositNbr : PX.Data.BQL.BqlString.Field<depositNbr> { }
		[PXDBString(15, IsUnicode = true)]
		public virtual string DepositNbr
		{
			get;
			set;
		}
		#endregion
	}

	[PXTable]
	[Serializable]
	public partial class ARInvoice : ARRegister
	{
		#region Keys
		public new class PK : PrimaryKeyOf<ARInvoice>.By<docType, refNbr>
		{
			public static ARInvoice Find(PXGraph graph, string docType, string refNbr) => FindBy(graph, docType, refNbr);
		}
		#endregion

		#region DocType
		public new abstract class docType : PX.Data.BQL.BqlString.Field<docType> { }
		#endregion
		#region RefNbr
		public new abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr> { }
		#endregion
		#region CustomerID
		public new abstract class customerID : PX.Data.BQL.BqlInt.Field<customerID> { }
		#endregion
		#region CuryID
		public new abstract class curyID : PX.Data.BQL.BqlString.Field<curyID> { }
		#endregion
		#region TermsID
		public abstract class termsID : IBqlField
		{
		}

		[PXDBString(10, IsUnicode = true)]
		public virtual string TermsID
		{
			get;
			set;
		}
		#endregion
		#region InvoiceNbr
		public abstract class invoiceNbr : PX.Data.BQL.BqlString.Field<invoiceNbr> { }
		[PXDBString(40, IsUnicode = true)]
		public virtual string InvoiceNbr
		{
			get;
			set;
		}
		#endregion
		#region DocDate
		public new abstract class docDate : PX.Data.BQL.BqlDateTime.Field<docDate> { }
		#endregion
		#region CuryInfoID
		public new abstract class curyInfoID : PX.Data.BQL.BqlLong.Field<curyInfoID> { }
		#endregion
		#region CuryDocBal
		public new abstract class curyDocBal : PX.Data.BQL.BqlDecimal.Field<curyDocBal> { }
		#endregion
		#region DocBal
		public new abstract class docBal : PX.Data.BQL.BqlDecimal.Field<docBal> { }
		#endregion
		#region CuryDiscBal
		public new abstract class curyDiscBal : PX.Data.BQL.BqlDecimal.Field<curyDiscBal> { }
		#endregion
		#region DiscBal
		public new abstract class discBal : PX.Data.BQL.BqlDecimal.Field<discBal> { }
		#endregion
		#region Released
		public new abstract class released : PX.Data.BQL.BqlBool.Field<released> { }
		#endregion
		#region OpenDoc
		public new abstract class openDoc : PX.Data.BQL.BqlBool.Field<openDoc> { }
		#endregion
		#region Voided
		public new abstract class voided : PX.Data.BQL.BqlBool.Field<voided> { }
		#endregion
		#region PaymentsByLinesAllowed
		public new abstract class paymentsByLinesAllowed : PX.Data.BQL.BqlBool.Field<paymentsByLinesAllowed> { }
		#endregion
		#region PaymentMethodID
		public abstract class paymentMethodID : PX.Data.IBqlField
		{
		}
		
		[PXDBString(10, IsUnicode = true)]
		public virtual String PaymentMethodID
		{
			get;
			set;
		}
		#endregion
		#region PMInstanceID
		public abstract class pMInstanceID : PX.Data.IBqlField
		{
		}

		[PXDBInt()]
		public virtual int? PMInstanceID
		{
			get;
			set;
		}
		#endregion
		#region CashAccountID
		public abstract class cashAccountID : IBqlField
		{
		}
		
		[PXDBInt()]
		public virtual Int32? CashAccountID
		{
			get;
			set;
		}
		#endregion
		#region DocDesc
		public new abstract class docDesc : PX.Data.BQL.BqlString.Field<docDesc> { }
		#endregion
		#region DrCr
		public abstract class drCr : PX.Data.IBqlField
		{
		}

		[PXString(1, IsFixed = true)]
		public virtual string DrCr
		{
			[PXDependsOnFields(typeof(docType))]
			get
			{
				return AR.ARInvoiceType.DrCr(this.DocType);
			}
			set
			{
			}
		}
		#endregion
		#region DiscDate
		public abstract class discDate : PX.Data.BQL.BqlDateTime.Field<discDate> { }

		/// <summary>
		/// The date when the cash discount can be taken in accordance with the <see cref="ARInvoice.TermsID">credit terms</see>.
		/// </summary>
		[PXDBDate]
		public virtual DateTime? DiscDate
		{
			get;
			set;
		}
		#endregion
	}
	
	[Serializable]
	public partial class ARRegister : IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<ARRegister>.By<docType, refNbr>
		{
			public static ARRegister Find(PXGraph graph, string docType, string refNbr) => FindBy(graph, docType, refNbr);
		}
		#endregion

		#region BranchID
		public abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID> { }
        [PXDBInt]
        public virtual int? BranchID
        {
            get;
            set;
        }
        #endregion
		#region DocType
		public abstract class docType : PX.Data.BQL.BqlString.Field<docType> { }
		[PXDBString(3, IsKey = true, IsFixed = true)]
		public virtual string DocType
		{
			get;
			set;
		}
		#endregion
		#region RefNbr
		public abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr> { }
		[PXDBString(15, IsUnicode = true, IsKey = true)]
		public virtual string RefNbr
		{
			get;
			set;
		}
		#endregion
		#region DocDate
		public abstract class docDate : PX.Data.BQL.BqlDateTime.Field<docDate> { }
		[PXDBDate]
		public virtual DateTime? DocDate
		{
			get;
			set;
		}
		#endregion
		#region DueDate
		public abstract class dueDate : IBqlField
		{
		}

		[PXDBDate()]
		public virtual DateTime? DueDate
		{
			get;
			set;
		}
		#endregion
		#region FinPeriodID
		public abstract class finPeriodID : PX.Data.BQL.BqlString.Field<finPeriodID> { }
		[PXDBString]
		public virtual string FinPeriodID
		{
			get;
			set;
		}
		#endregion
		#region CustomerID
		public abstract class customerID : PX.Data.BQL.BqlInt.Field<customerID> { }
		[PXDBInt]
		public virtual int? CustomerID
		{
			get;
			set;
		}
		#endregion
		#region CuryID
		public abstract class curyID : PX.Data.BQL.BqlString.Field<curyID> { }
		[PXDBString(5, IsUnicode = true)]
		public virtual string CuryID
		{
			get;
			set;
		}
		#endregion
		#region CuryInfoID
		public abstract class curyInfoID : PX.Data.BQL.BqlLong.Field<curyInfoID> { }
		[PXDBLong]
		public virtual long? CuryInfoID
		{
			get;
			set;
		}
		#endregion
		#region CuryDocBal
		public abstract class curyDocBal : PX.Data.BQL.BqlDecimal.Field<curyDocBal> { }
		[PXDBDecimal]
		public virtual decimal? CuryDocBal
		{
			get;
			set;
		}
		#endregion
		#region DocBal
		public abstract class docBal : PX.Data.BQL.BqlDecimal.Field<docBal> { }
		[PXDBDecimal]
		public virtual decimal? DocBal
		{
			get;
			set;
		}
		#endregion
		#region CuryDiscBal
		public abstract class curyDiscBal : PX.Data.BQL.BqlDecimal.Field<curyDiscBal> { }

		/// <summary>
		/// The cash discount balance of the document.
		/// Given in the <see cref="CuryID">currency of the document</see>.
		/// </summary>
		[PXDBDecimal]
		public virtual Decimal? CuryDiscBal
		{
			get;
			set;
		}
		#endregion
		#region DiscBal
		public abstract class discBal : PX.Data.BQL.BqlDecimal.Field<discBal> { }

		/// <summary>
		/// The cash discount balance of the document.
		/// Given in the <see cref="Company.BaseCuryID">base currency of the company</see>.
		/// </summary>
		[PXDBDecimal]
		public virtual Decimal? DiscBal
		{
			get;
			set;
		}
		#endregion
		#region DocDesc
		public abstract class docDesc : PX.Data.BQL.BqlString.Field<docDesc> { }
		[PXDBString(Common.Constants.TranDescLength512, IsUnicode = true)]
		public virtual string DocDesc
		{
			get;
			set;
		}
		#endregion
		#region Released
		public abstract class released : PX.Data.BQL.BqlBool.Field<released> { }
		[PXDBBool]
		public virtual bool? Released
		{
			get;
			set;
		}
		#endregion
		#region OpenDoc
		public abstract class openDoc : PX.Data.BQL.BqlBool.Field<openDoc> { }
		[PXDBBool]
		public virtual bool? OpenDoc
		{
			get;
			set;
		}
		#endregion
		#region Voided
		public abstract class voided : PX.Data.BQL.BqlBool.Field<voided> { }
		[PXDBBool]
		public virtual bool? Voided
		{
			get;
			set;
		}
		#endregion
		#region PaymentsByLinesAllowed
		public abstract class paymentsByLinesAllowed : PX.Data.BQL.BqlBool.Field<paymentsByLinesAllowed> { }

		[PXDBBool]
		public virtual bool? PaymentsByLinesAllowed
		{
			get;
			set;
		}
		#endregion
		#region Scheduled
		public abstract class scheduled : IBqlField
		{
		}

		[PXDBBool()]
		public virtual Boolean? Scheduled
		{
			get;
			set;
		}
		#endregion
		#region ScheduleID
		public abstract class scheduleID : IBqlField
		{
		}
		
		[PXDBString(15, IsUnicode = true)]
		public virtual string ScheduleID
		{
			get;
			set;
		}
		#endregion
	}
	
	[Serializable]
	public partial class BAccount : IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<BAccount>.By<bAccountID>
		{
			public static BAccount Find(PXGraph graph, int bAccountID) => FindBy(graph, bAccountID);
		}
		public class UK : PrimaryKeyOf<BAccount>.By<acctCD>
		{
			public static BAccount Find(PXGraph graph, string acctCD) => FindBy(graph, acctCD);
		}
		#endregion

		#region BAccountID
		public abstract class bAccountID : PX.Data.BQL.BqlInt.Field<bAccountID> { }
		[PXDBIdentity]
		public virtual int? BAccountID
		{
			get;
			set;
		}
		#endregion
		#region AcctName
		public abstract class acctName : PX.Data.BQL.BqlString.Field<acctName> { }

		[PXDBString(60, IsUnicode = true)]
		public virtual string AcctName
		{
			get;
			set;
		}
        #endregion
        #region ConsolidatingBAccountID
        public abstract class consolidatingBAccountID : PX.Data.BQL.BqlInt.Field<consolidatingBAccountID> { }

        [PXDBInt]
		public virtual int? ConsolidatingBAccountID
		{
			get;
			set;
		}
        #endregion
        #region AcctCD
        public abstract class acctCD : PX.Data.BQL.BqlString.Field<acctCD> { }
        [PXDimensionSelector("BIZACCT", typeof(BAccount.acctCD), typeof(BAccount.acctCD), DescriptionField = typeof(BAccount.acctName))]
        [PXDBString(30, IsUnicode = true, IsKey = true, InputMask = "")]
        public virtual string AcctCD
        {
            get;
            set;
        }
        #endregion
        #region Status
        public abstract class status : PX.Data.BQL.BqlString.Field<status> { }
        [PXDBString(1, IsFixed = true)]
        [CustomerStatus.List]
        public virtual string Status
        {
            get;
            set;
        }
        #endregion
        #region VStatus
        public abstract class vStatus : PX.Data.BQL.BqlString.Field<vStatus> { }
        [PXDBString(1, IsFixed = true)]
        [VendorStatus.List]
        public virtual string VStatus
        {
            get;
            set;
        }
        #endregion
        #region NoteID
        public abstract class noteID : PX.Data.BQL.BqlGuid.Field<noteID> { }
        [PXDBGuid]
        public virtual Guid? NoteID
        {
            get;
            set;
        }
        #endregion
    }
    
	[Serializable]
	public partial class CABankTranAdjustment : IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<CABankTranAdjustment>.By<tranID, adjNbr>
		{
			public static CABankTranAdjustment Find(PXGraph graph, int? tranID, int? adjNbr) => FindBy(graph, tranID, adjNbr);
		}
		#endregion

		#region TranID
		public abstract class tranID : PX.Data.BQL.BqlInt.Field<tranID> { }
		[PXDBInt(IsKey = true)]
		public virtual int? TranID
		{
			get;
			set;
		}
		#endregion
		#region AdjdModule
		public abstract class adjdModule : PX.Data.BQL.BqlString.Field<adjdModule> { }
		[PXDBString(2, IsFixed = true)]
		public virtual string AdjdModule
		{
			get;
			set;
		}
		#endregion
		#region AdjdDocType
		public abstract class adjdDocType : PX.Data.BQL.BqlString.Field<adjdDocType> { }
		[PXDBString(3, IsFixed = true)]
		public virtual string AdjdDocType
		{
			get;
			set;
		}
		#endregion
		#region AdjdRefNbr
		public abstract class adjdRefNbr : PX.Data.BQL.BqlString.Field<adjdRefNbr> { }
		[PXDBString(15, IsUnicode = true)]
		public virtual string AdjdRefNbr
		{
			get;
			set;
		}
		#endregion
		#region AdjNbr
		public abstract class adjNbr : PX.Data.BQL.BqlInt.Field<adjNbr> { }
		[PXDBInt(IsKey = true)]
		public virtual int? AdjNbr
		{
			get;
			set;
		}
		#endregion
		#region Released
		public abstract class released : PX.Data.BQL.BqlBool.Field<released> { }
		[PXDBBool]
		public virtual bool? Released
		{
			get;
			set;
		}
		#endregion
		#region Voided
		public abstract class voided : PX.Data.BQL.BqlBool.Field<voided> { }
		[PXDBBool]
		public virtual bool? Voided
		{
			get;
			set;
		}
		#endregion
	}

	[Serializable]
	public partial class ARAdjust : IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<ARAdjust>.By<adjgDocType, adjgRefNbr, adjNbr, adjdDocType, adjdRefNbr, adjdLineNbr>
		{
			public static ARAdjust Find(PXGraph graph, String adjgDocType, String adjgRefNbr, Int32? adjNbr, String adjdDocType, String adjdRefNbr, Int32? adjdLineNbr)
														=> FindBy(graph, adjgDocType, adjgRefNbr, adjNbr, adjdDocType, adjdRefNbr, adjdLineNbr);
		}
		#endregion

		#region AdjgDocType
		public abstract class adjgDocType : PX.Data.BQL.BqlString.Field<adjgDocType> { }
		[PXDBString(3, IsKey = true, IsFixed = true)]
		public virtual string AdjgDocType
		{
			get;
			set;
		}
		#endregion
		#region AdjgRefNbr
		public abstract class adjgRefNbr : PX.Data.BQL.BqlString.Field<adjgRefNbr> { }
		[PXDBString(15, IsUnicode = true, IsKey = true)]
		public virtual string AdjgRefNbr
		{
			get;
			set;
		}
		#endregion
		#region AdjdDocType
		public abstract class adjdDocType : PX.Data.BQL.BqlString.Field<adjdDocType> { }
		[PXDBString(3, IsKey = true, IsFixed = true)]
		public virtual string AdjdDocType
		{
			get;
			set;
		}
		#endregion
		#region AdjdRefNbr
		public abstract class adjdRefNbr : PX.Data.BQL.BqlString.Field<adjdRefNbr> { }
		[PXDBString(15, IsKey = true, IsUnicode = true)]
		public virtual string AdjdRefNbr
		{
			get;
			set;
		}
		#endregion
		#region AdjNbr
		public abstract class adjNbr : PX.Data.BQL.BqlInt.Field<adjNbr> { }
		[PXDBInt(IsKey = true)]
		public virtual int? AdjNbr
		{
			get;
			set;
		}
		#endregion
		#region AdjdLineNbr
		public abstract class adjdLineNbr : PX.Data.BQL.BqlInt.Field<adjdLineNbr> { }

		[PXDBInt(IsKey = true)]
		public virtual int? AdjdLineNbr
		{
			get;
			set;
		}
		#endregion
		#region Released
		public abstract class released : PX.Data.BQL.BqlBool.Field<released> { }
		[PXDBBool]
		public virtual bool? Released
		{
			get;
			set;
		}
		#endregion
		#region Voided
		public abstract class voided : PX.Data.BQL.BqlBool.Field<voided> { }
		[PXDBBool]
		public virtual bool? Voided
		{
			get;
			set;
		}
		#endregion
	}
	
	[PXTable]
	[Serializable]
	public partial class APInvoice : APRegister
	{
		#region Keys
		public new class PK : PrimaryKeyOf<APInvoice>.By<docType, refNbr>
		{
			public static APInvoice Find(PXGraph graph, string docType, string refNbr) => FindBy(graph, docType, refNbr);
		}
		#endregion

		#region DocType
		public new abstract class docType : PX.Data.BQL.BqlString.Field<docType> { }
		#endregion
		#region RefNbr
		public new abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr> { }
		#endregion
		#region VendorID
		public new abstract class vendorID : PX.Data.BQL.BqlInt.Field<vendorID> { }
		#endregion
		#region CuryID
		public new abstract class curyID : PX.Data.BQL.BqlString.Field<curyID> { }
		#endregion
		#region InvoiceNbr
		public abstract class invoiceNbr : PX.Data.BQL.BqlString.Field<invoiceNbr> { }
		[PXDBString(40, IsUnicode = true)]
		public virtual string InvoiceNbr
		{
			get;
			set;
		}
		#endregion
		#region DocDate
		public new abstract class docDate : PX.Data.BQL.BqlDateTime.Field<docDate> { }
		#endregion
		#region CuryInfoID
		public new abstract class curyInfoID : PX.Data.BQL.BqlLong.Field<curyInfoID> { }
		#endregion
		#region CuryDocBal
		public new abstract class curyDocBal : PX.Data.BQL.BqlDecimal.Field<curyDocBal> { }
		#endregion
		#region DocBal
		public new abstract class docBal : PX.Data.BQL.BqlDecimal.Field<docBal> { }
		#endregion
		#region CuryDiscBal
		public new abstract class curyDiscBal : PX.Data.BQL.BqlDecimal.Field<curyDiscBal> { }
		#endregion
		#region DiscBal
		public new abstract class discBal : PX.Data.BQL.BqlDecimal.Field<discBal> { }
		#endregion
		#region Released
		public new abstract class released : PX.Data.BQL.BqlBool.Field<released> { }
		#endregion
		#region OpenDoc
		public new abstract class openDoc : PX.Data.BQL.BqlBool.Field<openDoc> { }
		#endregion
		#region Voided
		public new abstract class voided : PX.Data.BQL.BqlBool.Field<voided> { }
		#endregion
		#region DocDesc
		public new abstract class docDesc : PX.Data.BQL.BqlString.Field<docDesc> { }
		#endregion
		#region PaymentsByLinesAllowed
		public new abstract class paymentsByLinesAllowed : PX.Data.BQL.BqlBool.Field<paymentsByLinesAllowed> { }
		#endregion
		#region PayAccountID
		public abstract class payAccountID : IBqlField
		{
		}
		
		[PXDBInt]
		public virtual Int32? PayAccountID
		{
			get;
			set;
		}
		#endregion		
		#region DueDate
		public abstract class dueDate : PX.Data.IBqlField
		{
		}
				
		[PXDBDate()]
		public virtual DateTime? DueDate
		{
			get;
			set;
		}
		#endregion
		#region PayDate
		public abstract class payDate : PX.Data.IBqlField
		{
		}

		[PXDBDate()]
		public virtual DateTime? PayDate
		{
			get;
			set;
		}
		#endregion
		#region DrCr
		public abstract class drCr : PX.Data.IBqlField
		{
		}

		[PXString(1, IsFixed = true)]
		public string DrCr
		{
			[PXDependsOnFields(typeof(docType))]
			get
			{
				return AP.APInvoiceType.DrCr(this.DocType);
			}
			set
			{
			}
		}
		#endregion
		#region TermsID
		public abstract class termsID : IBqlField
		{
		}

		[PXDBString(10, IsUnicode = true)]
		public virtual string TermsID
		{
			get;
			set;
		}
		#endregion
		#region PayTypeID
		public abstract class payTypeID : PX.Data.IBqlField
		{
		}

		[PXDBString(10, IsUnicode = true)]
		public virtual string PayTypeID
		{
			get;
			set;
		}
		#endregion
		#region DiscDate
		public abstract class discDate : PX.Data.BQL.BqlDateTime.Field<discDate> { }

		/// <summary>
		/// The date when the cash discount can be taken in accordance with the <see cref="APInvoice.TermsID">credit terms</see>.
		/// </summary>
		[PXDBDate()]
		public virtual DateTime? DiscDate
		{
			get;
			set;
		}
		#endregion
	}
	
	[Serializable]
	public partial class APRegister : IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<APRegister>.By<docType, refNbr>
		{
			public static APRegister Find(PXGraph graph, string docType, string refNbr) => FindBy(graph, docType, refNbr);
		}
		#endregion

		#region BranchID
		public abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID> { }
        [PXDBInt]
        public virtual int? BranchID
        {
            get;
            set;
        }
        #endregion
		#region DocType
		public abstract class docType : PX.Data.BQL.BqlString.Field<docType> { }
		[PXDBString(3, IsKey = true, IsFixed = true)]
		public virtual string DocType
		{
			get;
			set;
		}
		#endregion
		#region RefNbr
		public abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr> { }
		[PXDBString(15, IsUnicode = true, IsKey = true)]
		public virtual string RefNbr
		{
			get;
			set;
		}
		#endregion
		#region DocDate
		public abstract class docDate : PX.Data.BQL.BqlDateTime.Field<docDate> { }
		[PXDBDate]
		public virtual DateTime? DocDate
		{
			get;
			set;
		}
		#endregion
		#region FinPeriodID
		public abstract class finPeriodID : PX.Data.BQL.BqlString.Field<finPeriodID> { }
		[PXDBString]
		public virtual string FinPeriodID
		{
			get;
			set;
		}
		#endregion
		#region VendorID
		public abstract class vendorID : PX.Data.BQL.BqlInt.Field<vendorID> { }
		[PXDBInt]
		public virtual int? VendorID
		{
			get;
			set;
		}
		#endregion
		#region CuryID
		public abstract class curyID : PX.Data.BQL.BqlString.Field<curyID> { }
		[PXDBString(5, IsUnicode = true)]
		public virtual string CuryID
		{
			get;
			set;
		}
		#endregion
		#region CuryInfoID
		public abstract class curyInfoID : PX.Data.BQL.BqlLong.Field<curyInfoID> { }
		[PXDBLong]
		public virtual long? CuryInfoID
		{
			get;
			set;
		}
		#endregion
		#region CuryDocBal
		public abstract class curyDocBal : PX.Data.BQL.BqlDecimal.Field<curyDocBal> { }
		[PXDBDecimal]
		public virtual decimal? CuryDocBal
		{
			get;
			set;
		}
		#endregion
		#region DocBal
		public abstract class docBal : PX.Data.BQL.BqlDecimal.Field<docBal> { }
		[PXDBDecimal]
		public virtual decimal? DocBal
		{
			get;
			set;
		}
		#endregion
		#region CuryDiscBal
		public abstract class curyDiscBal : PX.Data.BQL.BqlDecimal.Field<curyDiscBal> { }

		/// <summary>
		/// The cash discount balance of the document.
		/// Given in the <see cref="CuryID">currency of the document</see>.
		/// </summary>
		[PXDBDecimal]
		public virtual Decimal? CuryDiscBal
		{
			get;
			set;
		}
		#endregion
		#region DiscBal
		public abstract class discBal : PX.Data.BQL.BqlDecimal.Field<discBal> { }

		/// <summary>
		/// The cash discount balance of the document.
		/// Given in the <see cref="Company.BaseCuryID">base currency of the company</see>.
		/// </summary>
		[PXDBDecimal]
		public virtual Decimal? DiscBal
		{
			get;
			set;
		}
		#endregion
		#region DocDesc
		public abstract class docDesc : PX.Data.BQL.BqlString.Field<docDesc> { }
		[PXDBString(Common.Constants.TranDescLength512, IsUnicode = true)]
		public virtual string DocDesc
		{
			get;
			set;
		}
		#endregion
		#region Released
		public abstract class released : PX.Data.BQL.BqlBool.Field<released> { }
		[PXDBBool]
		public virtual bool? Released
		{
			get;
			set;
		}
		#endregion
		#region OpenDoc
		public abstract class openDoc : PX.Data.BQL.BqlBool.Field<openDoc> { }
		[PXDBBool]
		public virtual bool? OpenDoc
		{
			get;
			set;
		}
		#endregion
		#region Voided
		public abstract class voided : PX.Data.BQL.BqlBool.Field<voided> { }
		[PXDBBool]
		public virtual bool? Voided
		{
			get;
			set;
		}
		#endregion
		#region PaymentsByLinesAllowed
		public abstract class paymentsByLinesAllowed : PX.Data.BQL.BqlBool.Field<paymentsByLinesAllowed> { }

		[PXDBBool]
		public virtual bool? PaymentsByLinesAllowed
		{
			get;
			set;
		}
		#endregion
		#region Scheduled
		public abstract class scheduled : PX.Data.IBqlField
		{
		}

		[PXDBBool()]
		public virtual Boolean? Scheduled
		{
			get;
			set;
		}
		#endregion
		#region ScheduleID
		public abstract class scheduleID : IBqlField
		{
		}

		[PXDBString(15, IsUnicode = true)]
		public virtual string ScheduleID
		{
			get;
			set;
		}
		#endregion
	}
	
	[Serializable]
	public partial class APAdjust : IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<APAdjust>.By<adjgDocType, adjgRefNbr, adjdDocType, adjdRefNbr, adjNbr, adjdLineNbr>
		{
			public static APAdjust Find(PXGraph graph, String adjgDocType, String adjgRefNbr, String adjdDocType, String adjdRefNbr, Int32? adjNbr, Int32? adjdLineNbr)
											=> FindBy(graph, adjgDocType, adjgRefNbr, adjdDocType, adjdRefNbr, adjNbr, adjdLineNbr);
		}
		#endregion

		#region AdjgDocType
		public abstract class adjgDocType : PX.Data.BQL.BqlString.Field<adjgDocType> { }
		[PXDBString(3, IsKey = true, IsFixed = true)]
		public virtual string AdjgDocType
		{
			get;
			set;
		}
		#endregion
		#region AdjgRefNbr
		public abstract class adjgRefNbr : PX.Data.BQL.BqlString.Field<adjgRefNbr> { }
		[PXDBString(15, IsUnicode = true, IsKey = true)]
		public virtual string AdjgRefNbr
		{
			get;
			set;
		}
		#endregion
		#region AdjdDocType
		public abstract class adjdDocType : PX.Data.BQL.BqlString.Field<adjdDocType> { }
		[PXDBString(3, IsKey = true, IsFixed = true)]
		public virtual string AdjdDocType
		{
			get;
			set;
		}
		#endregion
		#region AdjdRefNbr
		public abstract class adjdRefNbr : PX.Data.BQL.BqlString.Field<adjdRefNbr> { }
		[PXDBString(15, IsKey = true, IsUnicode = true)]
		public virtual string AdjdRefNbr
		{
			get;
			set;
		}
		#endregion
		#region AdjNbr
		public abstract class adjNbr : PX.Data.BQL.BqlInt.Field<adjNbr> { }
		[PXDBInt(IsKey = true)]
		public virtual int? AdjNbr
		{
			get;
			set;
		}
		#endregion
		#region AdjdLineNbr
		public abstract class adjdLineNbr : PX.Data.BQL.BqlInt.Field<adjdLineNbr> { }

		[PXDBInt(IsKey = true)]
		public virtual int? AdjdLineNbr
		{
			get;
			set;
		}
		#endregion
		#region Released
		public abstract class released : PX.Data.BQL.BqlBool.Field<released> { }
		[PXDBBool]
		public virtual bool? Released
		{
			get;
			set;
		}
		#endregion
		#region Voided
		public abstract class voided : PX.Data.BQL.BqlBool.Field<voided> { }

		[PXDBBool]
		public virtual bool? Voided
		{
			get;
			set;
		}
		#endregion
	}
    
	[Serializable]
    [PXTable(typeof(CA.Light.BAccount.bAccountID))]
    [PXCacheName(AR.Messages.Customer)]
    public class Customer : CA.Light.BAccount
    {
		#region Keys
		public new class PK : PrimaryKeyOf<Customer>.By<bAccountID>
		{
			public static Customer Find(PXGraph graph, int? bAccountID) => FindBy(graph, bAccountID);
		}
		public new class UK : PrimaryKeyOf<Customer>.By<acctCD>
		{
			public static Customer Find(PXGraph graph, string acctCD) => FindBy(graph, acctCD);
		}
		#endregion

		#region BAccountID
		public new abstract class bAccountID : PX.Data.BQL.BqlInt.Field<bAccountID> { }
        #endregion
		#region AcctCD
		public new abstract class acctCD : PX.Data.BQL.BqlString.Field<acctCD> { }
		#endregion
		#region AcctName
		public new abstract class acctName : PX.Data.BQL.BqlString.Field<acctName> { }
		#endregion
        #region CuryID
        public abstract class curyID : PX.Data.BQL.BqlString.Field<curyID> { }
        [PXDBString(5, IsUnicode = true)]
        public virtual string CuryID
        {
            get;
            set;
        }
        #endregion
        #region CustomerClassID
        public abstract class customerClassID : PX.Data.BQL.BqlString.Field<customerClassID> { }
        [PXDBString(10, IsUnicode = true)]
        public virtual string CustomerClassID
        {
            get;
            set;
        }
        #endregion
        #region StatementCycleId
        public abstract class statementCycleId : PX.Data.BQL.BqlString.Field<statementCycleId> { }
        [PXDBString(10, IsUnicode = true)]
        public virtual string StatementCycleId
        {
            get;
            set;
        }
        #endregion
        #region ConsolidatingBAccountID
        public new abstract class consolidatingBAccountID : PX.Data.BQL.BqlInt.Field<consolidatingBAccountID> { }
        #endregion
		#region NoteID
		public new abstract class noteID : PX.Data.BQL.BqlString.Field<noteID> { }
		#endregion
    }

    [Serializable]
    public class CustomerMaster : Customer
    {
		#region Keys
		public new class PK : PrimaryKeyOf<CustomerMaster>.By<bAccountID>
		{
			public static CustomerMaster Find(PXGraph graph, int? bAccountID) => FindBy(graph, bAccountID);
		}
		public new class UK : PrimaryKeyOf<CustomerMaster>.By<acctCD>
		{
			public static CustomerMaster Find(PXGraph graph, string acctCD) => FindBy(graph, acctCD);
		}
		#endregion

		#region BAccountID
		public new abstract class bAccountID : PX.Data.BQL.BqlInt.Field<bAccountID> { }
        [AR.Customer(IsKey = true, DisplayName = "Customer ID")]
        public override int? BAccountID
        {
            get;
            set;
        }
        #endregion
        #region AcctCD
        public new abstract class acctCD : PX.Data.BQL.BqlString.Field<acctCD> { }
        [PXDBString(30, IsUnicode = true)]
        public override string AcctCD
        {
            get;
            set;
        }
        #endregion
        #region StatementCycleID
        public new abstract class statementCycleId : PX.Data.BQL.BqlString.Field<statementCycleId> { }
        #endregion
    }
    
	[Serializable]
    [PXTable(typeof(CA.Light.BAccount.bAccountID))]
    [PXCacheName(AR.Messages.Customer)]
    public class Vendor : CA.Light.BAccount
    {
		#region Keys
		public new class PK : PrimaryKeyOf<Vendor>.By<bAccountID>
		{
			public static Vendor Find(PXGraph graph, int? bAccountID) => FindBy(graph, bAccountID);
		}
		public new class UK : PrimaryKeyOf<Vendor>.By<acctCD>
		{
			public static Vendor Find(PXGraph graph, string acctCD) => FindBy(graph, acctCD);
		}
		#endregion
		#region BAccountID
		public new abstract class bAccountID : PX.Data.BQL.BqlInt.Field<bAccountID> { }
		#endregion
		#region AcctCD
		public new abstract class acctCD : PX.Data.BQL.BqlString.Field<acctCD> { }
		#endregion
		#region AcctName
		public new abstract class acctName : PX.Data.BQL.BqlString.Field<acctName> { }
		#endregion
		#region CuryID
		public abstract class curyID : PX.Data.BQL.BqlString.Field<curyID> { }
		[PXDBString(5, IsUnicode = true)]
		public virtual string CuryID
		{
			get;
			set;
		}
		#endregion
		#region VendorClassID
		public abstract class vendorClassID : PX.Data.BQL.BqlString.Field<vendorClassID> { }
	    [PXDBString(10, IsUnicode = true)]
	    public virtual string VendorClassID
	    {
		    get;
		    set;
	    }
	    #endregion
    }
}
