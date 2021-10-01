using System;
using System.Collections.Generic;
using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Objects.CA;
using PX.Objects.CS;
using PX.Data.BQL;
using PX.Objects.Common.Extensions;

namespace PX.Objects.GL
{ 
	/// <summary>
	/// Represents CashAccount Field with Selector that shows all Cash Accounts.
	/// </summary>	
	[PXUIField(DisplayName = "Cash Account", Visibility = PXUIVisibility.Visible, FieldClass = DimensionName)]
	public abstract class CashAccountBaseAttribute : AcctSubAttribute, IPXFieldUpdatingSubscriber
	{
		private Type[] _selectorCols = new Type[]
		{
			typeof(CashAccount.cashAccountCD),
			typeof(CashAccount.accountID),
			typeof(CashAccount.descr),
			typeof(CashAccount.curyID),
			typeof(CashAccount.subID),
			typeof(CashAccount.branchID)
		};

		private Type _branchID;

		[Flags]
		private enum SearchParamTypes
		{
			Join = 0x01,
			Where = 0x02,
			Aggregate = 0x04,
			OrderBy = 0x08
		}

		private enum SearchTypes
		{
			Search23, Search24, Search54, Search55
		}

		public const string DimensionName = "CASHACCOUNT";

		public bool SuppressCurrencyValidation
		{
			get;
			set;
		}

		/// <summary>
		/// Constructor of the new CashAccountBaseAttribute object with all default parameters.
		/// Doesn't filter by branch, doesn't suppress <see cref="CashAccount.Active"/> status verification
		/// </summary>
		public CashAccountBaseAttribute() : this(suppressActiveVerification: false)
		{
		}

		/// <summary>
		/// Constructor of the new CashAccountBaseAttribute object with all default parameters except the <paramref name="search"/>.
		/// Doesn't filter by branch, doesn't suppress <see cref="CashAccount.Active"/> status verification
		/// </summary>
		/// <param name="search">The type of search. Should implement <see cref="IBqlSearch"/> or <see cref="IBqlSelect"/></param>
		public CashAccountBaseAttribute(Type search) : this(suppressActiveVerification: false, search: search)
		{
		}

		/// <summary>
		/// Constructor of the new CashAccountBaseAttribute object. Doesn't filter by branch.
		/// </summary>
		/// <param name="suppressActiveVerification">True to suppress <see cref="CashAccount.Active"/> verification.</param>
		/// <param name="branchID">(Optional) Identifier for the branch.</param>
		/// <param name="search">(Optional) The type of search. Should implement <see cref="IBqlSearch"/> or <see cref="IBqlSelect"/></param>
		public CashAccountBaseAttribute(bool suppressActiveVerification, Type branchID = null, Type search = null)
		{
			InitAttribute(search, branchID, filterBranch: false, suppressActiveVerify: suppressActiveVerification);
		}

		/// <summary>
		/// Constructor of the new CashAccountBaseAttribute object.
		/// </summary>
		/// <param name="suppressActiveVerification">True to suppress <see cref="CashAccount.Active"/> verification.</param>
		/// <param name="branchID">(Optional) Identifier for the branch.</param>
		/// <param name="search">(Optional) The type of search. Should implement <see cref="IBqlSearch"/> or <see cref="IBqlSelect"/></param>
		public CashAccountBaseAttribute(bool suppressActiveVerification, bool filterBranch, Type branchID = null, Type search = null)
		{
			InitAttribute(search, branchID, filterBranch: filterBranch, suppressActiveVerify: suppressActiveVerification);
		}

		/// <summary>
		/// Constructor of the new CashAccountBaseAttribute object. Filter by branch, doesn't suppress <see cref="CashAccount.Active"/> status verification.
		/// </summary>
		/// <param name="branchID">Identifier for the branch.</param>
		/// <param name="search">The type of search. Should implement <see cref="IBqlSearch"/> or <see cref="IBqlSelect"/></param>
		public CashAccountBaseAttribute(Type branchID, Type search)
		{
			InitAttribute(search, branchID, filterBranch: true, suppressActiveVerify: false);
		}

		/// <summary>
		/// Constructor of the new CashAccountBaseAttribute object. Filter by branch, doesn't suppress <see cref="CashAccount.Active"/> status verification.
		/// </summary>
		/// <param name="branchID">Identifier for the branch.</param>
		/// <param name="search">The type of search. Should implement <see cref="IBqlSearch"/> or <see cref="IBqlSelect"/></param>
		/// <param name="showClearingColumn"> If True, shows the Clearing Account column in a cash account selector</param>
		public CashAccountBaseAttribute(Type branchID, Type search, bool showClearingColumn)
		{
			InitAttribute(search, branchID, filterBranch: true, suppressActiveVerify: false, showClearingColumn);
		}

