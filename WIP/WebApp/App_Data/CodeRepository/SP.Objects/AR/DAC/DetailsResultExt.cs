using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PX.Data;
using PX.Objects.AR;
using PX.Objects.GL;

namespace SP.Objects.AR.DAC
{
    public class DetailsResultExt : PXCacheExtension<ARStatementForCustomer.DetailsResult>
    {
        #region BranchID
        public abstract class branchID : PX.Data.IBqlField
        {
        }
        protected Int32? _BranchID;
        [PXDBInt(IsKey = true)]
        [PXDefault()]
        [PXSelector(typeof(Search<Branch.branchID>))]
        [PXUIField(DisplayName = "Branch", Visible = false)]
        public virtual Int32? BranchID
        {
            get
            {
                return this._BranchID;
            }
            set
            {
                this._BranchID = value;
            }
        }
        #endregion
    }
}
