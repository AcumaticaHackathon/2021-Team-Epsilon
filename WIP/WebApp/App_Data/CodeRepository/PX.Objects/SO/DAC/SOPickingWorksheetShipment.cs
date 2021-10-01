using System;
using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.IN;

namespace PX.Objects.SO
{
	[PXCacheName(Messages.SOPickingWorksheetShipment, PXDacType.Details)]
	[PXProjection(typeof(
		SelectFrom<SOPickingWorksheetShipment>.
		InnerJoin<SOShipment>.On<SOPickingWorksheetShipment.FK.Shipment>),
		persistent: new[] { typeof(SOPickingWorksheetShipment) })]
	public class SOPickingWorksheetShipment : IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<SOPickingWorksheetShipment>.By<worksheetNbr, shipmentNbr>
		{
			public static SOPickingWorksheetShipment Find(PXGraph graph, string worksheetNbr, string shipmentNbr)
				=> FindBy(graph, worksheetNbr, shipmentNbr);
		}

		public static class FK
		{
			public class Worksheet : SOPickingWorksheet.PK.ForeignKeyOf<SOPickingWorksheetShipment>.By<worksheetNbr> { }
			public class Shipment : SOShipment.PK.ForeignKeyOf<SOPickingWorksheetShipment>.By<shipmentNbr> { }
		}
		#endregion

