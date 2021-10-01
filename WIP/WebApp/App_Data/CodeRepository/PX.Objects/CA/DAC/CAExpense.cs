using System;
using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.GL;
using PX.Objects.CM.Extensions;
using PX.Objects.TX;
using PX.Objects.CS;

namespace PX.Objects.CA
{
	[Serializable]
	[PXCacheName(nameof(CAExpense))]
	public class CAExpense : AP.IPaymentCharge, IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<CAExpense>.By<refNbr, lineNbr>
		{
			public static CAExpense Find(PXGraph graph, string refNbr, int? lineNbr) => FindBy(graph, refNbr, lineNbr);
		}

		public static class FK
		{
			public class CashTransfer : CA.CATransfer.PK.ForeignKeyOf<CAExpense>.By<refNbr> { }
			public class CashAccount : CA.CashAccount.PK.ForeignKeyOf<CAExpense>.By<cashAccountID> { }
			public class Branch : GL.Branch.PK.ForeignKeyOf<CAExpense>.By<branchID> { }
			public class CurrencyInfo : CM.CurrencyInfo.PK.ForeignKeyOf<CAExpense>.By<curyInfoID> { }
			public class Currency : CM.Currency.PK.ForeignKeyOf<CAExpense>.By<curyID> { }
			public class EntryType : CA.CAEntryType.PK.ForeignKeyOf<CAExpense>.By<entryTypeID> { }
			public class Account : GL.Account.PK.ForeignKeyOf<CAExpense>.By<accountID> { }
			public class Subaccount : GL.Sub.PK.ForeignKeyOf<CAExpense>.By<subID> { }
			public class CashAccountTran : CA.CATran.PK.ForeignKeyOf<CAExpense>.By<cashAccountID,cashTranID> { }
			//public class Batch : GL.Batch.PK.ForeignKeyOf<CAExpense>.By<BatchModule.moduleCA, batchNbr> { } // TODO: add FK
		}

		#endregion

		#region DocType
		public string DocType
		{
			get => CATranType.CATransferExp;
			set { }
		}
		#endregion
		#region RefNbr
		public abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr> { }

		[PXDBString(15, IsUnicode = true, IsKey = true)]
		[PXDBDefault(typeof(CATransfer.transferNbr))]
		[PXParent(typeof(Select<CATransfer, Where<CATransfer.transferNbr, Equal<Current<refNbr>>>>))]
		public string RefNbr
		{
			get;
			set;
		}
		#endregion
		#region LineNbr
		public abstract class lineNbr : PX.Data.BQL.BqlInt.Field<lineNbr> { }

		[PXDBInt(IsKey = true)]
		[PXLineNbr(typeof(CATransfer.expenseCntr), DecrementOnDelete = false)]
		[PXUIField(DisplayName = "Line Nbr.", Visibility = PXUIVisibility.Visible, Visible = false)]
		public int? LineNbr
		{
			get;
			set;
		}
		#endregion
		#region CashAccountID
		public abstract class cashAccountID : PX.Data.BQL.BqlInt.Field<cashAccountID> { }

		[CashAccount(typeof(CATransfer.outBranchID), null, DisplayName = "Cash Account", Visibility = PXUIVisibility.SelectorVisible, DescriptionField = typeof(CashAccount.descr), Required = true)]
		[PXDefault]
		public int? CashAccountID
		{
			get;
			set;
		}
		#endregion
		#region BranchID
		public abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID> { }

		[PXDBInt]
		[PXDefault(typeof(Search<CashAccount.branchID, Where<CashAccount.cashAccountID, Equal<Current<CAExpense.cashAccountID>>>>), PersistingCheck = PXPersistingCheck.Nothing)]
		[PXFormula(typeof(Default<cashAccountID>))]
		public virtual int? BranchID
		{
			get;
			set;
		}
		#endregion
		#region AdjRefNbr
		[Obsolete(PX.Objects.Common.InternalMessages.ObsoleteToBeRemovedIn2019r2)]
		public abstract class adjRefNbr : PX.Data.BQL.BqlString.Field<adjRefNbr> { }

