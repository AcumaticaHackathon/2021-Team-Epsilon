using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI;
using PX.Data;
using PX.Objects.CR;
using PX.Objects.IN;
using PX.SM;

namespace SP.Objects
{
    [Serializable]
    public class UsrBAccountT : IBqlTable
    {
        #region BAccountID
        public abstract class bAccountID : IBqlField { }
        [PXDBInt(IsKey = true)]
        public virtual Int32? BAccountID { get; set; }
        #endregion

        #region ParentBAccountID
        public abstract class parentBAccountID : IBqlField { }
        [PXDBInt(IsKey = true)]
        public virtual Int32? ParentBAccountID { get; set; }
        #endregion
    }

    [Serializable]
    public class BAccountOLD : BAccount
    {
        public new abstract class bAccountID : IBqlField { }

        public new abstract class parentBAccountID : IBqlField { }
    }

    [SerializableAttribute()]
    public class ContactT : Contact
	{
        public new abstract class bAccountID : IBqlField { }

		public new abstract class userID : IBqlField { }
	}

    #region BAccountTreeUserDefinition
    public sealed class BAccountTreeUserDefinition : IPrefetchable
    {
        private const string _BACCOUNTT_SLOT_KEY_PREFIX = "UsrBAccountTTreeUser@";
        private List<int> Accounts = new List<int>();
        private List<int> AccountsOLD = new List<int>();

        public List<int> GetBAccountTree()
        {
            return PXDatabase.Provider.SchemaCache.GetTableNames().Contains("UsrBAccountT") ? Accounts : AccountsOLD;
        }

        public void Prefetch()
        {
            if (PXDatabase.Provider.SchemaCache.GetTableNames().Contains("UsrBAccountT"))
            {
                PXSelectBase<UsrBAccountT> select = new PXSelectReadonly2<UsrBAccountT,
                    InnerJoin<ContactT, On<UsrBAccountT.parentBAccountID, Equal<ContactT.bAccountID>>>,
                    Where<ContactT.userID, Equal<Required<ContactT.userID>>>>(new PXGraph());

                using (new PXFieldScope(select.View, typeof(UsrBAccountT.bAccountID), typeof(ContactT.contactID)))
                {
                    foreach (UsrBAccountT ret in select.Select(PXAccess.GetUserID()))
                    {
                        Accounts.Add((int)ret.BAccountID);
                    }
                }
            }

            else 
            {
                foreach (

                    ContactT contact in
                        PXSelect<ContactT, Where<ContactT.userID, Equal<Required<ContactT.userID>>>>.Select(
                            new PXGraph(), PXAccess.GetUserID()))
                {
                    if (contact.BAccountID != null)
                    {
                        AccountsOLD.Add((int) contact.BAccountID);
                        AccountsOLD = GetBaccountsbyBAccount((int) contact.BAccountID, new PXGraph(), AccountsOLD);
                    }
                }
            }
        }

        private List<int> GetBaccountsbyBAccount(int? _baccountId, PXGraph graph, List<int> res)
        {
            List<int> ret = new List<int>();
            ret.AddRange(res);

            if (_baccountId == null) return ret;

            foreach (
                PXResult<BAccountOLD> childbaccount in
                    PXSelect<BAccountOLD, Where<BAccountOLD.parentBAccountID, Equal<Required<BAccountOLD.parentBAccountID>>>>.Select(
                        graph, _baccountId))
            {
                BAccount b = childbaccount;
                if (!ret.Contains((int)b.BAccountID))
                    ret.Add((int)b.BAccountID);

                foreach (
                    BAccountOLD contactnextlevel in
                        PXSelect<BAccountOLD, Where<BAccountOLD.parentBAccountID, Equal<Required<BAccountOLD.parentBAccountID>>>>.Select(graph,
                            b.BAccountID))
                {
                    if (!ret.Contains((int)contactnextlevel.BAccountID))
                    {
                        ret.Add((int)contactnextlevel.BAccountID);
                        ret = GetBaccountsbyBAccount(contactnextlevel.BAccountID, graph, ret);
                    }
                }
            }
            return ret;
        }

        public static BAccountTreeUserDefinition GetFromSlot()
        {
            Type[] tables = new Type[] { typeof(BAccount) };
            string key = _BACCOUNTT_SLOT_KEY_PREFIX + PXAccess.GetUserID();
            var slot = PXDatabase.GetSlot<BAccountTreeUserDefinition>(key, tables);
            return slot;
        }
    }
    #endregion	        
}
