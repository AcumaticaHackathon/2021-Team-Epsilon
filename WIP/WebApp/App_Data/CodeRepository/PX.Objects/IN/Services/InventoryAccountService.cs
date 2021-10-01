using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PX.Data;
using PX.Objects.AP;
using PX.Objects.CS;
using PX.Objects.IN.Overrides.INDocumentRelease;

namespace PX.Objects.IN.Services
{
	public class InventoryAccountService : IInventoryAccountService
	{
		public virtual int? GetAcctID<Field>(PXGraph graph, string AcctDefault, InventoryItem item, INSite site, INPostClass postclass)
		   where Field : IBqlField
		{
            switch (AcctDefault)
            {
                case INAcctSubDefault.MaskItem:
                default:
                    {
                        PXCache cache = graph.Caches[typeof(InventoryItem)];
                        try
                        {
                            return (int)cache.GetValue<Field>(item);
                        }
                        catch (NullReferenceException)
                        {
                            object keyval = cache.GetStateExt<InventoryItem.inventoryCD>(item);
                            if (item.StkItem == true)
                            {
                                throw new PXMaskArgumentException(Messages.MaskItem, PXUIFieldAttribute.GetDisplayName<Field>(cache), keyval);
                            }
                            throw new PXMaskArgumentException(Messages.MaskItem, GetSubstFieldDesr<Field>(cache), keyval);
                        }
                    }
                case INAcctSubDefault.MaskSite:
                    {
                        PXCache cache = graph.Caches[typeof(INSite)];
                        try
                        {
                            return (int)cache.GetValue<Field>(site);
                        }
                        catch (NullReferenceException)
                        {
                            object keyval = cache.GetStateExt<INSite.siteCD>(site);
                            throw new PXMaskArgumentException(Messages.MaskSite, PXUIFieldAttribute.GetDisplayName<Field>(cache), keyval);
                        }
                    }
                case INAcctSubDefault.MaskClass:
                    {
                        PXCache cache = graph.Caches[typeof(INPostClass)];
                        try
                        {
                            return (int)cache.GetValue<Field>(postclass);
                        }
                        catch (NullReferenceException)
                        {
                            object keyval = cache.GetStateExt<INPostClass.postClassID>(postclass);
                            throw new PXMaskArgumentException(Messages.MaskClass, PXUIFieldAttribute.GetDisplayName<Field>(cache), keyval);
                        }
                    }
            }
        }

        public virtual string GetSubstFieldDesr<Field>(PXCache cache)
           where Field : IBqlField
        {
            if (typeof(Field) == typeof(INPostClass.invtAcctID))
            {
                return PXUIFieldAttribute.GetDisplayName<NonStockItem.invtAcctID>(cache);
            }
            if (typeof(Field) == typeof(INPostClass.cOGSAcctID))
            {
                return PXUIFieldAttribute.GetDisplayName<NonStockItem.cOGSAcctID>(cache);
            }
            return PXUIFieldAttribute.GetDisplayName<Field>(cache);
        }

