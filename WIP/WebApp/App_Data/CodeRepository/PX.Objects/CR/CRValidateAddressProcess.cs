using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PX.Common;
using PX.CS.Contracts.Interfaces;
using PX.Data;
using PX.Data.EP;
using PX.Objects.AR;
using PX.Objects.Common;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.SM;

namespace PX.Objects.CR
{
	[Serializable]
	public class CRValidateAddressProcess : PXGraph<CRValidateAddressProcess>
	{
		#region DACs
		[Serializable]
		[PXHidden]
		public class ValidateAddressFilter : IBqlTable
		{
			#region IsOverride
			public abstract class isOverride : PX.Data.BQL.BqlBool.Field<isOverride> { }

			[PXBool]
			[PXUIField(DisplayName = "Override Addresses Automatically")]
			public virtual bool? IsOverride { get; set; }
			#endregion

			#region Country
			public abstract class country : PX.Data.BQL.BqlString.Field<country> { }

			[PXString(2, InputMask = ">??")]
			[PXUIField(DisplayName = "Country")]
			[PXSelector(typeof(Search<Country.countryID>),
				typeof(Country.countryID),
				typeof(Country.description),
				typeof(Country.addressValidatorPluginID),
				DescriptionField = typeof(Country.description))]
			public virtual string Country { get; set; }
			#endregion

			#region BAccountType
			public abstract class bAccountType : PX.Data.BQL.BqlString.Field<bAccountType> { }

			[PXString(2, IsFixed = true)]
			[PXUIField(DisplayName = "Customer/Vendor Type")]
			[BAccountType.List]
			public virtual string BAccountType { get; set; }
			#endregion

			#region BAccountStatus
			public abstract class bAccountStatus : PX.Data.BQL.BqlString.Field<bAccountStatus> { }

			[PXString(2, IsFixed = true)]
			[PXUIField(DisplayName = "Customer/Vendor Status")]
			[CustomerStatus.List]
			public virtual string BAccountStatus { get; set; }
			#endregion
		}

		[PXHidden]
		[PXProjection(typeof(Select2<Address,
			InnerJoin<BAccount, On<BAccount.bAccountID, Equal<Address.bAccountID>>>,
			Where<Address.isValidated, NotEqual<True>>>),
			new Type[] { typeof(Address) }, Persistent = true)]
		public class BAccountAddress : IBqlTable, IAddressBase, IValidatedAddress
		{
			#region Selected
			public abstract class selected : PX.Data.BQL.BqlBool.Field<selected> { }

