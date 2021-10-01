using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PX.Data;
using PX.Objects.IN;
using PX.TM;

namespace SP.Objects.SP
{
    public class SPInventoryInquiry : PXGraph<SPInventoryInquiry>
    {
        #region Filter
        [System.SerializableAttribute()]
        public partial class InventoryFilter : IBqlTable
        {
            #region Find Item
            public abstract class findItem : PX.Data.IBqlField
            {
            }
            protected String _FindItem;
            [PXString()]
            [PXUIField(DisplayName = "Find Item")]
            public virtual String FindItem
            {
                get { return this._FindItem; }
                set { _FindItem = value; }
            }
            #endregion
        }
        #endregion

        #region Select
        public PXFilter<InventoryFilter> Filter;

        public PXSelect<InventoryItem,
            Where<InventoryItem.inventoryCD, Like<Current<InventoryFilter.findItem>>,
                Or<Current<InventoryFilter.findItem>, IsNull>>>
            FilteredItems;

        /*public PXSelect<InventoryItem>
            FilteredItems;*/
        #endregion
    }
}
