using PX.Data;

namespace PX.Objects.FS.DAC.ReportParameters
{
    public class FSStaffMemberReportParameters : PX.Data.IBqlTable
    {
        #region EmployeeID
        public abstract class employeeID : PX.Data.BQL.BqlInt.Field<employeeID> { }

        [PXInt]
        [FSSelector_StaffMember_ServiceOrderProjectID]
        [PXUIField(DisplayName = "Staff Member", TabOrder = 0)]
        public virtual int? EmployeeID { get; set; }
        #endregion
    }
}
