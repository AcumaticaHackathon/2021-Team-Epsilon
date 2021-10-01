using System;
using System.Collections;
using PX.Common;
using PX.Data;
using PX.EP;
using PX.Objects.CR;
using PX.Objects.CR.MassProcess;
using PX.Objects.EP;
using PX.Objects.GL;
using PX.SM;
using PX.TM;
using SP.Objects.CR;

namespace SP.Objects.SP
{
    [DashboardType((int)DashboardTypeAttribute.Type.Default, TableAndChartDashboardTypeAttribute._AMCHARTS_DASHBOART_TYPE)]
    public class SPContactProductInquiry : PXGraph<SPContactProductInquiry>
    {
        #region Filter
        [System.SerializableAttribute()]
        public partial class BAFilter : IBqlTable
        {
            #region WorkgroupID
            public abstract class workgroupID : PX.Data.IBqlField { }

            [PXInt]
            [PXUIField(DisplayName = "Workgroup")]
            [PXCompanyTreeSelector]
            [PXMassUpdatableField]
            [PXMassMergableField]
            public virtual int? WorkgroupID { get; set; }
            #endregion
        }
        #endregion

        #region Select
        public PXFilter<BAFilter> Filter;
        [PXViewName(PX.Objects.CR.Messages.Contacts)]
        [PXFilterable]
        public PXSelectJoin<Contact,
            LeftJoin<Address, On<Address.addressID, Equal<Contact.defAddressID>>,
            LeftJoin<Users, On<Users.pKID, Equal<Contact.userID>>,
            LeftJoin<EPLoginType, On<EPLoginType.loginTypeID, Equal<Users.loginTypeID>>,
			LeftJoin<CRContactClass, On<CRContactClass.classID, Equal<Contact.classID>>>>>>,
            Where<Contact.contactType, Equal<ContactTypesAttribute.person>,
                And<Contact.bAccountID, Equal<Restriction.currentAccountID>,
                And2<Where<Contact.workgroupID, Equal<Current<BAFilter.workgroupID>>,
                Or<Current<BAFilter.workgroupID>, IsNull>>,
				And<Where<CRContactClass.isInternal, Equal<False>, Or<CRContactClass.isInternal, IsNull>>>>>>>
            FilteredItems;

        public PXSelectUsers<Contact, Where<Users.pKID, Equal<Current<Contact.userID>>>> User;

        #endregion

        #region Action
        public PXCancel<BAFilter> Cancel;

        public PXAction<BAFilter> viewDetails;
        [PXUIField(DisplayName = "Contact Details", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select, Visible = false)]
        [PXButton]
        public virtual IEnumerable ViewDetails(PXAdapter adapter)
        {
            if (this.FilteredItems.Current != null && this.Filter.Current != null)
            {
                Contact res = this.FilteredItems.Current;
                ProductContactMaint graph = PXGraph.CreateInstance<ProductContactMaint>();
                graph.Contact.Cache.Current = res;
                PXRedirectHelper.TryRedirect(graph, PXRedirectHelper.WindowMode.InlineWindow);
            }
            return Filter.Select();
        }

        public PXAction<BAFilter> addNew;
        [PXUIField(DisplayName = "Add New", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton]
        public virtual void AddNew()
        {
            ContactMaint graph = PXGraph.CreateInstance<ProductContactMaint>();
            Contact current = graph.Contact.Cache.Insert() as Contact;
            graph.Contact.Cache.Current = current;
            throw new PXRedirectRequiredException(graph, "Customer Details");
        }

        #endregion
    }
}
