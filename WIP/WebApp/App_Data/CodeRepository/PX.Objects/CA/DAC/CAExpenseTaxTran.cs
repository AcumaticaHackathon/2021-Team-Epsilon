using System;
using PX.Data;
using PX.Data.BQL;
using PX.Objects.CM.Extensions;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.TX;

namespace PX.Objects.CA
{
	[Serializable]
	[PXCacheName(nameof(CAExpenseTaxTran))]
	public class CAExpenseTaxTran : TaxTran
	{
		#region Module
		public new abstract class module : PX.Data.BQL.BqlString.Field<module> { }
		[PXDBString(2, IsKey = true, IsFixed = true)]
		[PXDefault(BatchModule.CA)]
		[PXUIField(DisplayName = "Module", Enabled = false, Visible = false)]
		public override string Module
		{
			get;
			set;
		}
		#endregion
		#region TranType
		public abstract new class tranType : BqlString.Field<tranType> { }

		[PXDBString(3, IsKey = true, IsFixed = true)]
		[PXDefault(typeof(CATranType.cATransferExp))]
		[PXUIField(DisplayName = "Tran. Type", Enabled = false, Visible = false)]
		[PXParent(typeof(Select<CAExpense, Where<CAExpense.refNbr, Equal<Current<TaxTran.refNbr>>, And<CAExpense.lineNbr, Equal<Current<TaxTran.lineNbr>>>>>))]
		public override string TranType 
		{ 
			get; 
			set; 
		}
		#endregion
		#region LineNbr
		public abstract new class lineNbr : PX.Data.BQL.BqlInt.Field<lineNbr> { }

		[PXDBInt(IsKey = true)]
		[PXUIField(DisplayName = "Line Nbr.", Visibility = PXUIVisibility.Visible, Visible = false)]
		[PXDBDefault(typeof(CAExpense.lineNbr))]
		public override int? LineNbr 
		{ 
			get; 
			set; 
		}
		#endregion
		#region RefNbr
		public abstract new class refNbr : BqlString.Field<refNbr> { }

