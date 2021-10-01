using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PX.Common;
using PX.Data;
using PX.Objects.CR.MassProcess;
using PX.Objects.CS;
using PX.Objects.CR.DAC;

namespace PX.Objects.CR
{
	public class CRSetupMaint : PXGraph<CRSetupMaint>
	{
		public PXSave<CRSetup> Save;
		public PXCancel<CRSetup> Cancel;
		public PXSelect<CRSetup> CRSetupRecord;

        public CRNotificationSetupList<CRNotification> Notifications;
        public PXSelect<NotificationSetupRecipient,
            Where<NotificationSetupRecipient.setupID, Equal<Current<CRNotification.setupID>>>> Recipients;

        public PXSelect<CRCampaignType> CampaignType;

		[PXHidden]
		public PXSelect<CRValidationRules> ValidationRules;

		public PXSelect<LeadContactValidationRules, Where<LeadContactValidationRules.validationType, Equal<ValidationTypesAttribute.leadContact>>> LeadContactValidationRules;
		public PXSelect<LeadAccountValidationRules, Where<LeadAccountValidationRules.validationType, Equal<ValidationTypesAttribute.leadAccount>>> LeadAccountValidationRules;
		public PXSelect<AccountValidationRules, Where<AccountValidationRules.validationType, Equal<ValidationTypesAttribute.account>>> AccountValidationRules;


		public CRSetupMaint()
		{
			InitRulesHandlres(LeadContactValidationRules);
			InitRulesHandlres(LeadAccountValidationRules);
			InitRulesHandlres(AccountValidationRules);
		}

        #region CacheAttached
        [PXDBString(10)]
        [PXDefault]
        [CRMContactType.List]
        [PXUIField(DisplayName = "Contact Type")]
        [PXCheckUnique(typeof(NotificationSetupRecipient.contactID),
            Where = typeof(Where<NotificationSetupRecipient.setupID, Equal<Current<NotificationSetupRecipient.setupID>>>))]
        public virtual void NotificationSetupRecipient_ContactType_CacheAttached(PXCache sender)
        {
        }
        [PXDBInt]
        [PXUIField(DisplayName = "Contact ID")]
        [PXNotificationContactSelector(typeof(NotificationSetupRecipient.contactType))]
        public virtual void NotificationSetupRecipient_ContactID_CacheAttached(PXCache sender)
        {
        }

        #endregion

        #region Event Handlers

        protected virtual void _(Events.RowSelected<CRSetup> e)
		{
			bool multicurrencyFeatureInstalled = PXAccess.FeatureInstalled<FeaturesSet.multicurrency>();

			PXUIFieldAttribute.SetVisible<CRSetup.defaultCuryID>(e.Cache, null, multicurrencyFeatureInstalled);
			PXUIFieldAttribute.SetVisible<CRSetup.defaultRateTypeID>(e.Cache, null, multicurrencyFeatureInstalled);
			PXUIFieldAttribute.SetVisible<CRSetup.allowOverrideCury>(e.Cache, null, multicurrencyFeatureInstalled);
			PXUIFieldAttribute.SetVisible<CRSetup.allowOverrideRate>(e.Cache, null, multicurrencyFeatureInstalled);

			PXUIFieldAttribute.SetEnabled<CRSetup.validateAccountDuplicatesOnEntry>(e.Cache, e.Row,
				!AccountValidationRules
					.Select()
					.FirstTableItems
					.Any(_ => _.CreateOnEntry != CreateOnEntryAttribute.Allow));

			PXUIFieldAttribute.SetEnabled<CRSetup.validateContactDuplicatesOnEntry>(e.Cache, e.Row,
				!LeadAccountValidationRules
					.Select()
					.FirstTableItems
					.Any(_ => _.CreateOnEntry != CreateOnEntryAttribute.Allow)
				&& !LeadContactValidationRules
					.Select()
					.FirstTableItems
					.Any(_ => _.CreateOnEntry != CreateOnEntryAttribute.Allow));
		}

		private void InitRulesHandlres(PXSelectBase select)
		{
			Type cacheType = select.View.CacheGetItemType();
		    if (select.Name == AccountValidationRules.Name)
		    {
                this.FieldSelecting.AddHandler(cacheType, typeof(CRValidationRules.matchingField).Name, (sender, e) => CreateFieldStateForFieldName(e, typeof(Location)));
		    }
		    else
		    {
                this.FieldSelecting.AddHandler(cacheType, typeof(CRValidationRules.matchingField).Name, (sender, e) => CreateFieldStateForFieldName(e, typeof(Contact)));    
		    }
			
			this.RowInserted.AddHandler(cacheType, (sender, e) => UpdateGrammValidationDate());

			this.RowUpdated.AddHandler(cacheType, (sender, e) =>
			{
				if (IsSignificantlyChanged(sender, e.Row, e.OldRow))
					UpdateGrammValidationDate();
			});

			this.RowDeleted.AddHandler(cacheType, (sender, e) => UpdateGrammValidationDate());

			this.RowSelected.AddHandler(cacheType, (sender, e) =>
			{
				var row = e.Row as CRValidationRules;
				if (row == null)
					return;

				PXUIFieldAttribute.SetEnabled<CRValidationRules.scoreWeight>(sender, row, row.CreateOnEntry == CreateOnEntryAttribute.Allow);
			});
		}

		protected virtual void _(Events.RowPersisting<CRSetup> e)
		{
			CRSetup row = e.Row as CRSetup;
			if (row != null && row.GrammValidationDateTime == null)			
				row.GrammValidationDateTime = PXTimeZoneInfo.Now;
		}