        public virtual int? GetSubID<Field>(PXGraph graph, string AcctDefault, string SubMask, InventoryItem item, INSite site, INPostClass postclass, INTran tran)
           where Field : IBqlField
        {
            if (typeof(Field) == typeof(INPostClass.cOGSSubID) && tran != null && postclass.COGSSubFromSales == true)
            {
                PXCache cache = graph.Caches[typeof(INTran)];

                object tran_SubID = cache.GetValueExt<INTran.subID>(tran);
                object value = (tran_SubID is PXFieldState) ? ((PXFieldState)tran_SubID).Value : tran_SubID;

                cache.RaiseFieldUpdating<Field>(tran, ref value);
                return (int?)value;
            }
            else
            {
                int? item_SubID = null;
                int? site_SubID = null;
                int? class_SubID = null;

                if (typeof(Field) == typeof(INPostClass.cOGSSubID) && postclass.COGSSubFromSales == true)
                {
                    item_SubID = (int?)graph.Caches[typeof(InventoryItem)].GetValue<InventoryItem.salesSubID>(item);
                    site_SubID = (int?)graph.Caches[typeof(INSite)].GetValue<INSite.salesSubID>(site);
                    class_SubID = (int?)graph.Caches[typeof(INPostClass)].GetValue<INPostClass.salesSubID>(postclass);
                }
                else
                {
                    item_SubID = (int?)graph.Caches[typeof(InventoryItem)].GetValue<Field>(item);
                    site_SubID = (int?)graph.Caches[typeof(INSite)].GetValue<Field>(site);
                    class_SubID = (int?)graph.Caches[typeof(INPostClass)].GetValue<Field>(postclass);
                }

                object value = null;

                try
                {
                    if (item.StkItem == true && typeof(Field) == typeof(INPostClass.invtSubID))
                        value = SubAccountMaskAttribute.MakeSub<INPostClass.invtSubMask>(graph, SubMask, item.StkItem, new object[] { item_SubID, site_SubID, class_SubID }, new Type[] { typeof(InventoryItem.invtSubID), typeof(INSite.invtSubID), typeof(INPostClass.invtSubID) });
                    if (item.StkItem != true && typeof(Field) == typeof(INPostClass.invtSubID))
                        value = SubAccountMaskAttribute.MakeSub<INPostClass.invtSubMask>(graph, SubMask, item.StkItem, new object[] { item_SubID, site_SubID, class_SubID }, new Type[] { typeof(NonStockItem.invtSubID), typeof(INSite.invtSubID), typeof(INPostClass.invtSubID) });
                    if (item.StkItem == true && typeof(Field) == typeof(INPostClass.cOGSSubID))
                        value = SubAccountMaskAttribute.MakeSub<INPostClass.cOGSSubMask>(graph, SubMask, item.StkItem, new object[] { item_SubID, site_SubID, class_SubID }, new Type[] { typeof(InventoryItem.cOGSSubID), typeof(INSite.cOGSSubID), typeof(INPostClass.cOGSSubID) });
                    if (item.StkItem != true && typeof(Field) == typeof(INPostClass.cOGSSubID))
                        value = SubAccountMaskAttribute.MakeSub<INPostClass.cOGSSubMask>(graph, SubMask, item.StkItem, new object[] { item_SubID, site_SubID, class_SubID }, new Type[] { typeof(NonStockItem.cOGSSubID), typeof(INSite.cOGSSubID), typeof(INPostClass.cOGSSubID) });
                    if (typeof(Field) == typeof(INPostClass.salesSubID))
                        value = SubAccountMaskAttribute.MakeSub<INPostClass.salesSubMask>(graph, SubMask, new object[] { item_SubID, site_SubID, class_SubID }, new Type[] { typeof(InventoryItem.salesSubID), typeof(INSite.salesSubID), typeof(INPostClass.salesSubID) });
                    if (typeof(Field) == typeof(INPostClass.stdCstVarSubID))
                        value = SubAccountMaskAttribute.MakeSub<INPostClass.stdCstVarSubMask>(graph, SubMask, new object[] { item_SubID, site_SubID, class_SubID }, new Type[] { typeof(InventoryItem.stdCstVarSubID), typeof(INSite.stdCstVarSubID), typeof(INPostClass.stdCstVarSubID) });
                    if (typeof(Field) == typeof(INPostClass.stdCstRevSubID))
                        value = SubAccountMaskAttribute.MakeSub<INPostClass.stdCstRevSubMask>(graph, SubMask, new object[] { item_SubID, site_SubID, class_SubID }, new Type[] { typeof(InventoryItem.stdCstRevSubID), typeof(INSite.stdCstRevSubID), typeof(INPostClass.stdCstRevSubID) });
                    if (typeof(Field) == typeof(INPostClass.pOAccrualSubID))
                        throw new NotImplementedException();
                    if (typeof(Field) == typeof(INPostClass.pPVSubID))
                        value = SubAccountMaskAttribute.MakeSub<INPostClass.pPVSubMask>(graph, SubMask, new object[] { item_SubID, site_SubID, class_SubID }, new Type[] { typeof(InventoryItem.pPVSubID), typeof(INSite.pPVSubID), typeof(INPostClass.pPVSubID) });
                    if (typeof(Field) == typeof(INPostClass.lCVarianceSubID))
                        value = SubAccountMaskAttribute.MakeSub<INPostClass.lCVarianceSubMask>(graph, SubMask, new object[] { item_SubID, site_SubID, class_SubID }, new Type[] { typeof(InventoryItem.lCVarianceSubID), typeof(INSite.lCVarianceSubID), typeof(INPostClass.lCVarianceSubID) });
                }
                catch (PXMaskArgumentException ex)
                {
                    object keyval;
                    switch (ex.SourceIdx)
                    {
                        case 0:
                        default:
                            keyval = graph.Caches[typeof(InventoryItem)].GetStateExt<InventoryItem.inventoryCD>(item);
                            break;
                        case 1:
                            keyval = graph.Caches[typeof(INSite)].GetStateExt<INSite.siteCD>(site);
                            break;
                        case 2:
                            keyval = graph.Caches[typeof(INPostClass)].GetStateExt<INPostClass.postClassID>(postclass);
                            break;
                    }
                    throw new PXMaskArgumentException(ex, keyval);
                }

                switch (AcctDefault)
                {
                    case INAcctSubDefault.MaskItem:
                    default:
                        RaiseFieldUpdating<Field>(graph.Caches[typeof(InventoryItem)], item, ref value);
                        break;
                    case INAcctSubDefault.MaskSite:
                        RaiseFieldUpdating<Field>(graph.Caches[typeof(INSite)], site, ref value);
                        break;
                    case INAcctSubDefault.MaskClass:
                        RaiseFieldUpdating<Field>(graph.Caches[typeof(INPostClass)], postclass, ref value);
                        break;
                }
                return (int?)value;
            }
        }

