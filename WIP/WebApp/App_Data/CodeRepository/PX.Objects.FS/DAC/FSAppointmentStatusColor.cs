using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;
using System;

namespace PX.Objects.FS
{
    [Serializable]
    [PXCacheName(TX.TableName.FSAppointmentStatusColor)]
    //[PXPrimaryGraph(typeof(AppointmentStatusColorMaint))]
    public class FSAppointmentStatusColor : IBqlTable
    {
        #region Keys
        public class PK : PrimaryKeyOf<FSAppointmentStatusColor>.By<statusID>
        {
            public static FSAppointmentStatusColor Find(PXGraph graph, string statusID) => FindBy(graph, statusID);
        }
        #endregion

        #region StatusID
        [PXDBString(1, IsKey = true, IsFixed = true, InputMask = "")]
        [PXUIField(DisplayName = "ID")]
        public virtual string StatusID { get; set; }
        public abstract class statusID : PX.Data.BQL.BqlString.Field<statusID> { }
        #endregion
        #region StatusLabel
        [PXDBString(60, IsUnicode = true, InputMask = "")]
        [PXDefault]
        [PXUIField(DisplayName = "Status")]
        public virtual string StatusLabel { get; set; }
        public abstract class statusLabel : PX.Data.BQL.BqlString.Field<statusLabel> { }
        #endregion
        #region IsVisible
        [PXDBBool()]
        [PXDefault(true)]
        [PXUIField(DisplayName = "Visible")]
        public virtual bool? IsVisible { get; set; }
        public abstract class isVisible : PX.Data.BQL.BqlBool.Field<isVisible> { }
        #endregion
        #region BackgroundColor
        public abstract class backgroundColor : PX.Data.BQL.BqlString.Field<backgroundColor> { }

        [PXDBString(7, IsUnicode = true, InputMask = "C<AAAAAA")]
        [PXDefault("#000000")]
        [PXUIField(DisplayName = "Background Color")]
        public virtual string BackgroundColor { get; set; }
        #endregion
        #region TextColor
        public abstract class textColor : PX.Data.BQL.BqlString.Field<textColor> { }

        [PXDBString(7, IsUnicode = true, InputMask = "C<AAAAAA")]
        [PXDefault("#000000")]
        [PXUIField(DisplayName = "Text Color")]
        public virtual string TextColor { get; set; }
        #endregion
        #region SystemRecord
        [PXDBBool()]
        [PXDefault(false)]
        [PXUIField(DisplayName = "System Record")]
        public virtual bool? SystemRecord { get; set; }
        public abstract class systemRecord : PX.Data.BQL.BqlBool.Field<systemRecord> { }
        #endregion

        #region CreatedByID
        [PXDBCreatedByID()]
        public virtual Guid? CreatedByID { get; set; }
        public abstract class createdByID : PX.Data.BQL.BqlGuid.Field<createdByID> { }
        #endregion
        #region CreatedByScreenID
        [PXDBCreatedByScreenID()]
        public virtual string CreatedByScreenID { get; set; }
        public abstract class createdByScreenID : PX.Data.BQL.BqlString.Field<createdByScreenID> { }
        #endregion
        #region CreatedDateTime
        [PXDBCreatedDateTime()]
        public virtual DateTime? CreatedDateTime { get; set; }
        public abstract class createdDateTime : PX.Data.BQL.BqlDateTime.Field<createdDateTime> { }
        #endregion
        #region LastModifiedByID
        [PXDBLastModifiedByID()]
        public virtual Guid? LastModifiedByID { get; set; }
        public abstract class lastModifiedByID : PX.Data.BQL.BqlGuid.Field<lastModifiedByID> { }
        #endregion
        #region LastModifiedByScreenID
        [PXDBLastModifiedByScreenID()]
        public virtual string LastModifiedByScreenID { get; set; }
        public abstract class lastModifiedByScreenID : PX.Data.BQL.BqlString.Field<lastModifiedByScreenID> { }
        #endregion
        #region LastModifiedDateTime
        [PXDBLastModifiedDateTime()]
        public virtual DateTime? LastModifiedDateTime { get; set; }
        public abstract class lastModifiedDateTime : PX.Data.BQL.BqlDateTime.Field<lastModifiedDateTime> { }
        #endregion
        #region Tstamp
        [PXDBTimestamp()]
        [PXUIField(DisplayName = "Tstamp")]
        public virtual byte[] Tstamp { get; set; }
        public abstract class tstamp : PX.Data.BQL.BqlByteArray.Field<tstamp> { }
        #endregion
    }
}
