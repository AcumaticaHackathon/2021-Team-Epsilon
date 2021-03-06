using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PX.Data;
using PX.Data.EP;
using PX.Data.WorkflowAPI;
using PX.Objects.GL;
using PX.Objects.CM;
using PX.Objects.AR;
using PX.TM;
using PX.Objects.CS;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.PM;

namespace PX.Objects.PM
{
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	[PXCacheName(Messages.ChangeRequest)]
	[Serializable]
	[PXEMailSource]
	public class PMChangeRequest : PX.Data.IBqlTable, IAssign
	{
		#region Events
		public class Events : PXEntityEvent<PMChangeRequest>.Container<Events>
		{
			public PXEntityEvent<PMChangeRequest> Open;
			public PXEntityEvent<PMChangeRequest> Close;
		}
		#endregion

		#region Selected
		public abstract class selected : PX.Data.BQL.BqlBool.Field<selected> { }
		protected bool? _Selected = false;

		/// <summary>
		/// Gets or sets whether the task is selected in the grid.
		/// </summary>
		[PXBool]
		[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Selected")]
		public virtual bool? Selected
		{
			get
			{
				return _Selected;
			}
			set
			{
				_Selected = value;
			}
		}
		#endregion

		#region RefNbr
		public abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr>
		{
			public const int Length = 15;
		}

		[PXDBString(refNbr.Length, IsUnicode = true, IsKey = true, InputMask = "")]
		[PXDefault()]
		[PXUIField(DisplayName = "Reference Nbr.", Visibility = PXUIVisibility.SelectorVisible)]
		[PXSelector(typeof(PMChangeRequest.refNbr), DescriptionField = typeof(PMChangeRequest.description))]
		[AutoNumber(typeof(Search<PMSetup.changeRequestNumbering>), typeof(AccessInfo.businessDate))]
		public virtual String RefNbr
		{
			get;
			set;
		}
		#endregion
		#region ChangeOrderNbr
		public abstract class changeOrderNbr : PX.Data.BQL.BqlString.Field<changeOrderNbr>
		{			
		}

		[PXDBString(refNbr.Length, IsUnicode = true, InputMask = "")]
		[PXUIField(DisplayName = "Change Order Nbr.",Enabled = false)]
		[PXSelector(typeof(PMChangeOrder.refNbr), DescriptionField = typeof(PMChangeOrder.description), DirtyRead = true)]
		public virtual String ChangeOrderNbr
		{
			get;
			set;
		}
		#endregion
		#region CostChangeOrderNbr
		public abstract class costChangeOrderNbr : PX.Data.BQL.BqlString.Field<costChangeOrderNbr>
		{
		}

		[PXDBString(refNbr.Length, IsUnicode = true, InputMask = "")]
		[PXUIField(DisplayName = "Cost Change Order Nbr.", Enabled = false)]
		[PXSelector(typeof(PMChangeOrder.refNbr), DescriptionField = typeof(PMChangeOrder.description), DirtyRead = true)]
		public virtual String CostChangeOrderNbr
		{
			get;
			set;
		}
		#endregion
		#region ProjectNbr
		public abstract class projectNbr : PX.Data.BQL.BqlString.Field<projectNbr> { }
		[PXDBString(15, IsUnicode = true)]
		[PXUIField(DisplayName = "Change Nbr.", Visibility = PXUIVisibility.SelectorVisible)]
		public virtual String ProjectNbr
		{
			get;
			set;
		}
		#endregion
		#region Description
		public abstract class description : PX.Data.BQL.BqlString.Field<description> { }

		[PXDBString(PX.Objects.Common.Constants.TranDescLength, IsUnicode = true)]
		[PXDefault()]
		[PXUIField(DisplayName = "Description", Visibility = PXUIVisibility.SelectorVisible)]
		[PXFieldDescription]
		public virtual String Description
		{
			get;
			set;
		}
		#endregion
		#region Text
		public abstract class text : PX.Data.BQL.BqlString.Field<text> { }
		protected String _Text;
		[PXDBText(IsUnicode = true)]
		[PXUIField(DisplayName = "Details")]
		public virtual String Text
		{
			get
			{
				return this._Text;
			}
			set
			{
				this._Text = value;
				_plainText = null;
			}
		}
		#endregion
		#region DescriptionAsPlainText
		public abstract class descriptionAsPlainText : PX.Data.BQL.BqlString.Field<descriptionAsPlainText> { }

		private string _plainText = null;
		[PXString(IsUnicode = true)]
		[PXUIField(Visible = false)]
		public virtual String DescriptionAsPlainText
		{
			get
			{
				return _plainText ?? (_plainText = PX.Data.Search.SearchService.Html2PlainText(this.Text));
			}
		}
		#endregion
		#region Status
		public abstract class status : PX.Data.BQL.BqlString.Field<status> { }
		[PXDBString(1, IsFixed = true)]
		[ChangeRequestStatus.List()]
		[PXDefault(ChangeRequestStatus.OnHold)]
		[PXUIField(DisplayName = "Status", Required = true, Enabled = false, Visibility = PXUIVisibility.SelectorVisible)]
		public virtual String Status
		{
			get;
			set;
		}
		#endregion
		#region Hold
		public abstract class hold : PX.Data.BQL.BqlBool.Field<hold> { }
		[PXDBBool]
		[PXUIField(DisplayName = "Hold")]
		[PXDefault(true)]
		public virtual Boolean? Hold
		{
			get;
			set;
		}
		#endregion
		#region Approved
		public abstract class approved : PX.Data.BQL.BqlBool.Field<approved> { }
		[PXDBBool]
		[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual Boolean? Approved
		{
			get;
			set;
		}
		#endregion
		#region Rejected
		public abstract class rejected : PX.Data.BQL.BqlBool.Field<rejected> { }
		[PXDBBool]
		[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
		public bool? Rejected
		{
			get;
			set;
		}
		#endregion
		#region ProjectID
		public abstract class projectID : PX.Data.BQL.BqlInt.Field<projectID> { }
		[PXDefault]
		[PXForeignReference(typeof(Field<projectID>.IsRelatedTo<PMProject.contractID>))]
		[Project(typeof(Where<PMProject.changeOrderWorkflow, Equal<True>>), Visibility = PXUIVisibility.SelectorVisible)]
		public virtual Int32? ProjectID
		{
			get;
			set;
		}
		#endregion
		#region CustomerID
		public abstract class customerID : PX.Data.BQL.BqlInt.Field<customerID> { }

		[PXFormula(typeof(Selector<projectID, PMProject.customerID>))]
		[Customer(DescriptionField = typeof(Customer.acctName), Enabled = false)]
		public virtual Int32? CustomerID
		{
			get;
			set;
		}
		#endregion
		#region Date
		public abstract class date : PX.Data.BQL.BqlDateTime.Field<date> { }

		[PXDBDate()]
		[PXDefault(typeof(AccessInfo.businessDate))]
		[PXUIField(DisplayName = "Change Date", Visibility = PXUIVisibility.SelectorVisible)]
		public virtual DateTime? Date
		{
			get;
			set;
		}
		#endregion
		#region ExtRefNbr
		public abstract class extRefNbr : PX.Data.BQL.BqlString.Field<extRefNbr> { }
		protected String _ExtRefNbr;
		[PXDBString(30, IsUnicode = true)]
		[PXUIField(DisplayName = "Ext. Ref. Nbr.")]
		public virtual String ExtRefNbr
		{
			get
			{
				return this._ExtRefNbr;
			}
			set
			{
				this._ExtRefNbr = value;
			}
		}
		#endregion
		#region CostTotal
		public abstract class costTotal : PX.Data.BQL.BqlDecimal.Field<costTotal> { }
		[PXDBBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Cost Total")]
		public virtual Decimal? CostTotal
		{
			get;
			set;
		}
		#endregion
		#region LineTotal
		public abstract class lineTotal : PX.Data.BQL.BqlDecimal.Field<lineTotal> { }
		[PXDBBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Line Total")]
		public virtual Decimal? LineTotal
		{
			get;
			set;
		}
		#endregion
		#region MarkupTotal
		public abstract class markupTotal : PX.Data.BQL.BqlDecimal.Field<markupTotal> { }
		[PXDBBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Markup Total")]
		public virtual Decimal? MarkupTotal
		{
			get;
			set;
		}
		#endregion
		#region PriceTotal
		public abstract class priceTotal : PX.Data.BQL.BqlDecimal.Field<priceTotal> { }
		[PXDBBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Price Total")]
		[PXFormula(typeof(Add<lineTotal, markupTotal>))]
		public virtual Decimal? PriceTotal
		{
			get;
			set;
		}
		#endregion

		#region GrossMarginPct
		public abstract class grossMarginPct : PX.Data.BQL.BqlDecimal.Field<grossMarginPct> { }
		[PXBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Gross Margin %")]
		public virtual Decimal? GrossMarginPct
		{
			[PXDependsOnFields(typeof(priceTotal), typeof(costTotal))]
			get
			{
				if (PriceTotal != 0)
				{
					return 100 * (PriceTotal - CostTotal) / PriceTotal;
				}
				else
					return 0;
			}
		}
		#endregion


		#region WorkgroupID
		public abstract class workgroupID : PX.Data.BQL.BqlInt.Field<workgroupID> { }
		protected int? _WorkgroupID;

		/// <summary>
		/// The workgroup that is responsible for the document.
		/// </summary>
		/// <value>
		/// Corresponds to the <see cref="PX.TM.EPCompanyTree.WorkGroupID">EPCompanyTree.WorkGroupID</see> field.
		/// </value>
		[PXDBInt]
		[PXDefault(typeof(Customer.workgroupID), PersistingCheck = PXPersistingCheck.Nothing)]
		[PXCompanyTreeSelector]
		[PXUIField(DisplayName = "Workgroup", Visibility = PXUIVisibility.Visible)]
		public virtual int? WorkgroupID
		{
			get
			{
				return this._WorkgroupID;
			}
			set
			{
				this._WorkgroupID = value;
			}
		}
		#endregion
		#region OwnerID
		public abstract class ownerID : PX.Data.BQL.BqlInt.Field<ownerID> { }
		protected int? _OwnerID;

		/// <summary>
		/// The <see cref="Contact">Contact</see> responsible for the document.
		/// </summary>
		/// <value>
		/// Corresponds to the <see cref="Contact.ContactID"/> field.
		/// </value>
		[PXDefault(typeof(Customer.ownerID), PersistingCheck = PXPersistingCheck.Nothing)]
		[Owner(typeof(PMChangeRequest.workgroupID))]
		public virtual int? OwnerID
		{
			get
			{
				return this._OwnerID;
			}
			set
			{
				this._OwnerID = value;
			}
		}
		#endregion
		#region LineCntr
		public abstract class lineCntr : PX.Data.BQL.BqlInt.Field<lineCntr> { }
		[PXDBInt()]
		[PXDefault(0)]
		public virtual Int32? LineCntr
		{
			get;
			set;
		}
		#endregion
		#region MarkupLineCntr
		public abstract class markupLineCntr : PX.Data.BQL.BqlInt.Field<markupLineCntr> { }
		[PXDBInt()]
		[PXDefault(0)]
		public virtual Int32? MarkupLineCntr
		{
			get;
			set;
		}
		#endregion
		#region DelayDays
		public abstract class delayDays : PX.Data.BQL.BqlInt.Field<delayDays> { }
		[PXDBInt()]
		[PXUIField(DisplayName = "Contract Time Change, Days")]
		public virtual Int32? DelayDays
		{
			get;
			set;
		}
		#endregion
		#region Released
		public abstract class released : PX.Data.BQL.BqlBool.Field<released> { }
		protected Boolean? _Released;
		[PXDBBool()]
		[PXUIField(DisplayName = "Released")]
		[PXDefault(false)]
		public virtual Boolean? Released
		{
			get
			{
				return this._Released;
			}
			set
			{
				this._Released = value;
			}
		}
		#endregion

		#region FormCaptionDescription
		[PXString]
		[PXFormula(typeof(Selector<projectID, PMProject.description>))]
		public string FormCaptionDescription { get; set; }
		#endregion

		#region System Columns
		#region NoteID
		public abstract class noteID : PX.Data.BQL.BqlGuid.Field<noteID> { }
		protected Guid? _NoteID;
		[ChangeRequestSearchable]
		[PXNote(DescriptionField = typeof(PMChangeRequest.description))]
		public virtual Guid? NoteID
		{
			get
			{
				return this._NoteID;
			}
			set
			{
				this._NoteID = value;
			}
		}
		#endregion
		#region tstamp
		public abstract class Tstamp : PX.Data.BQL.BqlByteArray.Field<Tstamp> { }
		protected Byte[] _tstamp;
		[PXDBTimestamp()]
		public virtual Byte[] tstamp
		{
			get
			{
				return this._tstamp;
			}
			set
			{
				this._tstamp = value;
			}
		}
		#endregion
		#region CreatedByID
		public abstract class createdByID : PX.Data.BQL.BqlGuid.Field<createdByID> { }
		protected Guid? _CreatedByID;
		[PXDBCreatedByID]
		public virtual Guid? CreatedByID
		{
			get
			{
				return this._CreatedByID;
			}
			set
			{
				this._CreatedByID = value;
			}
		}
		#endregion
		#region CreatedByScreenID
		public abstract class createdByScreenID : PX.Data.BQL.BqlString.Field<createdByScreenID> { }
		protected String _CreatedByScreenID;
		[PXDBCreatedByScreenID()]
		public virtual String CreatedByScreenID
		{
			get
			{
				return this._CreatedByScreenID;
			}
			set
			{
				this._CreatedByScreenID = value;
			}
		}
		#endregion
		#region CreatedDateTime
		public abstract class createdDateTime : PX.Data.BQL.BqlDateTime.Field<createdDateTime> { }
		protected DateTime? _CreatedDateTime;
		[PXUIField(DisplayName = PXDBLastModifiedByIDAttribute.DisplayFieldNames.CreatedDateTime, Enabled = false, IsReadOnly = true)]
		[PXDBCreatedDateTime]
		public virtual DateTime? CreatedDateTime
		{
			get
			{
				return this._CreatedDateTime;
			}
			set
			{
				this._CreatedDateTime = value;
			}
		}
		#endregion
		#region LastModifiedByID
		public abstract class lastModifiedByID : PX.Data.BQL.BqlGuid.Field<lastModifiedByID> { }
		protected Guid? _LastModifiedByID;
		[PXDBLastModifiedByID]
		public virtual Guid? LastModifiedByID
		{
			get
			{
				return this._LastModifiedByID;
			}
			set
			{
				this._LastModifiedByID = value;
			}
		}
		#endregion
		#region LastModifiedByScreenID
		public abstract class lastModifiedByScreenID : PX.Data.BQL.BqlString.Field<lastModifiedByScreenID> { }
		protected String _LastModifiedByScreenID;
		[PXDBLastModifiedByScreenID()]
		public virtual String LastModifiedByScreenID
		{
			get
			{
				return this._LastModifiedByScreenID;
			}
			set
			{
				this._LastModifiedByScreenID = value;
			}
		}
		#endregion
		#region LastModifiedDateTime
		public abstract class lastModifiedDateTime : PX.Data.BQL.BqlDateTime.Field<lastModifiedDateTime> { }
		protected DateTime? _LastModifiedDateTime;
		[PXUIField(DisplayName = PXDBLastModifiedByIDAttribute.DisplayFieldNames.LastModifiedDateTime, Enabled = false, IsReadOnly = true)]
		[PXDBLastModifiedDateTime]
		public virtual DateTime? LastModifiedDateTime
		{
			get
			{
				return this._LastModifiedDateTime;
			}
			set
			{
				this._LastModifiedDateTime = value;
			}
		}
		#endregion
		#endregion

	}
}