		public override void CacheAttached(PXCache sender)
		{
			base.CacheAttached(sender);

			if (_branchID != null)
			{
				sender.Graph.FieldUpdated.AddHandler(BqlCommand.GetItemType(_branchID), _branchID.Name, BranchFieldUpdated);
			}
		}

		private void InitAttribute(Type search, Type branchID, bool filterBranch, bool suppressActiveVerify, bool showClearingColumn = false)
		{
			_branchID = branchID;
			search = search ?? typeof(Search<CashAccount.cashAccountID>);

			Type searchCommand = generateNewSearch(search.GetGenericArguments(), _branchID, filterBranch);

			if (searchCommand == null)
			{
				throw new PXArgumentException("search", ErrorMessages.ArgumentException);
			}

			if (showClearingColumn)
			{
				_selectorCols = new Type[]
				{
					typeof(CashAccount.cashAccountCD),
					typeof(CashAccount.accountID),
					typeof(CashAccount.descr),
					typeof(CashAccount.curyID),
					typeof(CashAccount.subID),
					typeof(CashAccount.branchID),
					typeof(CashAccount.clearingAccount)
				};
			}

			var dimensionSelectorAttribute = new PXDimensionSelectorAttribute(DimensionName, searchCommand, typeof(CashAccount.cashAccountCD), _selectorCols)
			{
				CacheGlobal = true,
				DescriptionField = typeof(CashAccount.descr)
			};

			_Attributes.Add(dimensionSelectorAttribute);

			_SelAttrIndex = _Attributes.Count - 1;
			this.Filterable = true;

			if (!suppressActiveVerify)
			{
				Type msgParam = typeof(CashAccount.cashAccountCD);
				PXRestrictorAttribute cashAccountActiveStatusRestrictor = new PXRestrictorAttribute(typeof(Where<CashAccount.active, Equal<True>>),
																									CA.Messages.CashAccountInactive, 
																									msgParam);
				_Attributes.Add(cashAccountActiveStatusRestrictor);
			}

			if (PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>() 
				&& _branchID != null )
			{
				_Attributes.Add(new PXRestrictorAttribute(BqlCommand.Compose(
						typeof(Where<,>),
						typeof(CashAccount.baseCuryID),
						typeof(EqualBaseCuryID<>),
						typeof(Current<>), _branchID), Messages.BaseCurrencyDiffers, typeof(CashAccount.branchID)));
			}
		}

		private Type generateNewSearch(Type[] searchArgs, Type branchID, bool? filterBranch)
		{
			branchID = branchID ?? typeof(AccessInfo.branchID);
			SearchParamTypes argsTypes = 0;
			SearchTypes searchType = 0;
			var typesDict = new Dictionary<Type, Type>();

			foreach (Type arg in searchArgs)
			{
				if (typeof(IBqlJoin).IsAssignableFrom(arg))
				{
					typesDict[typeof(IBqlJoin)] = arg;
					argsTypes |= SearchParamTypes.Join;
				}
				else if (typeof(IBqlWhere).IsAssignableFrom(arg))
				{
					typesDict[typeof(IBqlWhere)] = arg;
					argsTypes |= SearchParamTypes.Where;
				}
				else if (typeof(IBqlAggregate).IsAssignableFrom(arg))
				{
					typesDict[typeof(IBqlAggregate)] = arg;
					argsTypes |= SearchParamTypes.Aggregate;
				}
				else if (typeof(IBqlOrderBy).IsAssignableFrom(arg))
				{
					typesDict[typeof(IBqlOrderBy)] = arg;
					argsTypes |= SearchParamTypes.OrderBy;
				}
			}

			Type newSearch = null;
			Type newSearchType = null;

			switch (argsTypes & (SearchParamTypes.Aggregate | SearchParamTypes.OrderBy))
			{
				case SearchParamTypes.Aggregate:
					newSearchType = typeof(Search5<,,,>);
					searchType = SearchTypes.Search54;
					break;
				case SearchParamTypes.OrderBy:
					newSearchType = typeof(Search2<,,,>);
					searchType = SearchTypes.Search24;
					break;
				case (SearchParamTypes.Aggregate | SearchParamTypes.OrderBy):
					newSearchType = typeof(Search5<,,,,>);
					searchType = SearchTypes.Search55;
					break;
				case 0:
					newSearchType = typeof(Search2<,,>);
					searchType = SearchTypes.Search23;
					break;
			}

			if (newSearchType != null)
			{
				if (typesDict.ContainsKey(typeof(IBqlJoin)))
				{
					typesDict[typeof(IBqlJoin)] = BqlCommand.Compose(
						typeof(InnerJoin<,,>),
						typeof(Account),
						typeof(On<,,>),
						typeof(Account.accountID), 
							typeof(Equal<CashAccount.accountID>),
						typeof(And2<,>),
						typeof(Match<Account, Current<AccessInfo.userName>>),
						typeof(And<>),
						typeof(Match<,>),
						typeof(Account),
						typeof(Optional<>), 
							branchID,
						typeof(InnerJoin<,,>),
						typeof(Sub),
						typeof(On<Sub.subID, Equal<CashAccount.subID>, And<Match<Sub, Current<AccessInfo.userName>>>>),
						typesDict[typeof(IBqlJoin)]);
				}
				else
				{
					typesDict[typeof(IBqlJoin)] = typeof(InnerJoin<Account,
						On<Account.accountID, Equal<CashAccount.accountID>, And<Match<Account, Current<AccessInfo.userName>>>>,
						InnerJoin<Sub, On<Sub.subID, Equal<CashAccount.subID>, And<Match<Sub, Current<AccessInfo.userName>>>>>>);
				}

				List<Type> args = new List<Type>
				{
					typeof(Where2<,>),
					typeof(Not<>),
					typeof(FeatureInstalled<FeaturesSet.branch>),
					typeof(Or<,,>),
					typeof(CashAccount.restrictVisibilityWithBranch),
					typeof(Equal<>),
					typeof(False),
					filterBranch == true 
						?typeof(Or<,,>)
						:typeof(Or<,>),
					typeof(CashAccount.branchID)
				};

				Type[] filterConditions = filterBranch == true ? new Type[] { typeof(Equal<>), typeof(Current<>), branchID, typeof(Or<,>), typeof(Current<>), branchID, typeof(IsNull) }
															   : new Type[] { typeof(IsNotNull) };
				args.AddRange(filterConditions);

				if (typesDict.ContainsKey(typeof(IBqlWhere)))
				{
					args.Insert(0, typeof(Where2<,>));
					args.AddRange(new Type[] { typeof(And<>), typesDict[typeof(IBqlWhere)] });
				}

				typesDict[typeof(IBqlWhere)] = BqlCommand.Compose(args.ToArray());
				var newSearchList = new List<Type> { newSearchType, typeof(CashAccount.cashAccountID), typesDict[typeof(IBqlJoin)], typesDict[typeof(IBqlWhere)] };

				if (searchType == SearchTypes.Search54 || searchType == SearchTypes.Search55)
				{
					newSearchList.Add(typesDict[typeof(IBqlAggregate)]);
				}

				if (searchType == SearchTypes.Search24 || searchType == SearchTypes.Search55)
				{
					newSearchList.Add(typesDict[typeof(IBqlOrderBy)]);
				}

				newSearch = BqlCommand.Compose(newSearchList.ToArray());
			}
			return newSearch;
		}