        public static void RaiseFieldUpdating<Field>(PXCache cache, object item, ref object value)
           where Field : IBqlField
        {
            try
            {
                cache.RaiseFieldUpdating<Field>(item, ref value);
            }
            catch (PXSetPropertyException ex)
            {
                string fieldname = typeof(Field).Name;
                string itemname = PXUIFieldAttribute.GetItemName(cache);
                string dispname = PXUIFieldAttribute.GetDisplayName(cache, fieldname);
                string errortext = ex.Message;

                if (dispname != null && fieldname != dispname)
                {
                    int fid = errortext.IndexOf(fieldname, StringComparison.OrdinalIgnoreCase);
                    if (fid >= 0)
                    {
                        errortext = errortext.Remove(fid, fieldname.Length).Insert(fid, dispname);
                    }
                }
                else
                {
                    dispname = fieldname;
                }

                dispname = string.Format("{0} {1}", itemname, dispname);

                throw new PXSetPropertyException(ErrorMessages.ValueDoesntExist, dispname, value);
            }
        }

        public virtual int? GetPOAccrualAcctID<Field>(PXGraph graph, string AcctDefault, InventoryItem item, INSite site, INPostClass postclass, Vendor vendor)
            where Field : IBqlField
        {
            switch (AcctDefault)
            {
                case INAcctSubDefault.MaskItem:
                default:
                    {
                        PXCache cache = graph.Caches[typeof(InventoryItem)];
                        try
                        {
                            return (int?)cache.GetValue<Field>(item);
                        }
                        catch (NullReferenceException)
                        {
                            object keyval = cache.GetStateExt<InventoryItem.inventoryCD>(item);
                            throw new PXMaskArgumentException(Messages.MaskItem, PXUIFieldAttribute.GetDisplayName<Field>(cache), keyval);
                        }
                    }
                case INAcctSubDefault.MaskSite:
                    {
                        PXCache cache = graph.Caches[typeof(INSite)];
                        try
                        {
                            return (int?)cache.GetValue<Field>(site);
                        }
                        catch (NullReferenceException)
                        {
                            object keyval = cache.GetStateExt<INSite.siteCD>(site);
                            throw new PXMaskArgumentException(Messages.MaskSite, PXUIFieldAttribute.GetDisplayName<Field>(cache), keyval);
                        }
                    }
                case INAcctSubDefault.MaskClass:
                    {
                        PXCache cache = graph.Caches[typeof(INPostClass)];
                        try
                        {
                            return (int?)cache.GetValue<Field>(postclass);
                        }
                        catch (NullReferenceException)
                        {
                            object keyval = cache.GetStateExt<INPostClass.postClassID>(postclass);
                            throw new PXMaskArgumentException(Messages.MaskClass, PXUIFieldAttribute.GetDisplayName<Field>(cache), keyval);
                        }
                    }
                case INAcctSubDefault.MaskVendor:
                    {
                        PXCache cache = graph.Caches[typeof(Vendor)];
                        try
                        {
                            return (int?)cache.GetValue<Field>(vendor);
                        }
                        catch (NullReferenceException)
                        {
                            object keyval = cache.GetStateExt<Vendor.bAccountID>(vendor);
                            throw new PXMaskArgumentException(Messages.MaskVendor, PXUIFieldAttribute.GetDisplayName<Field>(cache), keyval);
                        }
                    }
            }
        }

