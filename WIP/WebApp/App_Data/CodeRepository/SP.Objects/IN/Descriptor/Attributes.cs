using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.AR;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.IN;
using PX.Objects.IN.DAC;
using PX.Objects.SP.DAC;
using SP.Objects.IN.DAC;

namespace SP.Objects.IN.Descriptor
{
    public class Attributes
    {
        #region SiteRawAttribute

        [PXString(30, IsUnicode = true, InputMask = "")]
        [PXUIField(DisplayName = "Warehouse ID", Visibility = PXUIVisibility.SelectorVisible)]
        public sealed class SiteRawAttribute : AcctSubAttribute
        {
            public string DimensionName = "INSITE";

            public SiteRawAttribute()
                : base()
            {
                Type SearchType = typeof(Search<INSite.siteCD, Where<Match<Current<AccessInfo.userName>>>>);
                PXDimensionSelectorAttribute attr = new PXDimensionSelectorAttribute(DimensionName, SearchType,
                    typeof(INSite.siteCD));
                attr.CacheGlobal = true;
                _Attributes.Add(attr);
                _SelAttrIndex = _Attributes.Count - 1;
            }
        }

        #endregion

        public class InUnitPortalAttribute : INUnitAttribute
        {
            public InUnitPortalAttribute(Type InventoryType)
                : base(InventoryType)
            {
                var noDbStringAttribute = new PXStringAttribute(6)
                {
                    IsUnicode = true,
                    InputMask = ">aaaaaa"
                };
                this._Attributes[this._DBAttrIndex] = noDbStringAttribute;
            }
        }

        public class PortalSiteDBAvailAttribute : PortalSiteAvailAttribute
        {
            public PortalSiteDBAvailAttribute(Type InventoryType)
                : base(InventoryType)
            {
                var DbIntAttribute = new PXDBIntAttribute()
                {
                };

                for (int i = 0; i < _Attributes.Count; i++)
                {
                    if (_Attributes[i].GetType() == typeof(PXIntAttribute))
                        this._Attributes[i] = DbIntAttribute;
                }
            }
        }

        #region SiteAttribute
        [PXInt()]
        [PXUIField(DisplayName = "Warehouse", Visibility = PXUIVisibility.Visible,
            FieldClass = SiteAttribute.DimensionName)]
        [PXRestrictor(
            typeof(Where<INSite.active.IsEqual<True>.And<PortalSetup.IsCurrentPortal>>),
            "", typeof(INSite.siteCD))]
        public class PortalSiteAvailAttribute : PortalSiteAttribute
        {
            #region State

            protected Type _inventoryType;
            protected Type _subItemType;
            protected BqlCommand _Select;

            #endregion

            #region Ctor