			[PXBool]
			[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
			[PXUIField(DisplayName = "Selected")]
			public virtual bool? Selected { get; set; }
			#endregion

			#region AddressID
			public abstract class addressID : PX.Data.BQL.BqlInt.Field<addressID> { }

			[PXDBIdentity(IsKey = true, BqlField = typeof(Address.addressID))]
			[PXUIField(DisplayName = "Address ID")]
			public virtual int? AddressID { get; set; }
			#endregion

			#region AcctCD
			public abstract class acctCD : PX.Data.BQL.BqlString.Field<acctCD> { }

			[PXDimensionSelector("BIZACCT", typeof(Search<BAccount.acctCD, Where<Match<Current<AccessInfo.userName>>>>))]
			[PXDBString(30, IsUnicode = true, BqlField = typeof(BAccount.acctCD))]
			[PXUIField(DisplayName = "Customer/Vendor")]
			public virtual string AcctCD { get; set; }
			#endregion
			#region AcctName
			public abstract class acctName : PX.Data.BQL.BqlString.Field<acctName> { }

			[PXDBString(255, IsUnicode = true, BqlField = typeof(BAccount.acctName))]
			[PXUIField(DisplayName = "Name")]
			public virtual string AcctName { get; set; }
			#endregion
			#region Status
			public abstract class status : PX.Data.BQL.BqlString.Field<status> { }

			[PXDBString(1, IsFixed = true, BqlField = typeof(BAccount.status))]
			[PXUIField(DisplayName = "Status")]
			[CustomerStatus.List]
			public virtual string Status { get; set; }
			#endregion
			#region Type
			public abstract class type : PX.Data.BQL.BqlString.Field<type> { }

			[PXDBString(2, IsFixed = true, BqlField = typeof(BAccount.type))]
			[PXUIField(DisplayName = "Type")]
			[BAccountType.List]
			public virtual string Type { get; set; }
			#endregion

			#region AddressLine1
			public abstract class addressLine1 : PX.Data.BQL.BqlString.Field<addressLine1> { }

			[PXDBString(50, IsUnicode = true, BqlField = typeof(Address.addressLine1))]
			[PXUIField(DisplayName = "Address Line 1")]
			public virtual string AddressLine1 { get; set; }
			#endregion

			#region AddressLine2
			public abstract class addressLine2 : PX.Data.BQL.BqlString.Field<addressLine2> { }

			[PXDBString(50, IsUnicode = true, BqlField = typeof(Address.addressLine2))]
			[PXUIField(DisplayName = "Address Line 2")]
			public virtual string AddressLine2 { get; set; }
			#endregion

			#region AddressLine3
			public abstract class addressLine3 : PX.Data.BQL.BqlString.Field<addressLine3> { }

			[PXDBString(50, IsUnicode = true, BqlField = typeof(Address.addressLine3))]
			[PXUIField(DisplayName = "Address Line 3")]
			public virtual string AddressLine3 { get; set; }
			#endregion

			#region City
			public abstract class city : PX.Data.BQL.BqlString.Field<city> { }

			[PXDBString(50, IsUnicode = true, BqlField = typeof(Address.city))]
			[PXUIField(DisplayName = "City")]
			public virtual String City { get; set; }
			#endregion

			#region CountryID
			public abstract class countryID : PX.Data.BQL.BqlString.Field<countryID> { }

			[PXDBString(2, IsFixed = true, BqlField = typeof(Address.countryID))]
			[PXUIField(DisplayName = "Country")]
			public virtual String CountryID { get; set; }
			#endregion

			#region State
			public abstract class state : PX.Data.BQL.BqlString.Field<state> { }

			[PXDBString(50, IsUnicode = true, BqlField = typeof(Address.state))]
			[PXUIField(DisplayName = "State")]
			public virtual String State { get; set; }
			#endregion

			#region PostalCode
			public abstract class postalCode : PX.Data.BQL.BqlString.Field<postalCode> { }

			[PXDBString(20, BqlField = typeof(Address.postalCode))]
			[PXUIField(DisplayName = "Postal Code")]
			public virtual String PostalCode { get; set; }
			#endregion

			#region IsValidated
			public abstract class isValidated : PX.Data.BQL.BqlBool.Field<isValidated> { }

			[PXDBBool(BqlField = typeof(Address.isValidated))]
			[PXUIField(DisplayName = "Validated")]
			public virtual bool? IsValidated { get; set; }
			#endregion
		}
		#endregion

		#region Constants
		protected readonly static Type[] AddressFieldsToValidate = new Type[6]
		{
			typeof(BAccountAddress.addressLine1), typeof(BAccountAddress.addressLine2), typeof(BAccountAddress.city),
			typeof(BAccountAddress.state), typeof(BAccountAddress.countryID), typeof(BAccountAddress.postalCode)
		};
		#endregion

		#region Views
		[PXCacheName(Messages.Filter)]
		public PXFilter<ValidateAddressFilter> Filter;

		[PXCacheName(Messages.Address)]
		[PXFilterable]
		public PXFilteredProcessing<BAccountAddress, ValidateAddressFilter,
			Where2<Where<Current<ValidateAddressFilter.country>, IsNull, Or<Current<ValidateAddressFilter.country>, Equal<BAccountAddress.countryID>>>,
				And2<Where<Current<ValidateAddressFilter.bAccountType>, IsNull, Or<Current<ValidateAddressFilter.bAccountType>, Equal<BAccountAddress.type>>>,
				And<Where<Current<ValidateAddressFilter.bAccountStatus>, IsNull, Or<Current<ValidateAddressFilter.bAccountStatus>, Equal<BAccountAddress.status>>>>>>,
			OrderBy<Asc<BAccountAddress.acctCD>>> AddressList;
		#endregion

		#region Actions
		public PXCancel<ValidateAddressFilter> Cancel;
		#endregion

		#region Ctors
		public CRValidateAddressProcess()
		{
			AddressList.SetProcessCaption(PXMessages.LocalizeNoPrefix(Messages.Validate));
			AddressList.SetProcessAllCaption(PXMessages.LocalizeNoPrefix(Messages.ValidateAll));
			Actions.Move(Messages.Process, Messages.Cancel);
		}
		#endregion