		protected virtual void BranchFieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			PXFieldState state = (PXFieldState)sender.GetValueExt(e.Row, _FieldName);

			if (state != null && state.Value != null)
			{
				sender.SetValue(e.Row, _FieldName, null);
				sender.SetValueExt(e.Row, _FieldName, state.Value);
			}
		}

		public void FieldUpdating(PXCache sender, PXFieldUpdatingEventArgs e)
		{
			VerifyAccount(sender, e);
			VerifySubaccount(sender, e);
		}

		private void VerifyAccount(PXCache sender, PXFieldUpdatingEventArgs e)
		{
			if(e.NewValue == null)
			{
				return;
			}

			CashAccount cashAccount = SelectFrom<CashAccount>.Where<CashAccount.cashAccountID.IsEqual<@P.AsInt>>.View.Select(sender.Graph, e.NewValue);
			Account account = SelectFrom<Account>.Where<Account.accountID.IsEqual<@P.AsInt>>.View.Select(sender.Graph, cashAccount?.AccountID);
			if (account != null && account.Active == false)
			{
				PXSetPropertyException exception = new PXSetPropertyException(CA.Messages.GLAccountIsInactive, PXErrorLevel.Error, cashAccount.CashAccountCD, account.AccountCD);
				sender.RaiseExceptionHandling(_FieldName, e.Row, cashAccount.CashAccountCD, exception);
			}
		}
		
		private void VerifySubaccount(PXCache sender, PXFieldUpdatingEventArgs e)
		{
			if (e.NewValue == null)
			{
				return;
			}

			CashAccount cashAccount = SelectFrom<CashAccount>.Where<CashAccount.cashAccountID.IsEqual<@P.AsInt>>.View.Select(sender.Graph, e.NewValue);
			Sub sub = SelectFrom<Sub>.Where<Sub.subID.IsEqual<@P.AsInt>>.View.Select(sender.Graph, cashAccount.SubID);
			if (sub != null && sub.Active == false)
			{
				string subCD = sender.Graph.Caches[typeof(Sub)].GetFormatedMaskField<Sub.subCD>(sub);
				PXSetPropertyException exception = new PXSetPropertyException(CA.Messages.SubaccountIsInactive, PXErrorLevel.Error, subCD);
				sender.RaiseExceptionHandling(_FieldName, e.Row, cashAccount.CashAccountCD, exception);
			}
		}
	}
}
