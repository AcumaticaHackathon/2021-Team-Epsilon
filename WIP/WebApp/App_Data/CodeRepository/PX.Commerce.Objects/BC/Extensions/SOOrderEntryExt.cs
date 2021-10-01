using System;
using System.Collections.Generic;
using System.Linq;
using PX.Commerce.Core;
using PX.Data;
using PX.Objects.SO;
using PX.Common;
using PX.Data.WorkflowAPI;
using PX.Objects.SO.GraphExtensions.SOOrderEntryExt;
using PX.Objects.SO.Workflow.SalesOrder;

namespace PX.Commerce.Objects
{
	using static BoundedTo<SOOrderEntry, SOOrder>;
	using static PX.Commerce.Objects.BCSOOrderExt;
	using static SOOrder;
	public class BCSOOrderEntryExt : PXGraphExtension<SOOrderEntry>
	{
		public static bool IsActive() { return CommerceFeaturesHelper.CommerceEdition; }

		public PXSelect<SOOrderRisks, Where<SOOrderRisks.orderNbr, Equal<Current<SOOrder.orderNbr>>, And<SOOrderRisks.orderType, Equal<Current<SOOrder.orderType>>>>,
			OrderBy<Asc<SOOrderRisks.lineNbr>>> OrderRisks;

		public PXWorkflowEventHandler<SOOrder> OnRiskHoldConditionSatisfied;

		public class SOOrderEntry_RiskWorkflow : PXGraphExtension<SOOrderEntry_ApprovalWorkflow, SOOrderEntry_Workflow, SOOrderEntry>
		{
			public static bool IsActive() { return CommerceFeaturesHelper.CommerceEdition; }

