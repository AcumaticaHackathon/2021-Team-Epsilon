using PX.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.PM.DAC
{
	[PXCacheName(Messages.PMReportRowsMultiplier)]
	[Serializable()]
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	public partial class PMReportRowsMultiplier : IBqlTable
	{
		#region ID
		public abstract class id : PX.Data.BQL.BqlLong.Field<id>
		{
		}
		protected Int32? _RecordID;
		[PXUIField(DisplayName = "RecordID", Visible = false, Enabled = false)]
		[PXDBIdentity(IsKey = true)]
		public virtual Int32? RecordID
		{
			get
			{
				return this._RecordID;
			}
			set
			{
				this._RecordID = value;
			}
		}
		#endregion

		#region RowsCount
		public abstract class rowsCount : PX.Data.BQL.BqlInt.Field<rowsCount> { }
		protected Int32? _RowsCount;
		[PXDBInt]
		public virtual Int32? RowsCount
		{
			get
			{
				return this._RowsCount;
			}
			set
			{
				this._RowsCount = value;
			}
		}
		#endregion

		#region RowsCount
		public abstract class rowNumber : PX.Data.BQL.BqlInt.Field<rowNumber> { }
		protected Int32? _RowNumber;
		[PXDBInt]
		public virtual Int32? RowNumber
		{
			get
			{
				return this._RowNumber;
			}
			set
			{
				this._RowNumber = value;
			}
		}
		#endregion
	}
}