			public PortalSiteAvailAttribute(Type InventoryType)
				: base()
			{
				_inventoryType = InventoryType;

				var search = BqlTemplate.OfCommand<
						SelectFrom<INSite>
						.LeftJoin<PortalSetup>
							.On<PortalSetup.IsCurrentPortal>
						.LeftJoin<INSiteStatus>
							.On<INSiteStatus.siteID.IsEqual<INSite.siteID>
							.And<INSiteStatus.inventoryID.IsEqual<BqlPlaceholder.A.AsField.AsOptional>>
							.And<INSiteStatus.subItemID.IsEqual<PortalSetup.defaultSubItemID>>>
						.InnerJoin<WarehouseReference>
							.On<WarehouseReference.siteID.IsEqual<INSite.siteID>
							.And<WarehouseReference.portalSetupID.IsEqual<PortalSetup.portalSetupID>>>
						.LeftJoin<Address>
							.On<Address.addressID.IsEqual<INSite.addressID>>
						.LeftJoin<Country>
							.On<Country.countryID.IsEqual<Address.countryID>>
						.LeftJoin<State>
							.On<State.countryID.IsEqual<Address.countryID>
							.And<State.stateID.IsEqual<Address.state>>>
						.LeftJoin<INItemSiteSettings>
							.On<INItemSiteSettings.siteID.IsEqual<INSite.siteID>
							.And<INItemSiteSettings.inventoryID.IsEqual<BqlPlaceholder.A.AsField.AsOptional>>>
						.Where<
							INSite.siteID.IsNotEqual<SiteAttribute.transitSiteID>
							.And<Match<AccessInfo.userName.FromCurrent>>>
						.SearchFor<INSite.siteID>>
					.Replace<BqlPlaceholder.A>(_inventoryType)
					.ToType();

				_Attributes[_SelAttrIndex] = new PXDimensionSelectorAttribute(DimensionName, search, typeof(INSite.siteCD))
				{
					//should ALWAYS be false, otherwise parameters will be ignored
					CacheGlobal = false,
					DescriptionField = typeof(INSite.descr)
				};

				_Select = BqlTemplate.OfCommand<
					SelectFrom<InventoryItem>
					.InnerJoin<INSite>
						.On<INSite.siteID.IsEqual<InventoryItem.dfltSiteID>>
					.Where<
						InventoryItem.inventoryID.IsEqual<BqlPlaceholder.A.AsField.FromCurrent>
						.And<INSite.siteID.IsNotEqual<SiteAttribute.transitSiteID>
						.And<Match<INSite, AccessInfo.userName.FromCurrent>>>>
					>
					.Replace<BqlPlaceholder.A>(_inventoryType)
					.ToCommand();
			}

            public PortalSiteAvailAttribute(Type InventoryType, Type SubItemType)
                : base()
            {
                _inventoryType = InventoryType;
                _subItemType = SubItemType;

                Type SearchType = BqlCommand.Compose(
                    typeof(Search2<,,>),
                    typeof(INSite.siteID),
                    typeof(LeftJoin<,,>),
                    typeof(INSiteStatus),
                    typeof(On<,,>),
                    typeof(INSiteStatus.siteID),
                    typeof(Equal<>),
                    typeof(INSite.siteID),
                    typeof(And<,,>),
                    typeof(INSiteStatus.inventoryID),
                    typeof(Equal<>),
                    typeof(Optional<>),
                    InventoryType,
                    typeof(And<,>),
                    typeof(INSiteStatus.subItemID),
                    typeof(Equal<>),
                    typeof(Optional<>),
                    SubItemType,

                    typeof(InnerJoin<,,>),
                    typeof(WarehouseReference),
                    typeof(On<,>),
                    typeof(WarehouseReference.siteID),
                    typeof(Equal<>),
                    typeof(INSite.siteID),

                    typeof(LeftJoin<,,>),
                    typeof(Address),
                    typeof(On<,>),
                    typeof(Address.addressID),
                    typeof(Equal<>),
                    typeof(INSite.addressID),

                    typeof(LeftJoin<,>),
                    typeof(INItemSiteSettings),
                    typeof(On<,,>),
                    typeof(INItemSiteSettings.siteID),
                    typeof(Equal<>),
                    typeof(INSite.siteID),
                    typeof(And<,>),
                    typeof(INItemSiteSettings.inventoryID),
                    typeof(Equal<>),
                    typeof(Optional<>),
                    InventoryType,

					typeof(Where<,,>),
					typeof(INSite.siteID),
					typeof(NotEqual<SiteAttribute.transitSiteID>),
					typeof(And<,,>),
					typeof(WarehouseReference.portalSetupID),
					typeof(Equal<>),
					typeof(PortalSetup.portalSiteID),
					typeof(And<>),
					typeof(Match<>),
					typeof(Current<AccessInfo.userName>)
					);
                PXDimensionSelectorAttribute attr = new PXDimensionSelectorAttribute(DimensionName, SearchType,
                    typeof(INSite.siteCD));

                //should ALWAYS be false, otherwise parameters will be ignored
                attr.CacheGlobal = false;
                attr.DescriptionField = typeof(INSite.descr);
                _Attributes[_SelAttrIndex] = attr;

                Type SelectType = BqlCommand.Compose(
                    typeof(Select2<,,>),
                    typeof(InventoryItem),
                    typeof(InnerJoin<INSite, On<INSite.siteID, Equal<InventoryItem.dfltSiteID>>>),
                    typeof(Where<,,>), typeof(InventoryItem.inventoryID), typeof(Equal<>), typeof(Current<>),
                    _inventoryType,
					typeof(And<INSite.siteID, NotEqual<SiteAttribute.transitSiteID>, And<Match<INSite, Current<AccessInfo.userName>>>>));

				_Select = BqlCommand.CreateInstance(SelectType);
            }