		[Obsolete(PX.Objects.Common.InternalMessages.ObsoleteToBeRemovedIn2019r2)]
		[PXDBString(15, IsUnicode = true, InputMask = ">CCCCCCCCCCCCCCC")]
		[PXUIField(DisplayName = "Ref. Nbr.", Enabled = false, Visible = false)]
		public string AdjRefNbr
		{
			get;
			set;
		}
		#endregion
		#region Hold
		[Obsolete(PX.Objects.Common.InternalMessages.ObsoleteToBeRemovedIn2019r2)]
		public abstract class hold : PX.Data.BQL.BqlBool.Field<hold> { }

		[Obsolete(PX.Objects.Common.InternalMessages.ObsoleteToBeRemovedIn2019r2)]
		[PXBool]
		[PXUIField(Enabled = false, Visible = false)]
		public bool? Hold
		{
			get;
			set;
		}
		#endregion
		#region TranDate
		public abstract class tranDate : PX.Data.BQL.BqlDateTime.Field<tranDate> { }

		[PXDBDate]
		[PXDefault(typeof(CATransfer.outDate))]
		[PXUIField(DisplayName = "Doc. Date")]
		public virtual DateTime? TranDate
		{
			get;
			set;
		}
		#endregion
		#region TranPeriodID
		public abstract class tranPeriodID : PX.Data.BQL.BqlString.Field<tranPeriodID> { }

		[PeriodID]
		public virtual string TranPeriodID
		{
			get;
			set;
		}
		#endregion
		#region FinPeriodID
		public abstract class finPeriodID : PX.Data.BQL.BqlString.Field<finPeriodID> { }

		[CAOpenPeriod(typeof(tranDate),
					branchSourceType: typeof(cashAccountID),
					branchSourceFormulaType: typeof(Selector<cashAccountID, CashAccount.branchID>),
					masterFinPeriodIDType: typeof(tranPeriodID),
			ValidatePeriod = PeriodValidation.DefaultSelectUpdate)]
		[PXUIField(DisplayName = "Fin. Period")]
		public virtual string FinPeriodID
		{
			get;
			set;
		}
		#endregion
		#region CuryInfoID
		public abstract class curyInfoID : PX.Data.BQL.BqlLong.Field<curyInfoID> { }

		[PXDBLong]
		[CurrencyInfo]
		public long? CuryInfoID
		{
			get;
			set;
		}
		#endregion
		#region CuryID
		public abstract class curyID : PX.Data.BQL.BqlString.Field<curyID> { }