			public override void Configure(PXScreenConfiguration config)
			{
				if (!IsActive()) return;

				var context = config.GetScreenConfigurationContext<SOOrderEntry, SOOrder>();
				Condition Bql<T>() where T : IBqlUnary, new() => context.Conditions.FromBql<T>();

				var conditions = new
				{
					IsOnRiskHold
						= Bql<riskHold.IsEqual<True>>(),

					IsNotOnRiskHoldAndNotApproved
						= Bql<riskHold.IsEqual<False>.And<approved.IsEqual<False>>>(),

					IsNotOnRiskHold
						= Bql<riskHold.IsEqual<False>>(),

					IsNotOnRiskAndHasPaymentsInPendingProcessing
						= Bql<riskHold.IsEqual<False>.And<approved.IsEqual<True>.And<paymentsNeedValidationCntr.IsGreater<Zero>>>>(),

					IsNotOnRiskAndIsPaymentRequirementsViolated
						= Bql<riskHold.IsEqual<False>.And<approved.IsEqual<True>.And<prepaymentReqSatisfied.IsEqual<False>>>>(),

				}.AutoNameConditions();

				var RemoveRiskHold = context.ActionDefinitions
					.CreateNew("RemoveRiskHold", act => act
					.InFolder(FolderType.ActionsFolder)
					.DisplayName(BCObjectsMessages.RemoveRiskHold)
					.WithFieldAssignments(fass =>
					{
						fass.Add<riskHold>(v => v.SetFromValue(false));
					}
					));

				var RiskHold = context.ActionDefinitions
					.CreateNew("RiskHold", act => act
					.InFolder(FolderType.ActionsFolder)
					.DisplayName(BCObjectsMessages.RiskHold)
					.WithFieldAssignments(fass =>
					{
						fass.Add<riskHold>(v => v.SetFromValue(true));
					}
					));

				context.UpdateScreenConfigurationFor(screen =>
				{
					return screen
					.WithFieldStates(fieldStates =>
						{
							fieldStates.Add<SOOrder.status>(fieldState =>
							fieldState.SetComboValue(SOOrderStatusExt.RiskHold, BCObjectsMessages.RiskHold));
						})
					.WithActions(actions =>
						{
							actions.Add(RemoveRiskHold);
							actions.Add(RiskHold);
						})
					.WithHandlers(handlers =>
						{
							handlers.Add(handler =>
							{
								return handler
								.WithTargetOf<SOOrder>()
								.OfEntityEvent<BCSOOrderExt.Events>(e => e.RiskHoldConditionStatisfied)
								.Is(e => e.GetExtension<BCSOOrderEntryExt>().OnRiskHoldConditionSatisfied)
								.UsesTargetAsPrimaryEntity()
								.DisplayName("Risk Hold Required");
							});
						})
					.WithFlows(flows =>
						{
							flows.Update<SOBehavior.sO>(flow =>
							{
								const string initialState = "_";
								return flow
									.WithFlowStates(flowStates =>
									{
										flowStates.Add<SOOrderStatusExt.riskHold>(flowstate =>
										{
											return flowstate
											.WithActions(actions =>
											{
												actions.Add(RemoveRiskHold, g => g.IsDuplicatedInToolbar());
												actions.Add(g => g.putOnHold);
												actions.Add(g => g.cancelOrder);
											});
										});

										flowStates.Update<SOOrderStatus.open>(flowState =>
										{
											return flowState
											.WithActions(actions =>
											{
												actions.Add(RiskHold);
											})
											.WithEventHandlers(handlers =>
											{
												handlers.Add(g => g.GetExtension<BCSOOrderEntryExt>().OnRiskHoldConditionSatisfied);
											});
										});

										flowStates.Update<SOOrderStatus.hold>(flowState =>
										{
											return flowState
											.WithActions(actions =>
											{
												actions.Add(RiskHold);
											})
											.WithEventHandlers(handlers =>
											{
												handlers.Add(g => g.GetExtension<BCSOOrderEntryExt>().OnRiskHoldConditionSatisfied);
											});
										});
									})

									.WithTransitions(transitions =>
									{
										transitions.UpdateGroupFrom(initialState, ts =>
										{

											ts.Add(t => t
											.To<SOOrderStatusExt.riskHold>()
											.IsTriggeredOn(g => g.initializeState)
											.When(conditions.IsOnRiskHold)
											.PlaceAfter(tr => tr.From(initialState).To<SOOrderStatus.hold>().IsTriggeredOn(g => g.initializeState))
											.WithFieldAssignments(fas =>
											{
												fas.Add<inclCustOpenOrders>(e => e.SetFromValue(false));
											}));
										});

										transitions.UpdateGroupFrom<SOOrderStatus.hold>(ts =>
										{
											ts.Add(
												 t => t.To<SOOrderStatusExt.riskHold>()
												.IsTriggeredOn(RiskHold)
												.WithFieldAssignments(fas =>
												 {
													 fas.Add<hold>(e => e.SetFromValue(false));
												 }));

											ts.Add(
												 t => t.To<SOOrderStatusExt.riskHold>().IsTriggeredOn(g => g.releaseFromHold)
												 .PlaceBefore(tr => tr.From(SOOrderStatus.Hold).To<SOOrderStatus.pendingProcessing>().IsTriggeredOn(g => g.releaseFromHold))
												 .When(conditions.IsOnRiskHold));
										});

										transitions.UpdateGroupFrom<SOOrderStatus.open>(ts =>
										{
											ts.Add(
												 t => t.To<SOOrderStatusExt.riskHold>().IsTriggeredOn(RiskHold)
												.WithFieldAssignments(fas =>
												{
													fas.Add<inclCustOpenOrders>(e => e.SetFromValue(false));
												}));

											ts.Add(
												 t => t.To<SOOrderStatusExt.riskHold>()
												 .IsTriggeredOn(g => g.GetExtension<BCSOOrderEntryExt>().OnRiskHoldConditionSatisfied)
												 .WithFieldAssignments(fas =>
												 {
													 fas.Add<inclCustOpenOrders>(e => e.SetFromValue(false));
												 }));

										});

										transitions.AddGroupFrom<SOOrderStatusExt.riskHold>(ts =>
										{
											//put to hold on puttohold action
											ts.Add(t => t
													.To<SOOrderStatus.hold>()
													.IsTriggeredOn(g => g.putOnHold));

											//put to cancel on cancel action
											ts.Add(t => t
													.To<SOOrderStatus.cancelled>()
													.IsTriggeredOn(g => g.cancelOrder));


											ts.Add(t => t
														.To<SOOrderStatus.pendingApproval>()
														.IsTriggeredOn(RemoveRiskHold)
														.When(conditions.IsNotOnRiskHoldAndNotApproved)
														.WithFieldAssignments(fas =>
														{
															fas.Add<BCSOOrderExt.riskHold>(v => v.SetFromValue(false));

														}));

											ts.Add(t => t
														.To<SOOrderStatus.pendingProcessing>()
														.IsTriggeredOn(RemoveRiskHold)
														.When(conditions.IsNotOnRiskAndHasPaymentsInPendingProcessing));

											ts.Add(t => t
														.To<SOOrderStatus.awaitingPayment>()
														.IsTriggeredOn(RemoveRiskHold)
														.When(conditions.IsNotOnRiskAndIsPaymentRequirementsViolated));

											// put to open if releases from riskhold and let Open state decide where to go from  there
											ts.Add(t => t
													.To<SOOrderStatus.open>()
													.IsTriggeredOn(RemoveRiskHold)
													.WithFieldAssignments(fas =>
													{
														fas.Add<BCSOOrderExt.riskHold>(v => v.SetFromValue(false));
														fas.Add<inclCustOpenOrders>(e => e.SetFromValue(true));

													}));

										});
									});
							});
						});
				});
			}
		}

