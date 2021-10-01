using PX.Common;
using PX.Data;
using PX.Objects.CR;
using PX.Objects.EP;
using PX.Objects.SP.DAC;
using PX.SM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SP.Objects.CR
{
    public class UserProfileContactMaint : ContactMaint
    {
    }

    public class UserProfileContactMaintExt : PXGraphExtension<UserProfileContactMaint>
    {
        public PXSetup<Address> dummy;

        [PXViewName(ViewNamesMessage.UserPrefs)]
        public PXSelect<UserPreferences, Where<UserPreferences.userID, Equal<Current<Contact.userID>>>> UserPrefs;

        public override void Initialize()
        {
            if (PortalSetup.Current == null)
                throw new PXSetupNotEnteredException<PortalSetup>(ErrorMessages.SetupNotEntered);

            var actions = new string[] { "Save", "Cancel", "ResetPassword", "ActivateLogin", "EnableLogin", "DisableLogin", "UnlockLogin" };
            foreach (string action in Base.Actions.Keys)
            {
                if (actions.Contains(action))
                    continue;
                Base.Actions[action].SetVisible(false);
            }

            //There is bug - on Cancel Contact.Current became null. Now desided to hide this button. Needs to fix in future.
            Base.Actions["Cancel"].SetVisible(false);

            Base.UnattendedMode = true;
            if (Base.Contact.Cache.Current != null)
                PXUIFieldAttribute.SetEnabled<Contact.isActive>(Base.Caches[typeof(Contact)], Base.Contact.Cache.Current, false);

            Base.Actions["ResetPassword"].SetEnabled(true);
            Base.Actions["ActivateLogin"].SetEnabled(false);
            Base.Actions["EnableLogin"].SetEnabled(false);
            Base.Actions["DisableLogin"].SetEnabled(false);
            Base.Actions["UnlockLogin"].SetEnabled(false);

            PXUIFieldAttribute.SetEnabled<Users.loginTypeID>(Base.Caches[typeof(Users)].Graph.Caches[typeof(Users)], Base.User.Current, false);
            PXUIFieldAttribute.SetEnabled<Users.state>(Base.Caches[typeof(Users)], Base.User.Current, false);
            PXUIFieldAttribute.SetEnabled<Users.username>(Base.Caches[typeof(Users)], Base.User.Current, false);
            PXUIFieldAttribute.SetEnabled<Users.password>(Base.Caches[typeof(Users)], Base.User.Current, true);
            PXUIFieldAttribute.SetEnabled<Users.generatePassword>(Base.Caches[typeof(Users)], Base.User.Current, true);
        }

        public virtual System.Collections.IEnumerable contact()
        {
            var Res = new List<Contact>();
            foreach (var contact in PXSelect<Contact,
                Where<Contact.contactID, Equal<Required<Contact.contactID>>>>.Select(Base,
                Base.Contact.Current != null ? Base.Contact.Current.ContactID : ReadBAccount.ReadCurrentContact().ContactID))
            {
                Res.Add(contact);
            }
            foreach (var contact in Res)
            {
                yield return contact;
            }
        }

        #region Event Handlers
        protected virtual void UserPreferences_RowInserting(PXCache sender, PXRowInsertingEventArgs e, PXRowInserting ins)
        {
            var row = e.Row as UserPreferences;
            if (row == null) return;

            if (ins != null)
                ins(sender, e);

            if (((Contact)Base.Caches<Contact>().Current).UserID != null)
                row.UserID = ((Contact)Base.Caches<Contact>().Current).UserID;
            else
                e.Cancel = true;
        }

        internal void UserPreferences_RowPersisted(PXCache sender, PXRowPersistedEventArgs e)
        {
            PXTimeZoneInfo newTimeZone = PXTimeZoneInfo.FindSystemTimeZoneById(((UserPreferences)e.Row).TimeZone) ??
                                         PXTimeZoneInfo.FindSystemTimeZoneById(GetUserTimeZoneId());
            LocaleInfo.SetTimeZone(newTimeZone);

			if (e.TranStatus == PXTranStatus.Completed)
			{
				if (System.Web.HttpContext.Current != null &&
					this.UserPrefs.Current?.UserID == PXAccess.GetUserID())
				{
					// do not throw an exception to prevent breaking of the event-handling chain
					Redirector.Refresh(System.Web.HttpContext.Current);
				}
			}
        }

        protected virtual void Contact_RowSelected(PXCache sender, PXRowSelectedEventArgs e, PXRowSelected sel)
        {
            var row = e.Row as Contact;
            if (row == null) return;

            if (sel != null)
                sel(sender, e);

            PXUIFieldAttribute.SetEnabled<UserPreferences.timeZone>(Base.Caches[typeof(UserPreferences)], UserPrefs.Current, ((Contact)Base.Caches<Contact>().Current).UserID != null);
        }

        protected virtual void Users_RowSelected(PXCache sender, PXRowSelectedEventArgs e, PXRowSelected sel)
        {
            Base.Actions["ResetPassword"].SetEnabled(true);
            Base.Actions["ActivateLogin"].SetEnabled(false);
            Base.Actions["EnableLogin"].SetEnabled(false);
            Base.Actions["DisableLogin"].SetEnabled(false);
            Base.Actions["UnlockLogin"].SetEnabled(false);

            PXUIFieldAttribute.SetEnabled<Users.loginTypeID>(sender.Graph.Caches[typeof(Users)], Base.User.Current, false);
            PXUIFieldAttribute.SetEnabled<Users.state>(Base.Caches[typeof(Users)], Base.User.Current, false);
            PXUIFieldAttribute.SetEnabled<Users.username>(Base.Caches[typeof(Users)], Base.User.Current, false);
            PXUIFieldAttribute.SetEnabled<Users.password>(Base.Caches[typeof(Users)], Base.User.Current, true);
            PXUIFieldAttribute.SetEnabled<Users.generatePassword>(Base.Caches[typeof(Users)], Base.User.Current, true);
        }
        #endregion

        [PXOverride]
        public void Persist(Action method)
        {
            bool needInvalidate = false;
            if (UserPrefs.Current != null)
            {
                var prevTimeZone = (string)UserPrefs.Cache.GetValueOriginal<UserPreferences.timeZone>(UserPrefs.Current);
                if (prevTimeZone != UserPrefs.Current.TimeZone)
                {
                    needInvalidate = true;
                }
            }
            method?.Invoke();

            if (needInvalidate)
            {
                throw new PXRefreshException();
            }
        }

        protected string GetUserTimeZoneId()
        {
            if (UserPrefs.Current != null)
            {
                var search = PXSelect<Users>.Search<Users.pKID>(Base, UserPrefs.Current.UserID);
                if (search != null && search.Count > 0)
                {
                    var username = ((Users)search[0][typeof(Users)]).Username;
                    return GetUserTimeZoneId(username);
                }
            }
            return null;
        }

        public virtual string GetUserTimeZoneId(string username)
        {
            var set = PXSelectJoin<UserPreferences,
                InnerJoin<Users, On<Users.pKID, Equal<UserPreferences.userID>>>,
                Where<Users.username, Equal<Required<Users.username>>>>.
                Select(Base, username);
            return set != null && set.Count > 0
                ? ((UserPreferences)set[0][typeof(UserPreferences)]).TimeZone
                : null;
        }
    }
}