            public PortalSiteAvailAttribute(Type InventoryType, Type SubItemType, Type[] colsType)
                : base()
            {
                _inventoryType = InventoryType;
                _subItemType = SubItemType;

                Type SearchType = BqlCommand.Compose(
                    typeof(Search2<,,>),
                    typeof(INSite.siteID),
                    typeof(InnerJoin<,,>),
                    typeof(WarehouseReference),
                    typeof(On<,>),
                    typeof(WarehouseReference.siteID),
                    typeof(Equal<>),
                    typeof(INSite.siteID),
                    typeof(LeftJoin<,>),
                    typeof(INSiteStatus),
                    typeof(On<,,>),
                    typeof(INSiteStatus.siteID),
                    typeof(Equal<>),
                    typeof(INSite.siteID),
                    typeof(And<,,>),
                    typeof(INSiteStatus.inventoryID),
                    typeof(Equal<>),
                    typeof(Optional<>),
                    InventoryType,
                    typeof(And<,>),
                    typeof(INSiteStatus.subItemID),
                    typeof(Equal<>),
                    typeof(Optional<>),
                    SubItemType,
					typeof(Where<,,>),
					typeof(INSite.siteID),
					typeof(NotEqual<SiteAttribute.transitSiteID>),
                    typeof(And<,,>),
                    typeof(WarehouseReference.portalSetupID),
                    typeof(Equal<>),
                    typeof(PortalSetup.portalSiteID),
					typeof(And<>),
					typeof(Match<>),
					typeof(Current<AccessInfo.userName>)
					);
                PXDimensionSelectorAttribute attr = new PXDimensionSelectorAttribute(DimensionName, SearchType,
                    typeof(INSite.siteCD), colsType);
                //should ALWAYS be false, otherwise parameters will be ignored
                attr.CacheGlobal = false;
                attr.DescriptionField = typeof(INSite.descr);
                _Attributes[_SelAttrIndex] = attr;

                Type SelectType = BqlCommand.Compose(
                    typeof(Select2<,,>),
                    typeof(InventoryItem),
                    typeof(InnerJoin<INSite, On<INSite.siteID, Equal<InventoryItem.dfltSiteID>>>),
                    typeof(Where<,,>), typeof(InventoryItem.inventoryID), typeof(Equal<>), typeof(Current<>),
                    _inventoryType,
                    typeof(And<INSite.siteID, NotEqual<SiteAttribute.transitSiteID>, And<Match <INSite, Current<AccessInfo.userName>>>>));

                _Select = BqlCommand.CreateInstance(SelectType);
            }
            #endregion

            #region Initialization

            public override void CacheAttached(PXCache sender)
            {
                base.CacheAttached(sender);

                sender.Graph.FieldUpdated.AddHandler(sender.GetItemType(), _inventoryType.Name, InventoryID_FieldUpdated);
                sender.Graph.FieldDefaulting.AddHandler(sender.GetItemType(), typeof(InventoryLines.siteID).Name, SiteID_FieldDefaulting);
                sender.Graph.FieldUpdated.AddHandler(sender.GetItemType(), typeof(InventoryLines.siteID).Name, SiteID_FieldUpdated);
            }

            #endregion

            #region Implementation

            public virtual void InventoryID_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
            {
                try
                {
                    sender.SetDefaultExt(e.Row, _FieldName);
                }
                catch (PXUnitConversionException)
                {
                }
                catch (PXSetPropertyException)
                {
                    PXUIFieldAttribute.SetError(sender, e.Row, _FieldName, null);
                    sender.SetValue(e.Row, _FieldOrdinal, null);
                }
            }

