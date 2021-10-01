using System;
using System.Linq;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.IN;

namespace PX.Objects.CR
{
	[PXDBInt]
	[PXInt]
	[PXUIField(DisplayName = "Contact", Visibility = PXUIVisibility.Visible)]
	[PXRestrictor(typeof(
		Where<
			Contact.isActive, Equal<True>>),
		Messages.ContactInactive, typeof(Contact.displayName))]
	public class ContactRawAttribute : AcctSub2Attribute
	{
		#region State

		public virtual PXSelectorMode SelectorMode { get; set; } = PXSelectorMode.DisplayModeText;

		protected Type[] ContactTypes = new []{ typeof(ContactTypesAttribute.person) };

		protected Type BAccountIDField;

		public bool WithContactDefaultingByBAccount;

		#endregion

		#region ctor

		public ContactRawAttribute()
			: this(null) { }

		public ContactRawAttribute(Type bAccountIDField)
			: this(bAccountIDField, null) { }

		public ContactRawAttribute(Type bAccountIDField = null, Type[] contactTypes = null, Type customSearchField = null, Type customSearchQuery = null, Type[] fieldList = null, string[] headerList = null)
		{
			BAccountIDField = bAccountIDField;

			ContactTypes = contactTypes ?? ContactTypes;

			PXSelectorAttribute attr =
				new PXSelectorAttribute(customSearchQuery ?? CreateSelect(bAccountIDField, customSearchField),
					fieldList: fieldList ?? new Type[]
					{
						typeof(Contact.displayName),
						typeof(Contact.salutation),
						typeof(Contact.fullName),
						typeof(BAccount.acctCD),
						typeof(Contact.eMail),
						typeof(Contact.phone1),
						typeof(Contact.contactType)
					})
				{
					Headers = headerList ?? new[]
					{
						"Contact",
						"Job Title",
						"Account Name",
						"Business Account",
						"Email",
						"Phone 1",
						"Type"
					},

					DescriptionField = typeof(Contact.displayName),
					SelectorMode = SelectorMode,
					Filterable = true,
					DirtyRead = true
				};

			_Attributes.Add(attr);

			_SelAttrIndex = _Attributes.Count - 1;

			if (bAccountIDField != null)
			{
				_Attributes.Add(new PXRestrictorAttribute(BqlCommand.Compose(
					typeof(Where<,,>), typeof(Current<>), bAccountIDField, typeof(IsNull),
						typeof(Or<,>), typeof(Contact.bAccountID), typeof(Equal<>), typeof(Current<>), bAccountIDField),
					Messages.ContactBAccountDiff)
				{
					ShowWarning = true
				});
			}
		}

		protected virtual Type GetContactTypeWhere()
		{
			Type contactTypes = null;

			switch (ContactTypes.Length)
			{
				case 1:
					contactTypes = BqlCommand.Compose(
						typeof(Where<,>),
						typeof(Contact.contactType), typeof(Equal<>), ContactTypes[0]);
					break;
				case 2:
					contactTypes = BqlCommand.Compose(
						typeof(Where<,>),
						typeof(Contact.contactType), typeof(In3<,>), ContactTypes[0], ContactTypes[1]);
					break;
				case 3:
					contactTypes = BqlCommand.Compose(
						typeof(Where<,>),
						typeof(Contact.contactType), typeof(In3<,,>), ContactTypes[0], ContactTypes[1], ContactTypes[2]);
					break;
				case 4:
					contactTypes = BqlCommand.Compose(
						typeof(Where<,>),
						typeof(Contact.contactType), typeof(In3<,,,>), ContactTypes[0], ContactTypes[1], ContactTypes[2], ContactTypes[3]);
					break;
				case 5:
					contactTypes = BqlCommand.Compose(
						typeof(Where<,>),
						typeof(Contact.contactType), typeof(In3<,,,,>), ContactTypes[0], ContactTypes[1], ContactTypes[2], ContactTypes[3], ContactTypes[4]);
					break;
			}

			return contactTypes;
		}

