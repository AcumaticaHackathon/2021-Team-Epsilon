using System;
using PX.Data;
using PX.Data.BQL;
using PX.Data.EP;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.CT;
using PX.Objects.SP.DAC;
using SP.Objects.SP;

namespace SP.Objects.CR
{
    [Serializable]
    [CRPrimaryGraphRestricted(
        new[]{
            typeof(CRCaseMaint)
        },
        new[]{
            typeof(Select2<
                    CRCase,
                InnerJoin<CRCaseClass,
                    On<CRCaseClass.caseClassID, Equal<CRCase.caseClassID>,
                    And<CRCaseClass.isInternal, Equal<False>>>>,
                Where<
                    CRCase.caseCD, Equal<Current<CRCase.caseCD>>,
                    And<MatchWithBAccountNotNull<CRCase.customerID>>>>)
        })]
    [PXCacheName(PX.Objects.CR.Messages.Case)]
    [CREmailContactsView(typeof(Select2<Contact,
        LeftJoin<BAccount, On<BAccount.bAccountID, Equal<Contact.bAccountID>>>,
        Where<Contact.bAccountID, Equal<Optional<CRCase.customerID>>,
           Or<Contact.contactType, Equal<ContactTypesAttribute.employee>>>>))]
    [PXEMailSource]//NOTE: for assignment map
    public class CRCaseExt: PXCacheExtension<CRCase>
	{
		#region CaseCD
		public abstract class caseCD : IBqlField { }
		[PXDBString(10, IsUnicode = true, IsKey = true, InputMask = ">CCCCCCCCCCCCCCC")]
		[PXDefault()]
		[PXUIField(DisplayName = "Case ID", Visibility = PXUIVisibility.SelectorVisible)]
		[AutoNumber(typeof(CRSetup.caseNumberingID), typeof(AccessInfo.businessDate))]
		[PXSelector(typeof(Search2<CRCase.caseCD,
			LeftJoin<CRCaseClass, On<CRCase.caseClassID, Equal<CRCaseClass.caseClassID>>,
			LeftJoin<BAccount, On<BAccount.bAccountID, Equal<CRCase.customerID>>,
			LeftJoin<CRCustomerClass, On<CRCustomerClass.cRCustomerClassID, Equal<BAccount.classID>>,
			LeftJoin<Contact, On<Contact.contactID, Equal<CRCase.contactID>>,
			LeftJoin<CRContactClass, On<CRContactClass.classID, Equal<Contact.classID>>>>>>>,
			Where<Brackets<MatchWithBAccountNotNull<CRCase.customerID>>
				.And<Brackets<CRCaseClass.isInternal.IsEqual<False>.Or<CRCaseClass.isInternal.IsNull>>>
				.And<Brackets<CRCustomerClass.isInternal.IsEqual<False>.Or<CRCustomerClass.isInternal.IsNull>>>
				.And<Brackets<CRContactClass.isInternal.IsEqual<False>.Or<CRContactClass.isInternal.IsNull>>>>, 
			OrderBy<Desc<CRCase.caseCD>>>),
			typeof(CRCase.caseCD),
			typeof(CRCase.subject),
			typeof(CRCase.status),
			Filterable = true)]
		[PXFieldDescription]
		public virtual String CaseCD { get; set; }
		#endregion