            public virtual void SiteID_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
	        {
				InventoryLines row = e.Row as InventoryLines;
				if (row != null)
				{
					if (!PXAccess.FeatureInstalled<FeaturesSet.inventory>())
					{
						e.NewValue = null;
						e.Cancel = true;
						return;
					}
					var setting = PortalSetup.Current;
					if (setting != null)
					{
						if (row.StkItem == true)
							e.NewValue = setting.DefaultStockItemWareHouse;
						else
							e.NewValue = setting.DefaultNonStockItemWareHouse;
						if (e.NewValue == null)
						{
							Customer currentCustomer = ReadBAccount.ReadCurrentCustomer();
							Location currentdefLocation;
							using (new PXReadBranchRestrictedScope())
							{
								currentdefLocation = PXSelectJoin<Location,
									InnerJoin<INSite, On<Location.cSiteID, Equal<INSite.siteID>>,
									InnerJoin<WarehouseReference, On<WarehouseReference.siteID, Equal<INSite.siteID>>>>,
										Where<Location.locationID, Equal<Required<Location.locationID>>>>
									.SelectWindowed(sender.Graph, 0, 1, currentCustomer.DefLocationID);
							}
							if (currentdefLocation != null)
							{
								e.NewValue = currentdefLocation.CSiteID;
							}
							if (e.NewValue == null)
							{
								INSite site;
								using (new PXReadBranchRestrictedScope())
								{
									site = PXSelectJoin<INSite,
										 InnerJoin<WarehouseReference, On<WarehouseReference.siteID, Equal<INSite.siteID>>>,
										 Where<INSite.siteID, Equal<Required<INSite.siteID>>,
											 And<WarehouseReference.portalSetupID, Equal<PortalSetup.portalSiteID>>>>
									.SelectSingleBound(sender.Graph, null, row.DfltSiteID);
								}
								if (site != null)
								{
									e.NewValue = site.SiteID;
								}
							}
							if (e.NewValue == null)
							{
								INSite ramdomInSite;
								using (new PXReadBranchRestrictedScope())
								{
									ramdomInSite = PXSelectJoin<INSite,
											InnerJoin<WarehouseReference, On<WarehouseReference.siteID, Equal<INSite.siteID>>>,
											Where<WarehouseReference.portalSetupID, Equal<PortalSetup.portalSiteID>,
												And<INSite.siteID, NotEqual<SiteAttribute.transitSiteID>,
													And<INSite.active, Equal<True>>>>>
									.SelectWindowed(sender.Graph, 0, 1);
								}
								if (ramdomInSite != null)
									e.NewValue = ramdomInSite.SiteID;
							}
						}
					}
					row.SiteID = (int?)e.NewValue;
					sender.SetDefaultExt<InventoryLines.qtyAvailDflt>(row);
				}
			}

            public virtual void SiteID_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
            {
                var row = e.Row as InventoryLines;
                if (row == null)
                    return;

                sender.SetDefaultExt<InventoryLines.qtyAvailDflt>(row);
            }
            #endregion
        }


        [PXInt()]
        [PXUIField(DisplayName = "Warehouse", Visibility = PXUIVisibility.Visible,
            FieldClass = SiteAttribute.DimensionName)]
        public class PortalSiteAttribute : AcctSubAttribute
        {
            public const string DimensionName = "INSITE";

            public class dimensionName : Constant<string>
            {
                public dimensionName()
                    : base(DimensionName)
                {
                    ;
                }
            }

            private Type _whereType;

            public PortalSiteAttribute()
                : this(typeof(Where<Match<Current<AccessInfo.userName>>>), false)
            {
            }

            public PortalSiteAttribute(Type WhereType)
                : this(WhereType, true)
            {
            }