		[PXDBString(5, IsUnicode = true, InputMask = ">LLLLL")]
		[PXUIField(DisplayName = "Currency", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
		[PXSelector(typeof(Currency.curyID))]
		[PXDefault(typeof(Search<CashAccount.curyID, Where<CashAccount.cashAccountID, Equal<Current<cashAccountID>>>>), PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual string CuryID
		{
			get;
			set;
		}
		#endregion
		#region AdjCuryRate
		public abstract class adjCuryRate : PX.Data.BQL.BqlDecimal.Field<adjCuryRate> { }

		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXDecimal(8)]
		[PXUIField(DisplayName = "Currency Rate", Visibility = PXUIVisibility.Visible)]
		public virtual decimal? AdjCuryRate
		{
			get;
			set;
		}
		#endregion
		#region EntryTypeID
		public abstract class entryTypeID : PX.Data.BQL.BqlString.Field<entryTypeID> { }

		[PXDBString(10, IsUnicode = true)]
		[PXSelector(typeof(Search2<CAEntryType.entryTypeId,
			InnerJoin<CashAccountETDetail, On<CashAccountETDetail.entryTypeID, Equal<CAEntryType.entryTypeId>>>,
			Where<CashAccountETDetail.accountID, Equal<Current<cashAccountID>>,
				And<CAEntryType.module, Equal<BatchModule.moduleCA>>>>),
			DescriptionField = typeof(CAEntryType.descr), DirtyRead = false)]
		[PXDefault(typeof(Search2<CAEntryType.entryTypeId,
			InnerJoin<CashAccountETDetail, On<CashAccountETDetail.entryTypeID, Equal<CAEntryType.entryTypeId>>>,
			Where<CashAccountETDetail.accountID, Equal<Current<cashAccountID>>,
				And<CAEntryType.module, Equal<BatchModule.moduleCA>,
				And<CashAccountETDetail.isDefault, Equal<True>>>>>))]
		[PXFormula(typeof(Default<cashAccountID>))]
		[PXUIField(DisplayName = "Entry Type")]
		public virtual string EntryTypeID
		{
			get;
			set;
		}
		#endregion
		#region AccountID
		public abstract class accountID : PX.Data.BQL.BqlInt.Field<accountID> { }

		[PXDefault(typeof(Coalesce<Search<CashAccountETDetail.offsetAccountID, Where<CashAccountETDetail.entryTypeID, Equal<Current<entryTypeID>>,
							And<CashAccountETDetail.accountID, Equal<Current<cashAccountID>>>>>,
						Search<CAEntryType.accountID, Where<CAEntryType.entryTypeId, Equal<Current<entryTypeID>>>>>))]
		[Account(typeof(branchID), typeof(Search2<Account.accountID, LeftJoin<CashAccount, On<CashAccount.accountID, Equal<Account.accountID>>,
													InnerJoin<CAEntryType, On<CAEntryType.entryTypeId, Equal<Current<entryTypeID>>>>>,
												Where2<Where<CAEntryType.useToReclassifyPayments, Equal<False>,
												And<Where<Account.curyID, IsNull, Or<Account.curyID, Equal<Current<curyID>>>>>>,
													Or<Where<CashAccount.cashAccountID, IsNotNull,
														And<CashAccount.curyID, Equal<Current<curyID>>,
														And<CashAccount.cashAccountID, NotEqual<Current<cashAccountID>>>>>>>>),
			DisplayName = "Offset Account",
			DescriptionField = typeof(Account.description),
			AvoidControlAccounts = true)]
		[PXFormula(typeof(Default<cashAccountID, entryTypeID>))]
		public virtual int? AccountID
		{
			get;
			set;
		}
		#endregion
		#region SubID
		public abstract class subID : PX.Data.BQL.BqlInt.Field<subID> { }

		[PXDefault(typeof(Coalesce<Search<CashAccountETDetail.offsetSubID, Where<CashAccountETDetail.entryTypeID, Equal<Current<entryTypeID>>,
									And<CashAccountETDetail.accountID, Equal<Current<cashAccountID>>>>>,
								Search<CAEntryType.subID, Where<CAEntryType.entryTypeId, Equal<Current<entryTypeID>>>>>))]
		[SubAccount(typeof(accountID), DisplayName = "Offset Subaccount", Required = true)]
		[PXFormula(typeof(Default<cashAccountID, entryTypeID>))]
		public virtual int? SubID
		{
			get;
			set;
		}
		#endregion
		#region DrCr
		public abstract class drCr : PX.Data.BQL.BqlString.Field<drCr> { }

		[PXDBString(1, IsFixed = true)]
		[PXDefault(typeof(Search<CAEntryType.drCr, Where<CAEntryType.entryTypeId, Equal<Current<entryTypeID>>>>))]
		[CADrCr.List]
		[PXUIField(DisplayName = "Disb./Receipt", Visibility = PXUIVisibility.SelectorVisible)]
		[PXFormula(typeof(Default<entryTypeID>))]
		public string DrCr
		{
			get;
			set;
		}
		#endregion

		#region CashTranID
		public abstract class cashTranID : PX.Data.BQL.BqlLong.Field<cashTranID> { }

		[PXDBLong]
		[ExpenseCashTranID]
		public long? CashTranID
		{
			get;
			set;
		}
		#endregion
		#region CuryTaxableAmt
		public abstract class curyTaxableAmt : PX.Data.BQL.BqlDecimal.Field<curyTaxableAmt> { }

		[PXDBCurrency(typeof(curyInfoID), typeof(taxableAmt))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Amount", Enabled = true, Visible = true)]
		public decimal? CuryTaxableAmt
		{ 
			get; 
			set; 
		}
	#endregion
	#region TaxableAmt
	public abstract class taxableAmt : PX.Data.BQL.BqlDecimal.Field<taxableAmt> { }

		[PXDBBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Amount", Enabled = false, Visible = true)]
		public virtual decimal? TaxableAmt 
		{ 
			get; 
			set; 
		}
		#endregion
		#region CuryTranAmt
		public abstract class curyTranAmt : PX.Data.BQL.BqlDecimal.Field<curyTranAmt> { }

		[PXDependsOnFields(typeof(curyTaxableAmt), typeof(curyTaxAmt))]
		[PXDBCurrency(typeof(curyInfoID), typeof(tranAmt))]
		[PXDefault(typeof(curyTaxableAmt))]
		[PXUIField(DisplayName = "Total Amount", Enabled = false, Visible = true)]
		public decimal? CuryTranAmt 
		{ 
			get; 
			set; 
		}
		#endregion
		#region TranAmt
		public abstract class tranAmt : PX.Data.BQL.BqlDecimal.Field<tranAmt> { }

		[PXDBBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Total Amount", Enabled = false)]
		public virtual decimal? TranAmt 
		{ 
			get; 
			set; 
		}
		#endregion
		
		#region Released
		public abstract class released : PX.Data.BQL.BqlBool.Field<released> { }

		[PXDBBool]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Released")]
		public bool? Released 
		{ 
			get; 
			set; 
		}
		#endregion
		#region Cleared
		public abstract class cleared : PX.Data.BQL.BqlBool.Field<cleared> { }

		[PXDBBool]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Cleared")]
		public virtual bool? Cleared 
		{ 
			get; 
			set; 
		}
		#endregion
		#region ClearDate
		public abstract class clearDate : PX.Data.BQL.BqlDateTime.Field<clearDate> { }

		[PXDBDate]
		[PXUIField(DisplayName = "Clear Date")]
		public virtual DateTime? ClearDate 
		{ 
			get; 
			set; 
		}
		#endregion
		#region ExtRefNbr
		public abstract class extRefNbr : PX.Data.BQL.BqlString.Field<extRefNbr> { }

		[PXDBString(40, IsUnicode = true)]
		[PXUIField(DisplayName = "Document Ref.")]
		public virtual string ExtRefNbr 
		{ 
			get; 
			set; 
		}
		#endregion
		#region TranDesc
		public abstract class tranDesc : PX.Data.BQL.BqlString.Field<tranDesc> { }

		[PXDBString(Common.Constants.TranDescLength, IsUnicode = true)]
		[PXUIField(DisplayName = "Description")]
		[PXDefault(typeof(Search<CAEntryType.descr, Where<CAEntryType.entryTypeId, Equal<Current<CAExpense.entryTypeID>>>>), PersistingCheck = PXPersistingCheck.Nothing)]
		[PXFormula(typeof(Default<entryTypeID>))]
		public virtual string TranDesc
		{
			get;
			set;
		}
		#endregion
		#region BatchNbr
		public abstract class batchNbr : PX.Data.BQL.BqlString.Field<batchNbr> { }

		[PXString(15, IsUnicode = true)]
		[PXUIField(DisplayName = "Batch Number", Visibility = PXUIVisibility.SelectorVisible, Visible = false, Enabled = false)]
		[PXDBScalar(typeof(Search<CATran.batchNbr, Where<CATran.tranID, Equal<cashTranID>>>))]
		public virtual string BatchNbr
		{
			get;
			set;
		}
		#endregion
		#region TaxZoneID
		public abstract class taxZoneID : PX.Data.BQL.BqlString.Field<taxZoneID> { }

		/// <summary>
		/// The tax zone that applies to the transaction.
		/// </summary>
		/// <value>
		/// Corresponds to the value of the <see cref="TaxZone.TaxZoneID"/> field.
		/// </value>
		[PXDBString(10, IsUnicode = true)]
		[PXUIField(DisplayName = "Tax Zone", Visibility = PXUIVisibility.Visible)]
		[PXSelector(typeof(TaxZone.taxZoneID), DescriptionField = typeof(TaxZone.descr), Filterable = true)]
		[PXDefault(typeof(Search<CashAccountETDetail.taxZoneID, Where<CashAccountETDetail.accountID, Equal<Current<CAExpense.cashAccountID>>, And<CashAccountETDetail.entryTypeID, Equal<Current<CAExpense.entryTypeID>>>>>), PersistingCheck = PXPersistingCheck.Nothing)]
		[PXFormula(typeof(Default<cashAccountID, entryTypeID>))]
		public virtual string TaxZoneID
		{
			get;
			set;
		}
		#endregion
		#region TaxCategoryID
		public abstract class taxCategoryID : PX.Data.BQL.BqlString.Field<taxCategoryID> { }

		[PXDBString(10, IsUnicode = true)]
		[PXUIField(DisplayName = "Tax Category", Visibility = PXUIVisibility.Visible)]
		[PXSelector(typeof(TaxCategory.taxCategoryID), DescriptionField = typeof(TaxCategory.descr))]
		[PXDefault(typeof(Search<TaxZone.dfltTaxCategoryID, Where<TaxZone.taxZoneID, Equal<Current<taxZoneID>>>>), PersistingCheck = PXPersistingCheck.Nothing)]
		[PXFormula(typeof(Default<cashAccountID, entryTypeID, taxZoneID>))]
		[PXRestrictor(typeof(Where<TaxCategory.active, Equal<True>>), TX.Messages.InactiveTaxCategory, typeof(TaxCategory.taxCategoryID))]
		public virtual string TaxCategoryID
		{
			get;
			set;
		}
		#endregion
		#region CuryTaxTotal
		public abstract class curyTaxTotal : PX.Data.BQL.BqlDecimal.Field<curyTaxTotal> { }

		/// <summary>
		/// The total amount of tax paid on the document in the selected currency.
		/// </summary>
		[PXDBCurrency(typeof(CAExpense.curyInfoID), typeof(CAExpense.taxTotal))]
		[PXUIField(DisplayName = "Tax Total", Visibility = PXUIVisibility.Visible, Enabled = false)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual decimal? CuryTaxTotal
		{
			get;
			set;
		}
		#endregion
		#region TaxTotal
		public abstract class taxTotal : PX.Data.BQL.BqlDecimal.Field<taxTotal> { }

		/// <summary>
		/// The total amount of tax paid on the document in the base currency.
		/// </summary>
		[PXDBDecimal(4)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual decimal? TaxTotal
		{
			get;
			set;
		}
		#endregion
		#region CuryVatExemptTotal
		public abstract class curyVatExemptTotal : PX.Data.BQL.BqlDecimal.Field<curyVatExemptTotal> { }

		/// <summary>
		/// The document total that is exempt from VAT in the selected currency. 
		/// This total is calculated as the taxable amount for the tax 
		/// with the <see cref="Tax.ExemptTax"/> field set to <c>true</c> (that is, the Include in VAT Exempt Total check box selected on the Taxes (TX205000) form).
		/// </summary>
		[PXDBCurrency(typeof(CAExpense.curyInfoID), typeof(CAExpense.vatExemptTotal))]
		[PXUIField(DisplayName = "VAT Exempt Total", Visibility = PXUIVisibility.Visible, Enabled = false)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual decimal? CuryVatExemptTotal
		{
			get;
			set;
		}
		#endregion
		#region VatExemptTaxTotal
		public abstract class vatExemptTotal : PX.Data.BQL.BqlDecimal.Field<vatExemptTotal> { }

		/// <summary>
		/// The document total that is exempt from VAT in the base currency. 
		/// </summary>
		[PXDBDecimal(4)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual decimal? VatExemptTotal
		{
			get;
			set;
		}
		#endregion
		#region CuryVatTaxableTotal
		public abstract class curyVatTaxableTotal : PX.Data.BQL.BqlDecimal.Field<curyVatTaxableTotal> { }

		/// <summary>
		/// The document total that is subjected to VAT in the selected currency.
		/// The field is displayed only if 
		/// the <see cref="Tax.IncludeInTaxable"/> field is set to <c>true</c> (that is, the Include in VAT Exempt Total check box is selected on the Taxes (TX205000) form).
		/// </summary>
		[PXDBCurrency(typeof(curyInfoID), typeof(vatTaxableTotal))]
		[PXUIField(DisplayName = "VAT Taxable Total", Visibility = PXUIVisibility.Visible, Enabled = false)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual decimal? CuryVatTaxableTotal
		{
			get;
			set;
		}
		#endregion
		#region VatTaxableTotal
		public abstract class vatTaxableTotal : PX.Data.BQL.BqlDecimal.Field<vatTaxableTotal> { }

		/// <summary>
		/// The document total that is subjected to VAT in the base currency.
		/// </summary>
		[PXDBDecimal(4)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual decimal? VatTaxableTotal
		{
			get;
			set;
		}
		#endregion
		#region CuryTaxAmt
		public abstract class curyTaxAmt : PX.Data.BQL.BqlDecimal.Field<curyTaxAmt> { }

		/// <summary>
		/// The tax amount to be paid for the document in the selected currency.
		/// This field is enable and visible only if the <see cref="CASetup.RequireControlTaxTotal"/> field 
		/// and the <see cref="FeaturesSet.NetGrossEntryMode"/> field are set to <c>true</c>.
		/// </summary>
		[PXDBCurrency(typeof(CAExpense.curyInfoID), typeof(CAExpense.taxAmt))]
		[PXUIField(DisplayName = "Tax Amount")]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual decimal? CuryTaxAmt
		{
			get;
			set;
		}
		#endregion
		#region TaxAmt
		public abstract class taxAmt : PX.Data.BQL.BqlDecimal.Field<taxAmt> { }

		/// <summary>
		/// The tax amount to be paid for the document in the base currency.
		/// </summary>
		[PXDBDecimal(4, BqlField = typeof(CAExpense.taxAmt))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual decimal? TaxAmt
		{
			get;
			set;
		}
		#endregion
		#region CurySplitTotal
		public abstract class curySplitTotal : PX.Data.BQL.BqlDecimal.Field<curySplitTotal> { }

		/// <summary>
		/// The sum of amounts of all detail lines in the selected currency.
		/// </summary>
		[PXDBCurrency(typeof(CAExpense.curyInfoID), typeof(CAExpense.splitTotal))]
		[PXUIField(DisplayName = "Detail Total", Visibility = PXUIVisibility.Visible, Enabled = false)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual decimal? CurySplitTotal
		{
			get;
			set;
		}
		#endregion
		#region SplitTotal
		public abstract class splitTotal : PX.Data.BQL.BqlDecimal.Field<splitTotal> { }

		/// <summary>
		/// The sum of amounts of all detail lines in the base currency.
		/// </summary>
		[PXDBDecimal(4)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual decimal? SplitTotal
		{
			get;
			set;
		}
		#endregion
		#region CuryControlAmt
		public abstract class curyControlAmt : PX.Data.BQL.BqlDecimal.Field<curyControlAmt> { }

		/// <summary>
		/// The control total of the transaction in the selected currency.
		/// A user enters this amount manually.
		/// This amount should be equal to the <see cref="CurySplitTotal">sum of amounts of all detail lines</see> of the transaction.
		/// </summary>
		[PXDBCurrency(typeof(CAExpense.curyInfoID), typeof(CAExpense.controlAmt))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Control Total")]
		public virtual decimal? CuryControlAmt
		{
			get;
			set;
		}
		#endregion
		#region ControlAmt
		public abstract class controlAmt : PX.Data.BQL.BqlDecimal.Field<controlAmt> { }

		/// <summary>
		/// The control total of the transaction in the base currency.
		/// </summary>
		[PXDBBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual decimal? ControlAmt
		{
			get;
			set;
		}
		#endregion
		#region TaxCalcMode
		public abstract class taxCalcMode : PX.Data.BQL.BqlString.Field<taxCalcMode> { }

		/// <summary>
		/// The tax calculation mode, which defines which amounts (tax-inclusive or tax-exclusive) 
		/// should be entered in the detail lines of a document. 
		/// This field is displayed only if the <see cref="FeaturesSet.NetGrossEntryMode"/> field is set to <c>true</c>.
		/// </summary>
		/// <value>
		/// The field can have one of the following values:
		/// <c>"T"</c> (Tax Settings): The tax amount for the document is calculated according to the settings of the applicable tax or taxes.
		/// <c>"G"</c> (Gross): The amount in the document detail line includes a tax or taxes.
		/// <c>"N"</c> (Net): The amount in the document detail line does not include taxes.
		/// </value>
		[PXDBString(1, IsFixed = true)]
		[PXDefault(TaxCalculationMode.TaxSetting, typeof(Search<CashAccountETDetail.taxCalcMode,
			Where<CashAccountETDetail.accountID, Equal<Current<CAExpense.cashAccountID>>,
				And<CashAccountETDetail.entryTypeID, Equal<Current<CAExpense.entryTypeID>>>>>))]
		[PXFormula(typeof(Default<cashAccountID, entryTypeID>))]
		[TaxCalculationMode.List]
		[PXUIField(DisplayName = "Tax Calculation Mode")]
		public virtual string TaxCalcMode
		{
			get;
			set;
		}
		#endregion
		#region CuryTaxRoundDiff
		public abstract class curyTaxRoundDiff : PX.Data.BQL.BqlDecimal.Field<curyTaxRoundDiff> { }

		/// <summary>
		/// The difference between the original document amount and the rounded amount in the selected currency.
		/// </summary>
		[PXDBCurrency(typeof(CAExpense.curyInfoID), typeof(CAExpense.taxRoundDiff), BaseCalc = false)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Rounding Diff.", Enabled = false, Visibility = PXUIVisibility.SelectorVisible, Visible = false)]
		public decimal? CuryTaxRoundDiff
		{
			get;
			set;
		}
		#endregion
		#region TaxRoundDiff
		public abstract class taxRoundDiff : PX.Data.BQL.BqlDecimal.Field<taxRoundDiff> { }

		/// <summary>
		/// The difference between the original document amount and the rounded amount in the base currency.
		/// </summary>
		[PXDBBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public decimal? TaxRoundDiff
		{
			get;
			set;
		}
		#endregion
		#region HasWithHoldTax - unbound
		public abstract class hasWithHoldTax : PX.Data.BQL.BqlBool.Field<hasWithHoldTax> { }

		/// <summary>
		/// Specifies (if set to <c>true</c>) that withholding taxes are applied to the document.
		/// This is a technical field, which is calculated on the fly and is used to restrict the values of the <see cref="TaxCalcMode"/> field.
		/// </summary>
		[PXBool]
		[RestrictWithholdingTaxCalcMode(typeof(CAExpense.taxZoneID), typeof(CAExpense.taxCalcMode))]
		public virtual bool? HasWithHoldTax
		{
			get;
			set;
		}
		#endregion
		#region HasUseTax - unbound
		public abstract class hasUseTax : PX.Data.BQL.BqlBool.Field<hasUseTax> { }

		/// <summary>
		/// Specifies (if set to <c>true</c>) that use taxes are applied to the document.
		/// This is a technical field, which is calculated on the fly and is used to restrict the values of the <see cref="TaxCalcMode"/> field.
		/// </summary>
		[PXBool]
		[RestrictUseTaxCalcMode(typeof(CAExpense.taxZoneID), typeof(CAExpense.taxCalcMode))]
		public virtual bool? HasUseTax
		{
			get;
			set;
		}
		#endregion
		#region UsesManualVAT
		public abstract class usesManualVAT : PX.Data.BQL.BqlBool.Field<usesManualVAT> { }

		/// <summary>
		/// Specifies (if set to <c>true</c>) that the document's tax zone is set up as "Manual VAT Zone".
		/// This is a technical field, which is used to restrict the values of the <see cref="TaxCalcMode"/> field.
		/// </summary>
		[PXDBBool]
		[RestrictManualVAT(typeof(CAExpense.taxZoneID), typeof(CAExpense.taxCalcMode))]
		[PXUIField(DisplayName = "Manual VAT Entry", Enabled = false)]
		public virtual bool? UsesManualVAT
		{
			get;
			set;
		}
		#endregion
		#region PaymentsReclassification - unbound
		public abstract class paymentsReclassification : PX.Data.BQL.BqlBool.Field<paymentsReclassification> { }

		/// <summary>
		/// Specifies (if set to <c>true</c>) that this transaction is used for payments reclassification.
		/// </summary>
		/// <value>
		/// Corresponds to the value of the <see cref="CAEntryType.UseToReclassifyPayments"/> field of the selected <see cref="CAEntryType">entry type</see>.
		/// </value>
		[PXBool]
		[PXDBScalar(typeof(Search<CAEntryType.useToReclassifyPayments, Where<CAEntryType.entryTypeId, Equal<CAExpense.entryTypeID>>>))]
		public virtual bool? PaymentsReclassification
		{
			get;
			set;
		}
		#endregion
		#region NoteID
		public abstract class noteID : PX.Data.BQL.BqlGuid.Field<noteID> { }

		[PXNote]
		public virtual Guid? NoteID
		{
			get;
			set;
		}
		#endregion
		#region CreatedByID
		public abstract class createdByID : PX.Data.BQL.BqlGuid.Field<createdByID> { }

		[PXDBCreatedByID]
		public virtual Guid? CreatedByID
		{
			get;
			set;
		}
		#endregion
		#region CreatedByScreenID
		public abstract class createdByScreenID : PX.Data.BQL.BqlString.Field<createdByScreenID> { }

		[PXDBCreatedByScreenID]
		public virtual string CreatedByScreenID
		{
			get;
			set;
		}
		#endregion
		#region CreatedDateTime
		public abstract class createdDateTime : PX.Data.BQL.BqlDateTime.Field<createdDateTime> { }

		[PXDBCreatedDateTime]
		public virtual DateTime? CreatedDateTime
		{
			get;
			set;
		}
		#endregion
		#region LastModifiedByID
		public abstract class lastModifiedByID : PX.Data.BQL.BqlGuid.Field<lastModifiedByID> { }

		[PXDBLastModifiedByID]
		public virtual Guid? LastModifiedByID
		{
			get;
			set;
		}
		#endregion
		#region LastModifiedByScreenID
		public abstract class lastModifiedByScreenID : PX.Data.BQL.BqlString.Field<lastModifiedByScreenID> { }

		[PXDBLastModifiedByScreenID]
		public virtual string LastModifiedByScreenID
		{
			get;
			set;
		}
		#endregion
		#region LastModifiedDateTime
		public abstract class lastModifiedDateTime : PX.Data.BQL.BqlDateTime.Field<lastModifiedDateTime> { }

		[PXDBLastModifiedDateTime]
		public virtual DateTime? LastModifiedDateTime
		{
			get;
			set;
		}
		#endregion
		#region tstamp
		public abstract class Tstamp : PX.Data.BQL.BqlByteArray.Field<Tstamp> { }

		[PXDBTimestamp]
		public virtual byte[] tstamp
		{
			get;
			set;
		}
		#endregion
	}
}