		#region WorksheetNbr
		[PXDBString(15, IsUnicode = true, IsKey = true, InputMask = ">CCCCCCCCCCCCCCC")]
		[PXUIField(DisplayName = "Worksheet Nbr.", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
		[PXDBDefault(typeof(SOPickingWorksheet.worksheetNbr))]
		[PXParent(typeof(FK.Worksheet))]
		public virtual String WorksheetNbr { get; set; }
		public abstract class worksheetNbr : PX.Data.BQL.BqlString.Field<worksheetNbr> { }
		#endregion
		#region ShipmentNbr
		[PXDBString(15, IsUnicode = true, IsKey = true, InputMask = "")]
		[PXUIField(DisplayName = "Shipment Nbr.", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
		[PXSelector(typeof(SearchFor<SOShipment.shipmentNbr>))]
		[PXParent(typeof(FK.Shipment))]
		public virtual String ShipmentNbr { get; set; }
		public abstract class shipmentNbr : PX.Data.BQL.BqlString.Field<shipmentNbr> { }
		#endregion
		#region Unlinked
		[PXBool]
		[PXDBCalced(typeof(True.When<SOShipment.currentWorksheetNbr.IsNull.Or<SOShipment.currentWorksheetNbr.IsNotEqual<SOPickingWorksheetShipment.worksheetNbr>>>.Else<False>), typeof(bool))]
		[PXUIField(DisplayName = "Unlinked", Enabled = false)]
		public virtual Boolean? Unlinked { get; set; }
		public abstract class unlinked : PX.Data.BQL.BqlBool.Field<unlinked> { }
		#endregion

		#region SOShipment Fields
		#region Status
		[PXDBString(1, IsFixed = true, BqlTable = typeof(SOShipment))]
		[PXUIField(DisplayName = "Status", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
		[SOShipmentStatus.List]
		public virtual String Status { get; set; }
		public abstract class status : PX.Data.BQL.BqlString.Field<status> { }
		#endregion
		#region Picked
		[PXDBBool(BqlTable = typeof(SOShipment))]
		[PXUIField(DisplayName = "Picked", Enabled = false)]
		public virtual Boolean? Picked { get; set; }
		public abstract class picked : PX.Data.BQL.BqlBool.Field<picked> { }
		#endregion
		#region PickedViaWorksheet
		[PXDBBool(BqlTable = typeof(SOShipment))]
		[PXUIField(DisplayName = "Picked via Worksheet", Enabled = false)]
		public virtual Boolean? PickedViaWorksheet { get; set; }
		public abstract class pickedViaWorksheet : PX.Data.BQL.BqlBool.Field<pickedViaWorksheet> { }
		#endregion
		#region PickedQty
		[PXDBQuantity(BqlTable = typeof(SOShipment))]
		[PXUIField(DisplayName = "Picked Qty.", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
		public virtual Decimal? PickedQty { get; set; }
		public abstract class pickedQty : PX.Data.BQL.BqlDecimal.Field<pickedQty> { }
		#endregion
		#region PackedQty
		[PXDBQuantity(BqlTable = typeof(SOShipment))]
		[PXUIField(DisplayName = "Packed Qty.", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
		public virtual Decimal? PackedQty { get; set; }
		public abstract class packedQty : PX.Data.BQL.BqlDecimal.Field<packedQty> { }
		#endregion
		#region ShipmentQty
		[PXDBQuantity(BqlTable = typeof(SOShipment))]
		[PXUIField(DisplayName = "Shipped Quantity", Visibility = PXUIVisibility.SelectorVisible)]
		public virtual Decimal? ShipmentQty { get; set; }
		public abstract class shipmentQty : PX.Data.BQL.BqlDecimal.Field<shipmentQty> { }
		#endregion
		#region ShipmentWeight
		[PXDBDecimal(6, BqlTable = typeof(SOShipment))]
		[PXUIField(DisplayName = "Shipped Weight", Enabled = false)]
		public virtual Decimal? ShipmentWeight { get; set; }
		public abstract class shipmentWeight : PX.Data.BQL.BqlDecimal.Field<shipmentWeight> { }
		#endregion
		#region ShipmentVolume
		[PXDBDecimal(6, BqlTable = typeof(SOShipment))]
		[PXUIField(DisplayName = "Shipped Volume", Enabled = false)]
		public virtual Decimal? ShipmentVolume { get; set; }
		public abstract class shipmentVolume : PX.Data.BQL.BqlDecimal.Field<shipmentVolume> { }
		#endregion
		#endregion

		#region Audit Fields
		#region tstamp
		[PXDBTimestamp]
		public virtual Byte[] tstamp { get; set; }
		public abstract class Tstamp : PX.Data.BQL.BqlByteArray.Field<Tstamp> { }
		#endregion
		#region CreatedByID
		[PXDBCreatedByID]
		public virtual Guid? CreatedByID { get; set; }
		public abstract class createdByID : PX.Data.BQL.BqlGuid.Field<createdByID> { }
		#endregion
		#region CreatedByScreenID
		[PXDBCreatedByScreenID]
		[PXUIField(DisplayName = "Created At", Enabled = false, IsReadOnly = true)]
		public virtual String CreatedByScreenID { get; set; }
		public abstract class createdByScreenID : PX.Data.BQL.BqlString.Field<createdByScreenID> { }
		#endregion
		#region CreatedDateTime
		[PXDBCreatedDateTime]
		[PXUIField(DisplayName = PXDBLastModifiedByIDAttribute.DisplayFieldNames.CreatedDateTime, Enabled = false, IsReadOnly = true)]
		public virtual DateTime? CreatedDateTime { get; set; }
		public abstract class createdDateTime : PX.Data.BQL.BqlDateTime.Field<createdDateTime> { }
		#endregion
		#region LastModifiedByID
		[PXDBLastModifiedByID]
		[PXUIField(DisplayName = PXDBLastModifiedByIDAttribute.DisplayFieldNames.LastModifiedByID, Enabled = false, IsReadOnly = true)]
		public virtual Guid? LastModifiedByID { get; set; }
		public abstract class lastModifiedByID : PX.Data.BQL.BqlGuid.Field<lastModifiedByID> { }
		#endregion
		#region LastModifiedByScreenID
		[PXDBLastModifiedByScreenID]
		[PXUIField(DisplayName = "Last Modified At", Enabled = false, IsReadOnly = true)]
		public virtual String LastModifiedByScreenID { get; set; }
		public abstract class lastModifiedByScreenID : PX.Data.BQL.BqlString.Field<lastModifiedByScreenID> { }
		#endregion
		#region LastModifiedDateTime
		[PXDBLastModifiedDateTime]
		[PXUIField(DisplayName = PXDBLastModifiedByIDAttribute.DisplayFieldNames.LastModifiedDateTime, Enabled = false, IsReadOnly = true)]
		public virtual DateTime? LastModifiedDateTime { get; set; }
		public abstract class lastModifiedDateTime : PX.Data.BQL.BqlDateTime.Field<lastModifiedDateTime> { }
		#endregion
		#endregion
	}
}