		[PXDBString(15, IsKey = true, IsUnicode = true)]
		[PXDBDefault(typeof(CAExpense.refNbr))]
		[PXUIField(DisplayName = "Ref. Nbr.", Enabled = false, Visible = false)]
		public override string RefNbr 
		{ 
			get; 
			set; 
		}
		#endregion
		#region BranchID
		public new abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID> { }
		[Branch(typeof(Search<CashAccount.branchID, Where<CashAccount.cashAccountID, Equal<Current<CAExpense.cashAccountID>>>>), Enabled = false)]
		public override int? BranchID
		{
			get;
			set;
		}
		#endregion
		#region Released
		public new abstract class released : PX.Data.BQL.BqlBool.Field<released> { }
		#endregion
		#region Voided
		public new abstract class voided : PX.Data.BQL.BqlBool.Field<voided> { }
		#endregion
		#region TaxPeriodID
		public new abstract class taxPeriodID : PX.Data.BQL.BqlString.Field<taxPeriodID> { }
		[FinPeriodID]
		public override string TaxPeriodID
		{
			get;
			set;
		}
		#endregion
		#region FinPeriodID
		public new abstract class finPeriodID : PX.Data.BQL.BqlString.Field<finPeriodID> { }
		[FinPeriodID(branchSourceType: typeof(branchID),
			headerMasterFinPeriodIDType: typeof(CAExpense.finPeriodID))]
		[PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
		public override string FinPeriodID
		{
			get;
			set;
		}
		#endregion
		#region TaxID
		public new abstract class taxID : PX.Data.BQL.BqlString.Field<taxID> { }
		[PXDBString(Tax.taxID.Length, IsUnicode = true, IsKey = true)]
		[PXDefault]
		[PXUIField(DisplayName = "Tax ID", Visibility = PXUIVisibility.Visible)]
		[PXSelector(typeof(Tax.taxID), DescriptionField = typeof(Tax.descr), DirtyRead = true)]
		public override string TaxID
		{
			get;
			set;
		}
		#endregion
		#region RecordID
		public new abstract class recordID : PX.Data.BQL.BqlInt.Field<recordID>
		{
		}

		/// <summary>
		/// This is an auto-numbered field, which is a part of the primary key.
		/// </summary>
		[PXDBIdentity(IsKey = true)]
		public override int? RecordID 
		{ 
			get; 
			set; 
		}
		#endregion
		#region VendorID
		public new abstract class vendorID : PX.Data.BQL.BqlInt.Field<vendorID> { }
		[PXDBInt]
		[PXDefault(typeof(Search<Tax.taxVendorID, Where<Tax.taxID, Equal<Current<taxID>>>>), PersistingCheck = PXPersistingCheck.Nothing)]
		public override int? VendorID
		{
			get;
			set;
		}
		#endregion
		#region TaxZoneID
		public new abstract class taxZoneID : PX.Data.BQL.BqlString.Field<taxZoneID> { }
		[PXDBString(10, IsUnicode = true)]
		[PXDBDefault(typeof(CAExpense.taxZoneID))]
		public override string TaxZoneID
		{
			get;
			set;
		}
		#endregion
		#region AccountID
		public new abstract class accountID : PX.Data.BQL.BqlInt.Field<accountID> { }
		[Account]
		[PXDefault]
		public override int? AccountID
		{
			get;
			set;
		}
		#endregion
		#region SubID
		public new abstract class subID : PX.Data.BQL.BqlInt.Field<subID> { }
		[SubAccount]
		[PXDefault]
		public override int? SubID
		{
			get;
			set;
		}
		#endregion
		#region TranDate
		public new abstract class tranDate : PX.Data.BQL.BqlDateTime.Field<tranDate> { }
		[PXDBDate]
		[PXDBDefault(typeof(CAExpense.tranDate))]
		public override DateTime? TranDate
		{
			get;
			set;
		}
		#endregion
		#region TaxType
		public new abstract class taxType : PX.Data.BQL.BqlString.Field<taxType> { }
		[PXDBString(1, IsFixed = true)]
		[PXDefault]
		public override string TaxType
		{
			get;
			set;
		}
		#endregion
		#region TaxBucketID
		public new abstract class taxBucketID : PX.Data.BQL.BqlInt.Field<taxBucketID> { }
		[PXDBInt]
		[PXDefault(typeof(Search<TaxRev.taxBucketID, Where<TaxRev.taxID, Equal<Current<taxID>>, And<Current<tranDate>, Between<TaxRev.startDate, TaxRev.endDate>, And2<Where<TaxRev.taxType, Equal<Current<taxType>>, Or<TaxRev.taxType, Equal<TaxType.sales>, And<Current<taxType>, Equal<TaxType.pendingSales>, Or<TaxRev.taxType, Equal<TaxType.purchase>, And<Current<taxType>, Equal<TaxType.pendingPurchase>>>>>>, And<TaxRev.outdated, Equal<boolFalse>>>>>>))]
		public override int? TaxBucketID
		{
			get;
			set;
		}
		#endregion
		#region CuryInfoID
		public new abstract class curyInfoID : PX.Data.BQL.BqlLong.Field<curyInfoID> { }
		[PXDBLong]
		[CurrencyInfo(typeof(CAExpense.curyInfoID))]
		public override long? CuryInfoID
		{
			get;
			set;
		}
		#endregion
		#region CuryTaxableAmt
		public new abstract class curyTaxableAmt : PX.Data.BQL.BqlDecimal.Field<curyTaxableAmt> { }
		[PXDBCurrency(typeof(curyInfoID), typeof(taxableAmt))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Taxable Amount", Visibility = PXUIVisibility.Visible)]
		[PXUnboundFormula(typeof(Switch<Case<WhereExempt<taxID>, curyTaxableAmt>, decimal0>), typeof(SumCalc<CAExpense.curyVatExemptTotal>))]
		[PXUnboundFormula(typeof(Switch<Case<WhereTaxable<taxID>, curyTaxableAmt>, decimal0>), typeof(SumCalc<CAExpense.curyVatTaxableTotal>))]
		public override decimal? CuryTaxableAmt
		{
			get;
			set;
		}
		#endregion
		#region TaxableAmt
		public new abstract class taxableAmt : PX.Data.BQL.BqlDecimal.Field<taxableAmt> { }

		[PXDBBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Taxable Amount", Visibility = PXUIVisibility.Visible)]
		public override decimal? TaxableAmt
		{
			get;
			set;
		}
		#endregion
		#region CuryExemptedAmt
		public new abstract class curyExemptedAmt : IBqlField { }

		/// <summary>
		/// The exempted amount in the record currency.
		/// </summary>
		[PXDBCurrency(typeof(curyInfoID), typeof(exemptedAmt))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Exempted Amount", Visibility = PXUIVisibility.Visible, FieldClass = nameof(FeaturesSet.ExemptedTaxReporting))]
		public override decimal? CuryExemptedAmt
		{
			get;
			set;
		}
		#endregion
		#region ExemptedAmt
		public new abstract class exemptedAmt : IBqlField { }

		/// <summary>
		/// The exempted amount in the base currency.
		/// </summary>
		[PXDBDecimal(4)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Exempted Amount", Visibility = PXUIVisibility.Visible, FieldClass = nameof(FeaturesSet.ExemptedTaxReporting))]
		public override decimal? ExemptedAmt
		{
			get;
			set;
		}
		#endregion
		#region CuryTaxAmt
		public new abstract class curyTaxAmt : PX.Data.BQL.BqlDecimal.Field<curyTaxAmt> { }
		[PXDBCurrency(typeof(curyInfoID), typeof(taxAmt))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Tax Amount", Visibility = PXUIVisibility.Visible)]
		public override decimal? CuryTaxAmt
		{
			get;
			set;
		}
		#endregion
		#region TaxAmt
		public new abstract class taxAmt : PX.Data.BQL.BqlDecimal.Field<taxAmt> { }

		[PXDBBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Tax Amount", Visibility = PXUIVisibility.Visible)]
		public override decimal? TaxAmt
		{
			get;
			set;
		}
		#endregion

		#region NonDeductibleTaxRate
		public new abstract class nonDeductibleTaxRate : PX.Data.BQL.BqlDecimal.Field<nonDeductibleTaxRate> { }
		#endregion
		#region ExpenseAmt
		public new abstract class expenseAmt : PX.Data.BQL.BqlDecimal.Field<expenseAmt> { }
		#endregion
		#region CuryExpenseAmt
		public new abstract class curyExpenseAmt : PX.Data.BQL.BqlDecimal.Field<curyExpenseAmt> { }
		[PXDBCurrency(typeof(curyInfoID), typeof(expenseAmt))]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Expense Amount", Visibility = PXUIVisibility.Visible)]
		public override decimal? CuryExpenseAmt
		{
			get;
			set;
		}
		#endregion
	}
}