		#region Event Handlers
		protected virtual void ValidateAddressFilter_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
			ValidateAddressFilter filter = e.Row as ValidateAddressFilter;
			if (filter == null) return;

			AddressList.SetProcessDelegate((CRValidateAddressProcess graph, BAccountAddress address) => graph.ProcessAddress(graph, filter, address));

			bool isCountryWarning = PXSelectReadonly<Country, Where<Country.addressValidatorPluginID, IsNotNull>>.Select(this).Count == 0;
			PXUIFieldAttribute.SetWarning<ValidateAddressFilter.country>(Filter.Cache, null, isCountryWarning ? Messages.CountryValidationWarning : null);
		}
		#endregion

		#region Public Methods
		public virtual void ProcessAddress(PXGraph graph, ValidateAddressFilter filter, BAccountAddress address)
		{
			List<string> warnings = new List<string>();
			bool isOverride = filter?.IsOverride ?? false;

			foreach (Type field in AddressFieldsToValidate)
			{
				graph.ExceptionHandling.AddHandler(typeof(BAccountAddress), field.Name,
					new PXExceptionHandling((sender, e) => OnFieldException(sender, e, field, ref warnings)));
			}

			try
			{
				PXProcessing<BAccountAddress>.SetCurrentItem(address);

				if (ValidateAddress(graph, address, isOverride))
					PXProcessing<BAccountAddress>.SetProcessed();
				else
					PXProcessing<BAccountAddress>.SetWarning(FormatWarningMessage(warnings));
			}
			catch (PXException unknownException)
			{
				PXProcessing<BAccountAddress>.SetError(unknownException);
			}
			finally
			{
				warnings.Clear();
				graph.Clear();
			}
		}
		#endregion

		#region Protected Methods
		protected virtual bool ValidateAddress(PXGraph graph, BAccountAddress address, bool isOverride)
		{
			if (graph == null || address == null)
				return false;

			if (address.IsValidated == true)
				return true;

			PXCache cache = graph.Caches[typeof(BAccountAddress)];
			BAccountAddress addressToValidate = (!isOverride ? address : cache.Insert(cache.CreateCopy(address)) as BAccountAddress);

			try
			{
				if (PXAddressValidator.Validate(graph,
					addressToValidate,
					aSynchronous: true,
					updateToValidAddress: isOverride,
					forceOverride: isOverride))
				{
					addressToValidate.IsValidated = true;
					cache.Update(addressToValidate);

					using (PXTransactionScope ts = new PXTransactionScope())
					{
						cache.Persist(addressToValidate, PXDBOperation.Update);
						ts.Complete(graph);
					}

					return true;
				}
			}
			finally
			{
				cache.Remove(addressToValidate);
			}

			return false;
		}

		protected static string FormatWarningMessage(List<string> warnings)
		{
			if (warnings == null)
				return string.Empty;

			StringBuilder stringBuilder = new StringBuilder();

			for (int i = 0; i < warnings.Count; i++)
			{
				stringBuilder.AppendLine(warnings[i]);

				if (i < warnings.Count - 1)
					stringBuilder.AppendLine();
			}

			if (warnings.Count == 0)
				stringBuilder.Append(PXMessages.LocalizeNoPrefix(Messages.AddressInvalidWarning));

			return stringBuilder.ToString();
		}

		protected static void OnFieldException(PXCache sender, PXExceptionHandlingEventArgs e, Type field, ref List<string> warnings)
		{
			if (sender == null || e == null || field == null || warnings == null)
				return;

			BAccountAddress address = e.Row as BAccountAddress;
			PXSetPropertyException setPropertyException = e.Exception as PXSetPropertyException;

			if (address == null || setPropertyException == null || string.IsNullOrWhiteSpace(setPropertyException.MessageNoPrefix))
				return;

			warnings.Add(PXMessages.LocalizeFormatNoPrefix(
				Messages.AddressInvalidFieldWarning,
				PXUIFieldAttribute.GetDisplayName(sender, field.Name).ToLower(),
				sender.GetValue(address, field.Name),
				Environment.NewLine,
				setPropertyException.MessageNoPrefix));
		}
		#endregion
	}
}