        #region ContractID
        public abstract class contractID : IBqlField { }
        [PXDBInt]
        [PXUIField(DisplayName = "Contract")]
        [PXSelector(typeof(Search2<Contract.contractID,
                LeftJoin<ContractBillingSchedule, On<Contract.contractID, Equal<ContractBillingSchedule.contractID>>>,
            Where<Contract.isTemplate, NotEqual<True>,
                And<Contract.baseType, Equal<Contract.ContractBaseType>,
				And<Where2<
							MatchWithBAccountNotNull<Contract.customerID>,
						Or<
							MatchWithBAccountNotNull<ContractBillingSchedule.accountID>>>
				>>>,
                OrderBy<Desc<Contract.contractCD>>>),
            new Type[] 
                    { 
                        typeof(Contract.contractCD),
			            typeof(Contract.description),
			            typeof(Contract.status),
			            typeof(Contract.expireDate),
                    },
            DescriptionField = typeof(Contract.description),
            SubstituteKey = typeof(Contract.contractCD), Filterable = true)]
        [PXRestrictor(typeof(Where<Contract.status, Equal<Contract.status.active>>), PX.Objects.CR.Messages.ContractIsNotActive)]
        [PXRestrictor(typeof(Where<Current<AccessInfo.businessDate>, LessEqual<Contract.graceDate>, Or<Contract.expireDate, IsNull>>), PX.Objects.CR.Messages.ContractExpired)]
        [PXRestrictor(typeof(Where<Current<AccessInfo.businessDate>, GreaterEqual<Contract.startDate>>), PX.Objects.CR.Messages.ContractActivationDateInFuture, typeof(Contract.startDate))]
        [PXFormula(typeof(Default<CRCase.customerID>))]
        [PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual int? ContractID { get; set; }
        #endregion

		#region CaseClassID
		public abstract class caseClassID : IBqlField { }

		[PXDBString(10, IsUnicode = true, InputMask = ">aaaaaaaaaa")]
        [PXDefault(typeof(Search<PortalSetup.defaultCaseClassID,
			Where<PortalSetup.IsCurrentPortal>>))]
		[PXUIField(DisplayName = "Class ID")]
		[PXSelector(typeof(Search<CRCaseClass.caseClassID,
			Where<CRCaseClass.isInternal,Equal<False>>>),
			DescriptionField = typeof(CRCaseClass.description),
			CacheGlobal = true)]
		public virtual String CaseClassID { get; set; }
		#endregion

        #region Priority
        public abstract class priority : IBqlField { }

        [PXDBString(1, IsFixed = true)]
        [PXDefault(typeof(Search<PortalSetup.defaultCasePriority>))]
        [PXUIField(DisplayName = "Priority")]
        [PXStringList(new string[] { "L", "M", "H" },
            new string[] { "Low", "Medium", "High" })]
        public virtual String Priority { get; set; }
        #endregion

        public string ClassID
        {
            get { return Base.CaseClassID; }
        }

		#region NoteID
		public abstract class noteID : IBqlField { }

		[PXSearchable(PX.Objects.SM.SearchCategory.CR, PX.Objects.CR.Messages.CaseSearchTitle, new Type[] { typeof(CRCase.caseCD), typeof(CRCase.customerID), typeof(BAccount.acctName) },
			new Type[] { typeof(CRCase.contactID), typeof(Contact.firstName), typeof(Contact.lastName), typeof(Contact.eMail),
				typeof(CRCase.ownerID), typeof(PX.Objects.EP.EPEmployee.acctCD),typeof(PX.Objects.EP.EPEmployee.acctName),typeof(CRCase.subject) },
			NumberFields = new Type[] { typeof(CRCase.caseCD) },
			Line1Format = "{1}{3}{4}", Line1Fields = new Type[] { typeof(CRCase.caseClassID), typeof(CRCaseClass.description), typeof(CRCase.contactID), typeof(Contact.fullName), typeof(CRCase.status) },
			Line2Format = "{0}", Line2Fields = new Type[] { typeof(CRCase.subject) },
			MatchWithJoin = typeof(InnerJoin<PX.Objects.AR.Customer, On<PX.Objects.AR.Customer.bAccountID, Equal<CRCase.customerID>>>)
		)]
		[PXNote(
			DescriptionField = typeof(CRCase.caseCD),
			Selector = typeof(CRCase.caseCD),
			ShowInReferenceSelector = true)]
		public virtual Guid? NoteID { get; set; }
		#endregion
    }

	public class Contact2 : Contact
	{
		public new abstract class contactID : PX.Data.BQL.BqlInt.Field<contactID> { }

		public new abstract class bAccountID : PX.Data.BQL.BqlInt.Field<bAccountID> { }

		public new abstract class userID : PX.Data.BQL.BqlGuid.Field<userID> { }
	}

	#region BAccount2

	[Serializable]
	public sealed class BAccount2 : BAccount
	{
		public new abstract class bAccountID : IBqlField { }

		public new abstract class acctCD : IBqlField { }

		public new abstract class acctName : IBqlField { }

		public new abstract class acctReferenceNbr : IBqlField { }

		public new abstract class parentBAccountID : IBqlField { }

		public new abstract class ownerID : IBqlField { }

		public new abstract class type : IBqlField { }

		public new abstract class defContactID : IBqlField { }

		public new abstract class defLocationID : IBqlField { }
	}

	#endregion
}