		protected virtual Type CreateSelect(Type bAccountIDField, Type customSearchField)
		{
			Type contactTypes = GetContactTypeWhere();

			return BqlTemplate.OfCommand<
					SelectFrom<
						Contact>
					.LeftJoin<BAccount>
						.On<BAccount.bAccountID.IsEqual<Contact.bAccountID>>
					.Where<
						Brackets<
							BAccount.bAccountID.IsNull
							.Or<Match<BAccount, Current<AccessInfo.userName>>>
						>
						.And<Brackets<
							BAccount.type.IsNull
							.Or<BAccount.type.IsEqual<BAccountType.customerType>>
							.Or<BAccount.type.IsEqual<BAccountType.prospectType>>
							.Or<BAccount.type.IsEqual<BAccountType.combinedType>>
							.Or<BAccount.type.IsEqual<BAccountType.vendorType>>
						>>
						.And<BqlPlaceholder.A>
					>
					.SearchFor<BqlPlaceholder.B>>
				.Replace<BqlPlaceholder.A>(contactTypes)
				.Replace<BqlPlaceholder.B>(customSearchField ?? typeof(Contact.contactID))
				.ToType();
		}

		#endregion

		#region Events

		public override void CacheAttached(PXCache sender)
		{
			base.CacheAttached(sender);

			if (BAccountIDField != null)
			{
				sender.Graph.FieldDefaulting.AddHandler(sender.GetItemType(), this.FieldName, ContactID_FieldDefaulting);

				sender.Graph.FieldVerifying.AddHandler(sender.GetItemType(), this.FieldName, ContactID_FieldVerifying);

				sender.Graph.FieldUpdated.AddHandler(sender.GetItemType(), BAccountIDField.Name, BAccountID_FieldUpdated);
			}
		}

		protected virtual void BAccountID_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			if (e.Row == null) return;

			var bAccountID = sender.GetValue(e.Row, BAccountIDField.Name);

			if (bAccountID == null || int.Equals(bAccountID, e.OldValue)) return;

			var contactID = sender.GetValue(e.Row, this.FieldName);

			if (contactID != null) return;

			sender.SetDefaultExt(e.Row, this.FieldName);
		}

		protected virtual void ContactID_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			if (e.Row == null || e.Cancel) return;

			var bAccountID = sender.GetValue(e.Row, BAccountIDField.Name);

			if (bAccountID == null) return;

			if (WithContactDefaultingByBAccount && !sender.Graph.UnattendedMode)
			{
				Type contactTypes = GetContactTypeWhere();

				var cmd =
					BqlTemplate.OfCommand<
						SelectFrom<Contact>
							.LeftJoin<BAccount>
								.On<BAccount.bAccountID.IsEqual<Contact.bAccountID>
								.And<BAccount.defContactID.IsNotEqual<Contact.contactID>>>
							.Where<
								Contact.bAccountID.IsEqual<P.AsInt>
								.And<Contact.isActive.IsEqual<True>>
								.And<BqlPlaceholder.A>>>
					.Replace<BqlPlaceholder.A>(contactTypes)
					.ToCommand();

				PXView query = new PXView(sender.Graph, true, cmd);
				var contactsSet = query
					.SelectMulti(bAccountID)
					.ToList();

				if (contactsSet == null) return;
				if (contactsSet.Count == 1 && contactsSet.FirstOrDefault() is PXResult<Contact> SingleContact)
				{
					e.NewValue = PXResult.Unwrap<Contact>(SingleContact)?.ContactID;
					e.Cancel = true;
				}
				else if (contactsSet.FirstOrDefault(result => PXResult.Unwrap<Contact>(result).ContactID == PXResult.Unwrap<BAccount>(result).PrimaryContactID) is PXResult<Contact> PrimaryContact)
				{
					e.NewValue = PXResult.Unwrap<Contact>(PrimaryContact)?.ContactID;
					e.Cancel = true;
				}
			}
		}

		protected virtual void ContactID_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			if (e.Row == null) return;

			// assuming that the source entity is in "legal" state
			if (sender.Graph.IsCopyPasteContext)
				e.Cancel = true;
		}

		#endregion

		#region Helpers

		#endregion
	}
}