        public virtual int? GetPOAccrualSubID<Field>(PXGraph graph, string AcctDefault, string SubMask, InventoryItem item, INSite site, INPostClass postclass, Vendor vendor)
            where Field : IBqlField
        {
            int? item_SubID = (int?)graph.Caches[typeof(InventoryItem)].GetValue<Field>(item);
            int? site_SubID = (int?)graph.Caches[typeof(INSite)].GetValue<Field>(site);
            int? class_SubID = (int?)graph.Caches[typeof(INPostClass)].GetValue<Field>(postclass);
            int? vendor_SubID = (int?)graph.Caches[typeof(Vendor)].GetValue<Field>(vendor);

            object value = null;

            try
            {
                value = POAccrualSubAccountMaskAttribute.MakeSub<INPostClass.pOAccrualSubMask>(graph, SubMask, new object[] { item_SubID, site_SubID, class_SubID, vendor_SubID }, new Type[] { typeof(InventoryItem.pOAccrualSubID), typeof(INSite.pOAccrualSubID), typeof(INPostClass.pOAccrualSubID), typeof(Vendor.pOAccrualSubID) });
            }
            catch (PXMaskArgumentException ex)
            {
                object keyval;
                switch (ex.SourceIdx)
                {
                    case 0:
                    default:
                        keyval = graph.Caches[typeof(InventoryItem)].GetStateExt<InventoryItem.inventoryCD>(item);
                        break;
                    case 1:
                        keyval = graph.Caches[typeof(INSite)].GetStateExt<INSite.siteCD>(site);
                        break;
                    case 2:
                        keyval = graph.Caches[typeof(INPostClass)].GetStateExt<INPostClass.postClassID>(postclass);
                        break;
                    case 3:
                        keyval = graph.Caches[typeof(Vendor)].GetStateExt<Vendor.bAccountID>(vendor);
                        break;
                }
                throw new PXMaskArgumentException(ex, keyval);
            }

            switch (AcctDefault)
            {
                case INAcctSubDefault.MaskItem:
                default:
                    RaiseFieldUpdating<Field>(graph.Caches[typeof(InventoryItem)], item, ref value);
                    break;
                case INAcctSubDefault.MaskSite:
                    RaiseFieldUpdating<Field>(graph.Caches[typeof(INSite)], site, ref value);
                    break;
                case INAcctSubDefault.MaskClass:
                    RaiseFieldUpdating<Field>(graph.Caches[typeof(INPostClass)], postclass, ref value);
                    break;
                case INAcctSubDefault.MaskVendor:
                    RaiseFieldUpdating<Field>(graph.Caches[typeof(Vendor)], vendor, ref value);
                    break;
            }
            return (int?)value;
        }
    }
}