            public PortalSiteAttribute(Type WhereType, bool validateAccess)
            {
                if (WhereType != null)
                {
                    _whereType = WhereType;

					Type SearchType = validateAccess
						? BqlCommand.Compose(
                            typeof(Search<,>),
                            typeof(INSite.siteID),
                            typeof(Where2<,>),
                            typeof(Match<>),
                            typeof(Current<AccessInfo.userName>),
							typeof(And<,,>),
							typeof(INSite.siteID),
							typeof(NotEqual<SiteAttribute.transitSiteID>),
							typeof(And<>),
							_whereType)
                        : BqlCommand.Compose(
                            typeof(Search<,>),
                            typeof(INSite.siteID),
							typeof(Where2<,>),
							typeof(Where<,>),
							typeof(INSite.siteID),
							typeof(NotEqual<SiteAttribute.transitSiteID>),
							typeof(And<>),
							_whereType);

                    PXDimensionSelectorAttribute attr;
                    _Attributes.Add(
                        attr = new PXDimensionSelectorAttribute(DimensionName, SearchType, typeof(INSite.siteCD)));
                    attr.CacheGlobal = true;
                    attr.DescriptionField = typeof(INSite.descr);
                    _SelAttrIndex = _Attributes.Count - 1;
                }
            }

            #region Implemetation

            public override void CacheAttached(PXCache sender)
            {
                if (!PXAccess.FeatureInstalled<FeaturesSet.warehouse>() && sender.Graph.GetType() != typeof(PXGraph))
                {
                    ((PXDimensionSelectorAttribute)this._Attributes[_Attributes.Count - 1]).ValidComboRequired = false;
                    sender.Graph.FieldDefaulting.AddHandler(sender.GetItemType(), _FieldName, Feature_FieldDefaulting);
                }

                base.CacheAttached(sender);
            }

            public virtual void Feature_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
            {
                if (!e.Cancel)
                {
                    if (this.Definitions.DefaultSiteID == null)
                    {
                        object newValue = INSite.Main;
                        sender.RaiseFieldUpdating(_FieldName, e.Row, ref newValue);
                        e.NewValue = newValue;
                    }
                    else
                    {
                        e.NewValue = this.Definitions.DefaultSiteID;
                    }

                    e.Cancel = true;
                }
            }

            #endregion

            #region Default SiteID
            protected virtual PortalDefinition Definitions
            {
                get
                {
                    PortalDefinition defs = PX.Common.PXContext.GetSlot<PortalDefinition>();
                    if (defs == null)
                    {
                        defs =
                            PX.Common.PXContext.SetSlot<PortalDefinition>(PXDatabase.GetSlot<PortalDefinition>("PortalSite.Definition2",
                                typeof(INSite)));
                    }
                    return defs;
                }
            }

            public class PortalDefinition : IPrefetchable
            {
                private int? _DefaultSiteID;
                public int? DefaultSiteID
                {
                    get { return _DefaultSiteID; }
                }

                public void Prefetch()
                {
                    using (PXDataRecord record = PXDatabase.SelectSingle<INSite>(
                        new PXDataField<INSite.siteID>(),
                        new PXDataFieldOrder<INSite.siteID>()))
                    {
                        _DefaultSiteID = null;
                        if (record != null)
                            _DefaultSiteID = record.GetInt32(0);
                    }
                }
            }
            #endregion
        }
        #endregion

        public class SOShipCompletePortal
        {
            public class ListAttribute : PXStringListAttribute
            {
                public ListAttribute()
                    : base(
                        new string[] { ShipComplete, CancelRemainder, BackOrderAllowed },
                        new string[] { Messages.ShipComplete, Messages.CancelRemainder, Messages.BackOrderAllowed, }) { ; }
            }

            public const string ShipComplete = "C";
            public const string BackOrderAllowed = "B";
            public const string CancelRemainder = "L";

            public class shipComplete : Constant<string>
            {
                public shipComplete() : base(ShipComplete) { ;}
            }

            public class backOrderAllowed : Constant<string>
            {
                public backOrderAllowed() : base(BackOrderAllowed) { ;}
            }

            public class cancelRemainder : Constant<string>
            {
                public cancelRemainder() : base(CancelRemainder) { ;}
            }
        }
    }
}