		public class SOOrderStatusExt
		{
			public const string RiskHold = "X";
			public class riskHold : PX.Data.BQL.BqlString.Constant<riskHold>
			{
				public riskHold() : base(RiskHold) { }
			}

		}

		public delegate void PersistDelegate();
		[PXOverride]
		public void Persist(PersistDelegate handler)
		{
			SOOrder entry = Base.Document.Current;
			BCAPISyncScope.BCSyncScopeContext context = BCAPISyncScope.GetScoped();

			if (context != null && entry != null)
			{

				SOOrderType orderType = PXSelect<SOOrderType, Where<SOOrderType.orderType, Equal<Required<SOOrderType.orderType>>
			   >>.Select(Base, entry.OrderType);
				if (entry.Hold == true && orderType?.HoldEntry == false)
					Base.Actions["releaseFromHold"].Press();
			}
			AdjustAppliedtoOrderAmount(entry, context);
			handler();
		}
		protected virtual void _(PX.Data.Events.RowSelected<SOOrder> e)
		{
			SOOrder row = e.Row;
			if (row == null)
				return;

			//To hide risks tab if not risks
			OrderRisks.AllowSelect = (row.GetExtension<BCSOOrderExt>().RiskStatus != BCCaptions.None && row.GetExtension<BCSOOrderExt>().RiskStatus != null);
			PXUIFieldAttribute.SetVisible<BCSOOrderExt.riskStatus>(e.Cache, row, OrderRisks.AllowSelect);
		}

		protected virtual void _(PX.Data.Events.FieldUpdated<BCSOOrderExt.riskStatus> e)
		{
			SOOrder order = Base.Document.Current;
			if (order != null)
			{
				if (e.NewValue != null)
				{
					string riskStatus = e.NewValue.ToString();
					BCBindingExt store = null;
					BCAPISyncScope.BCSyncScopeContext context = BCAPISyncScope.GetScoped();
					if (context != null)
						store = BCBindingExt.PK.Find(Base, context.Binding);
					else
					{
						BCSyncStatus syncStatus = PXSelect<BCSyncStatus, Where<BCSyncStatus.localID, Equal<Required<BCSyncStatus.localID>>, And<BCSyncStatus.entityType, Equal<Required<BCSyncStatus.entityType>>>>>.Select(Base, order.NoteID, BCEntitiesAttribute.Order);
						if (syncStatus != null)
							store = BCBindingExt.PK.Find(Base, syncStatus?.BindingID);
					}
					if (store != null)
					{
						if ((store.HoldOnRiskStatus == BCRiskStatusAttribute.HighRisk && riskStatus == BCCaptions.High) ||
							(store.HoldOnRiskStatus == BCRiskStatusAttribute.MediumRiskorHighRisk && (riskStatus == BCCaptions.High || riskStatus == BCCaptions.Medium)))
						{
							Base.Document.Cache.SetValueExt<BCSOOrderExt.riskHold>(Base.Document.Current, true);
							BCSOOrderExt.Events.Select(x => x.RiskHoldConditionStatisfied).FireOn(Base, order);
						}
						else
							Base.Document.Cache.SetValueExt<BCSOOrderExt.riskHold>(Base.Document.Current, false);
					}
					else
						Base.Document.Cache.SetValueExt<BCSOOrderExt.riskHold>(Base.Document.Current, false);
				}
			}
		}
		protected virtual void _(PX.Data.Events.FieldUpdated<SOOrderRisks.score> e)
		{
			if (e.NewValue != null)
			{
				decimal newValue = decimal.Parse(e.NewValue.ToString());
				if (Base.Document.Current.GetExtension<BCSOOrderExt>().MaxRiskScore == null || Base.Document.Current.GetExtension<BCSOOrderExt>().MaxRiskScore < newValue)
					Base.Document.Cache.SetValueExt<BCSOOrderExt.maxRiskScore>(Base.Document.Current, newValue);
			}
		}