		protected virtual bool IsSignificantlyChanged(PXCache sender, object row, object oldRow)
		{
			if (row == null || oldRow == null)
				return true;

			return !sender.ObjectsEqual<CRValidationRules.matchingField>(row, oldRow)
				|| !sender.ObjectsEqual<CRValidationRules.scoreWeight>(row, oldRow)
				|| !sender.ObjectsEqual<CRValidationRules.transformationRule>(row, oldRow);
		}

		private void UpdateGrammValidationDate()
		{			
			CRSetup record = PXCache<CRSetup>.CreateCopy(this.CRSetupRecord.Current) as CRSetup;
			record.GrammValidationDateTime = null;			
			CRSetupRecord.Update(record);
		}

		protected virtual void _(Events.FieldUpdated<CRSetup.leadValidationThreshold> e)
		{
			LeadContactValidationRules
				.Select()
				.FirstTableItems
				.Where(_ => _.CreateOnEntry != CreateOnEntryAttribute.Allow)
				.ForEach(rule =>
				{
					rule.ScoreWeight = e.NewValue as decimal?;

					LeadContactValidationRules.Update(rule);
				});
		}

		protected virtual void _(Events.FieldUpdated<CRSetup.leadToAccountValidationThreshold> e)
		{
			LeadAccountValidationRules
				.Select()
				.FirstTableItems
				.Where(_ => _.CreateOnEntry != CreateOnEntryAttribute.Allow)
				.ForEach(rule =>
				{
					rule.ScoreWeight = e.NewValue as decimal?;

					LeadAccountValidationRules.Update(rule);
				});
		}

		protected virtual void _(Events.FieldUpdated<CRSetup.accountValidationThreshold> e)
		{
			AccountValidationRules
				.Select()
				.FirstTableItems
				.Where(_ => _.CreateOnEntry != CreateOnEntryAttribute.Allow)
				.ForEach(rule =>
				{
					rule.ScoreWeight = e.NewValue as decimal?;

					AccountValidationRules.Update(rule);
				});
		}

		protected virtual void _(Events.RowUpdated<AccountValidationRules> e)
		{
			CRValidationRules row = e.Row as CRValidationRules;
			if (row == null)
				return;

			if (row.CreateOnEntry == e.OldRow.CreateOnEntry)
				return;

			ProcessBlockTypeChange<CRSetup.accountValidationThreshold>(row, typeof(CRSetup.validateAccountDuplicatesOnEntry));
		}

		protected virtual void _(Events.RowUpdated<LeadAccountValidationRules> e)
		{
			CRValidationRules row = e.Row as CRValidationRules;
			if (row == null)
				return;

			if (row.CreateOnEntry == e.OldRow.CreateOnEntry)
				return;

			ProcessBlockTypeChange<CRSetup.leadToAccountValidationThreshold>(row, typeof(CRSetup.validateContactDuplicatesOnEntry));
		}

		protected virtual void _(Events.RowUpdated<LeadContactValidationRules> e)
		{
			CRValidationRules row = e.Row as CRValidationRules;
			if (row == null)
				return;

			if (row.CreateOnEntry == e.OldRow.CreateOnEntry)
				return;

			ProcessBlockTypeChange<CRSetup.leadValidationThreshold>(row, typeof(CRSetup.validateContactDuplicatesOnEntry));
		}

		protected virtual void ProcessBlockTypeChange<TThreshold>(CRValidationRules row, params Type[] ValidateOnEntry)
			where TThreshold : IBqlField
		{
			if (row.CreateOnEntry != CreateOnEntryAttribute.Allow)
			{
				row.ScoreWeight = CRSetupRecord.Cache.GetValue<TThreshold>(this.CRSetupRecord.Current) as decimal? ?? row.ScoreWeight;

				foreach (Type type in ValidateOnEntry)
				{
					CRSetupRecord.Cache.SetValueExt(this.CRSetupRecord.Current, type.Name, true);
				}
			}
			else
			{
				this.Caches[row.GetType()].SetDefaultExt<CRValidationRules.scoreWeight>(row);
			}
		}


		#endregion

		private void CreateFieldStateForFieldName(PXFieldSelectingEventArgs e, Type type)
		{
			List<string> allowedValues = new List<string>();
			List<string> allowedLabels = new List<string>();

			Dictionary<string, string> fields = new Dictionary<string, string>();

			foreach (var field in PXCache.GetBqlTable(type)
						.GetProperties(BindingFlags.Instance | BindingFlags.Public)
						.SelectMany(p => p.GetCustomAttributes(true).Where(atr => atr is PXMassMergableFieldAttribute),(p, atr) => p))
			{
				PXFieldState fs = this.Caches[type].GetStateExt(null, field.Name) as PXFieldState;
				if (!fields.ContainsKey(field.Name))
					fields[field.Name] = fs != null ? fs.DisplayName : field.Name;
			}

            if (type == typeof(Location))
            {
                foreach (var field in PXCache.GetBqlTable(typeof(Contact))
                        .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                        .SelectMany(p => p.GetCustomAttributes(true).Where(atr => atr is PXMassMergableFieldAttribute), (p, atr) => p))
                {
                    PXFieldState fs = this.Caches[type].GetStateExt(null, field.Name) as PXFieldState;
                    if (!fields.ContainsKey(field.Name))
                        fields[field.Name] = fs != null ? fs.DisplayName : field.Name;
                }
            }

			foreach (var item in fields.OrderBy(i => i.Value))
			{
				allowedValues.Add(item.Key);
				allowedLabels.Add(item.Value);
			}

			e.ReturnState = PXStringState.CreateInstance(e.ReturnValue, 60, null, "FieldName", false, 1, null, allowedValues.ToArray(), allowedLabels.ToArray(), true, null);
		}
	}
}
