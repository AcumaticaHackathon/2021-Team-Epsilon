using System;
using PX.Data;
using PX.Security;
using PX.SM;

namespace SP.Objects.SP
{
	public class SPSettings : PXGraph<SPSettings>
	{
		[InjectDependency]
		// ReSharper disable InconsistentNaming
		private IUserManagementService _userManagementService { get; set; }
		// ReSharper restore InconsistentNaming

		#region Selects
		public PXSelect<Users, 
			Where<Users.pKID, Equal<Current<AccessInfo.userID>>>> 
			CurrentUser;
		#endregion

		#region Actions
		public PXSave<Users> Save;

		public PXCancel<Users> Cancel;

		public PXAction<Users> ChangePassword;
		[PXUIField(DisplayName = PX.SM.Messages.ChangePassword, MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update, Visible = false)]
		[PXButton()]
		public virtual void changePassword()
		{
			Access.SetPassword(_userManagementService, true, CurrentUser.Current);
		}
		#endregion

		#region Event Handlers

		internal void Users_RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
		{
			bool isDirty = false;
			foreach (string field in sender.Fields)
			{
				if (field != "NewPassword" &&
						field != "OldPassword" &&
						field != "ConfirmPassword")
				{
					object value = sender.GetValue(e.Row, field);
					object oldValue = sender.GetValue(e.OldRow, field);
					if (!object.Equals(value, oldValue))
					{
						isDirty = true;
						break;
					}
				}
			}
			sender.IsDirty = isDirty;
		}

		public void Users_RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			var row = e.Row as Users;
			if (row == null) return;

			row.PasswordChangeOnNextLogin = false;
		}

		#endregion
	}
}