		protected virtual void AdjustAppliedtoOrderAmount(SOOrder entry, BCAPISyncScope.BCSyncScopeContext context)
		{
			if (context != null)
			{
				//Adjust applied to order field to handle refunds flow
				var sOAdjusts = Base.Adjustments.Select();
				if (sOAdjusts.Count > 0 && entry.Cancelled != true && entry.Completed != true)
				{
					var appliedTotal = sOAdjusts?.ToList()?.Sum(x => x.GetItem<SOAdjust>().CuryAdjdAmt ?? 0) ?? 0;
				 if (entry.CuryOrderTotal < appliedTotal)
					{
						decimal? difference = appliedTotal - entry.CuryOrderTotal;
						foreach (var soadjust in sOAdjusts)
						{
							var adjust = soadjust.GetItem<SOAdjust>();
							if (difference == 0) break;
							if (adjust.CuryAdjdAmt > 0)
							{
								decimal? newValue = adjust.CuryAdjdAmt;
								if (difference >= adjust.CuryAdjdAmt)
									newValue = difference = difference - adjust.CuryAdjdAmt;
								else if (difference < adjust.CuryAdjdAmt)
								{
									newValue = adjust.CuryAdjdAmt - difference;
									difference = 0;
								}
								Base.Adjustments.Cache.SetValueExt<SOAdjust.curyAdjdAmt>(adjust, newValue);
								Base.Adjustments.Cache.Update((SOAdjust)Base.Adjustments.Cache.CreateCopy(adjust));
							}
						}
					}
					else if (entry.CuryOrderTotal > appliedTotal)
					{
						decimal? difference = entry.CuryOrderTotal - appliedTotal;
						foreach (var soadjust in sOAdjusts)
						{
							var adjust = soadjust.GetItem<SOAdjust>();
							if (difference == 0) break;
							decimal? balance = adjust.CuryOrigDocAmt - adjust.CuryAdjdAmt;
							if (balance > 0)
							{
								decimal? newValue = adjust.CuryAdjdAmt;
								if (difference <= balance)
								{
									newValue = adjust.CuryAdjdAmt + difference;
									difference = 0;
								}
								else if (difference > balance)
								{
									difference = difference - balance;
									newValue = adjust.CuryAdjdAmt + balance;
								}
								Base.Adjustments.Cache.SetValueExt<SOAdjust.curyAdjdAmt>(adjust, newValue);
								Base.Adjustments.Cache.Update((SOAdjust)Base.Adjustments.Cache.CreateCopy(adjust));
							}
						}
					}
				}
			}
		}
	

		public void SOOrder_RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			SOOrder order = (SOOrder)e.Row;
			BCAPISyncScope.BCSyncScopeContext context = BCAPISyncScope.GetScoped();
			if (context != null && order != null)
			{

				if (String.IsNullOrEmpty(order.ShipVia)
					&& ((order.OverrideFreightAmount == true && order.CuryFreightAmt > 0)
						|| order.CuryPremiumFreightAmt > 0))
					throw new PXException(BCObjectsMessages.OrderMissingShipVia);

			}
		}
		protected void SOOrder_TaxZoneID_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e, PXFieldDefaulting baseHandler)
		{
			baseHandler?.Invoke(sender, e);

			if (e.NewValue == null)
			{
				BCAPISyncScope.BCSyncScopeContext context = BCAPISyncScope.GetScoped();
				if (context == null) return;

				BCBindingExt store = BCBindingExt.PK.Find(Base, context.Binding);
				if (store != null && store.TaxSynchronization == true)
					e.NewValue = store.DefaultTaxZoneID;
			}
		}

		protected virtual void _(PX.Data.Events.FieldUpdating<SOOrder, SOOrder.cancelled> eventArgs)
		{
			BCAPISyncScope.BCSyncScopeContext context = BCAPISyncScope.GetScoped();
			bool value;
			bool.TryParse(eventArgs.NewValue?.ToString(), out value);
			if (value && context != null)
			{
				var sOAdjusts = Base.Adjustments.Select();
				if (sOAdjusts.Count > 0)
				{
					foreach (var soadjust in sOAdjusts)
					{
						var adjust = soadjust.GetItem<SOAdjust>();

						adjust.CuryAdjdAmt = 0;
						Base.Adjustments.Update(adjust);
					}
				}
			}
		}

		public bool? isTaxValid = null;
		protected virtual void _(PX.Data.Events.FieldUpdated<SOOrder, SOOrder.isTaxValid> e)
		{
			if (e.Row == null || e.NewValue == null) return;

			if (BCAPISyncScope.IsScoped() && (e.NewValue as Boolean?) == true)
			{
				isTaxValid = true;
			}
		}

		//Sync Time 
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PX.Commerce.Core.BCSyncExactTime()]
		public void SOOrder_LastModifiedDateTime_CacheAttached(PXCache sender) { }


		protected virtual void _(PX.Data.Events.RowPersisting<SOOrder> e)
		{
			if (e.Row == null || (e.Operation & PXDBOperation.Command) != PXDBOperation.Update) return;
			Object oldRow = e.Cache.GetOriginal(e.Row);

			List<Type> monitoringTypes = new List<Type>();
			monitoringTypes.Add(typeof(SOOrder.customerID));
			monitoringTypes.Add(typeof(SOOrder.customerLocationID));
			monitoringTypes.Add(typeof(SOOrder.curyID));
			monitoringTypes.Add(typeof(SOOrder.orderQty));
			monitoringTypes.Add(typeof(SOOrder.curyDiscTot));
			monitoringTypes.Add(typeof(SOOrder.curyTaxTotal));
			monitoringTypes.Add(typeof(SOOrder.curyOrderTotal));
			monitoringTypes.Add(typeof(SOOrder.curyFreightTot));
			monitoringTypes.Add(typeof(SOOrder.shipVia));

			if (e.Cache.GetStatus(e.Row) != PXEntryStatus.Inserted && !BCAPISyncScope.IsScoped() && ((bool?)e.Cache.GetValue<BCSOOrderExt.externalOrderOriginal>(e.Row) == true)
				&& monitoringTypes.Any(t => !object.Equals(e.Cache.GetValue(e.Row, t.Name), e.Cache.GetValue(oldRow, t.Name))))
			{
				e.Cache.SetValueExt<BCSOOrderExt.externalOrderOriginal>(e.Row, false);
			}
		}
		protected virtual void _(PX.Data.Events.RowPersisting<SOLine> e)
		{
			if (e.Row == null || (e.Operation & PXDBOperation.Command) != PXDBOperation.Update) return;
			Object oldRow = e.Cache.GetOriginal(e.Row);

			List<Type> monitoringTypes = new List<Type>();
			monitoringTypes.Add(typeof(SOLine.inventoryID));
			monitoringTypes.Add(typeof(SOLine.curyDiscAmt));
			monitoringTypes.Add(typeof(SOLine.curyLineAmt));

			if (e.Cache.GetStatus(e.Row) != PXEntryStatus.Inserted && !BCAPISyncScope.IsScoped()
				&& ((bool?)Base.Document.Cache.GetValue<BCSOOrderExt.externalOrderOriginal>(Base.Document.Current) == true)
				&& monitoringTypes.Any(t => !object.Equals(e.Cache.GetValue(e.Row, t.Name), e.Cache.GetValue(oldRow, t.Name))))
			{
				Base.Document.Cache.SetValueExt<BCSOOrderExt.externalOrderOriginal>(Base.Document.Current, false);
			}
		}
		protected virtual void _(PX.Data.Events.RowUpdated<SOBillingAddress> e)
		{
			if (e.Row == null || e.OldRow == null || Base.Document.Current == null) return;

			if (e.ExternalCall && e.Cache.GetStatus(e.Row) != PXEntryStatus.Inserted && !BCAPISyncScope.IsScoped()
				&& ((bool?)Base.Document.Cache.GetValue<BCSOOrderExt.externalOrderOriginal>(Base.Document.Current) == true))
			{
				Base.Document.Cache.SetValueExt<BCSOOrderExt.externalOrderOriginal>(Base.Document.Current, false);
			}
		}
		protected virtual void _(PX.Data.Events.RowUpdated<SOBillingContact> e)
		{
			if (e.Row == null || e.OldRow == null || Base.Document.Current == null) return;

			if (e.ExternalCall && e.Cache.GetStatus(e.Row) != PXEntryStatus.Inserted && !BCAPISyncScope.IsScoped()
				&& ((bool?)Base.Document.Cache.GetValue<BCSOOrderExt.externalOrderOriginal>(Base.Document.Current) == true))
			{
				Base.Document.Cache.SetValueExt<BCSOOrderExt.externalOrderOriginal>(Base.Document.Current, false);
			}
		}

		protected virtual void _(PX.Data.Events.RowUpdated<SOShippingAddress> e)
		{
			if (e.Row == null || e.OldRow == null || Base.Document.Current == null) return;

			if (e.ExternalCall && e.Cache.GetStatus(e.Row) != PXEntryStatus.Inserted && !BCAPISyncScope.IsScoped()
				&& ((bool?)Base.Document.Cache.GetValue<BCSOOrderExt.externalOrderOriginal>(Base.Document.Current) == true))
			{
				Base.Document.Cache.SetValueExt<BCSOOrderExt.externalOrderOriginal>(Base.Document.Current, false);
			}
		}
		protected virtual void _(PX.Data.Events.RowUpdated<SOShippingContact> e)
		{
			if (e.Row == null || e.OldRow == null || Base.Document.Current == null) return;

			if (e.ExternalCall && e.Cache.GetStatus(e.Row) != PXEntryStatus.Inserted && !BCAPISyncScope.IsScoped()
				&& ((bool?)Base.Document.Cache.GetValue<BCSOOrderExt.externalOrderOriginal>(Base.Document.Current) == true))
			{
				Base.Document.Cache.SetValueExt<BCSOOrderExt.externalOrderOriginal>(Base.Document.Current, false);
			}
		}

		//to handle payments with Order, taxsyncMone= nosync, as applied to order amount will be greater due to taxes than ordertotal which is without taxes
		protected virtual void _(PX.Data.Events.RowPersisting<SOAdjust> e)
		{

			if (e.Row == null || Base.Document.Current == null || (e.Operation & PXDBOperation.Command) == PXDBOperation.Update || Base.Document.Current.IsTaxValid != false) return;
			BCAPISyncScope.BCSyncScopeContext context = BCAPISyncScope.GetScoped();
			if (context != null)
			{
				//Calculated Unpaid Balance
				decimal curyUnpaidBalance = Base.Document.Current.CuryOrderTotal ?? 0m;
				foreach (SOAdjust adj in Base.Adjustments.Select())
				{
					curyUnpaidBalance -= adj.CuryAdjdAmt ?? 0m;
				}

				decimal applicationAmount = (decimal)e.Row.CuryAdjdAmt > curyUnpaidBalance ? curyUnpaidBalance : (decimal)e.Row.CuryAdjdAmt;
				e.Cache.SetValueExt<SOAdjust.curyAdjdAmt>(e.Row, applicationAmount);

			}
		}
	}



}