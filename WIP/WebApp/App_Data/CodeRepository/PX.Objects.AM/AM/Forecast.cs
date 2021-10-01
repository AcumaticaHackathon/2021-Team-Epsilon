using System;
using System.Collections.Generic;
using PX.Data;
using PX.Objects.AM.Attributes;
using PX.Common;
using PX.Objects.Common;
using PX.Objects.CS;
using System.Collections;

namespace PX.Objects.AM
{
    /// <summary>
    /// MRP Forecast Graph
    /// </summary>
    public class Forecast : PXGraph<Forecast>, PXImportAttribute.IPXPrepareItems
    {
        public PXSave<AMForecast> Save;
        public PXCancel<AMForecast> Cancel;
        public PXCopyPasteAction<AMForecast> CopyPaste;

        [PXFilterable]
        [PXImport(typeof(AMForecast))]
        public PXSelect<AMForecast> ForecastRecords;
        public PXSetup<AMRPSetup> setup;
        [PXHidden]
        public PXSetup<Numbering>.Where<Numbering.numberingID.IsEqual<AMRPSetup.forecastNumberingID.FromCurrent>> ForecastNumbering;

        public Forecast()
        {
            var mrpSetup = setup.Current;

            // Forecast numbering id is optional unless you want to use forecast you must have a value entered
            if (mrpSetup == null || string.IsNullOrWhiteSpace(mrpSetup.ForecastNumberingID))
            {
                throw new PXSetupNotEnteredException(AM.Messages.GetLocal(AM.Messages.ForecastNumberSequenceSetupNotEntered), typeof(AMRPSetup), AM.Messages.GetLocal(AM.Messages.MrpSetup));
            }

            PXUIFieldAttribute.SetVisible<AMForecast.forecastID>(ForecastRecords.Cache, null, IsForecastIdEnabled);
        }

        /// <summary>
        /// IS the forecast id field enabled for the user? (Linked to manual numbering for the forecast numbering sequence)
        /// </summary>
        public bool IsForecastIdEnabled
        {
            get
            {
                if (ForecastNumbering == null)
                {
                    return false;
                }

                if (ForecastNumbering.Current == null)
                {
                    ForecastNumbering.Current = ForecastNumbering.Select();

                    if (ForecastNumbering.Current == null)
                    {
                        return false;
                    }
                }

                return ForecastNumbering.Current.UserNumbering.GetValueOrDefault();
            }
        }

        protected virtual void _(Events.FieldUpdated<AMForecast, AMForecast.beginDate> e)
        {
            SetEndDate(e.Cache, e.Row);
        }

        protected virtual void _(Events.FieldUpdated<AMForecast, AMForecast.interval> e)
        {
            SetEndDate(e.Cache, e.Row);
        }

        protected virtual void _(Events.RowSelected<AMForecast> e)
        {
            PXUIFieldAttribute.SetEnabled<AMForecast.forecastID>(e.Cache, null, IsForecastIdEnabled);
        }

        protected virtual void _(Events.RowPersisting<AMForecast> e)
        {
            if (e.Row == null)
            {
                return;
            }

            if (e.Row.Qty.GetValueOrDefault() <= 0)
            {
                e.Cache.RaiseExceptionHandling<AMForecast.qty>(e.Row, e.Row.Qty, new PXSetPropertyException(AM.Messages.QuantityGreaterThanZero));
            }
            if (!Common.Dates.StartBeforeEnd(e.Row.BeginDate, e.Row.EndDate))
            {
                e.Cache.RaiseExceptionHandling<AMForecast.endDate>(e.Row, e.Row.EndDate, 
                    new PXSetPropertyException(AM.Messages.MustBeGreaterThan, 
                        PXUIFieldAttribute.GetDisplayName<AMForecast.endDate>(ForecastRecords.Cache),
                        PXUIFieldAttribute.GetDisplayName<AMForecast.beginDate>(ForecastRecords.Cache)));
            }
        }

        /// <summary>
        /// Send the end date based on the current interval/begin date
        /// </summary>
        protected virtual void SetEndDate(PXCache cache, AMForecast forecast)
        {
            if (forecast == null
                || Common.Dates.IsDateNull(forecast.BeginDate)
                || string.IsNullOrWhiteSpace(forecast.Interval))
            {
                return;
            }

            var endDate = GetEndDate(cache, forecast);
            cache.SetValueExt<AMForecast.endDate>(forecast, endDate);
        }

        /// <summary>
        /// Get the calculated forecast end date
        /// </summary>
        public virtual DateTime? GetEndDate(PXCache cache, AMForecast forecast)
        {
            if (forecast == null
                || Common.Dates.IsDateNull(forecast.BeginDate)
                || string.IsNullOrWhiteSpace(forecast.Interval))
            {
                return forecast == null || forecast.BeginDate == null ? Accessinfo.BusinessDate : forecast.BeginDate;
            }

            return ForecastInterval.GetEndDate(forecast.Interval, forecast.BeginDate.GetValueOrDefault());
        }

        protected virtual void _(Events.FieldDefaulting<AMForecast, AMForecast.endDate> e)
        {
            if (e.Row == null)
            {
                e.NewValue = Accessinfo.BusinessDate;
                return;
            }

            e.NewValue = GetEndDate(e.Cache, e.Row);
        }

        /// <summary>
        /// Converts a forecast record into the intervals that would be used for MRP Planning
        /// </summary>
        /// <param name="forecast">Valid forecast row</param>
        /// <returns>List of paired start/end dates</returns>
        public static List<Tuple<DateTime, DateTime>> GetForecastIntervals(AMForecast forecast)
        {
            if (forecast == null || forecast.BeginDate == null || forecast.EndDate == null)
            {
                return null;
            }

            DateTime beginDate = forecast.BeginDate ?? Common.Dates.EndOfTimeDate;
            DateTime endDate = forecast.EndDate ?? Common.Dates.BeginOfTimeDate;
            var intervals = new List<Tuple<DateTime, DateTime>>();

            if (string.IsNullOrWhiteSpace(forecast.Interval) || forecast.Interval == ForecastInterval.OneTime)
            {
                intervals.Add(new Tuple<DateTime, DateTime>(beginDate, endDate));
                return intervals;
            }

            DateTime intervalEndDate = endDate;
            while (beginDate < intervalEndDate)
            {
                endDate = ForecastInterval.GetEndDate(forecast.Interval, beginDate);

                intervals.Add(new Tuple<DateTime, DateTime>(beginDate, endDate));

                beginDate = endDate.AddDays(1);
            }

            return intervals;
        }

        protected virtual void _(Events.RowInserting<AMForecast> e)
        {
            if (IsForecastIdEnabled || e.Row == null)
            {
                return;
            }

            // Temp key to allow multiple inserts before persisting. When persisting and auto number it will swap the value for us.
            var insertedCounter = e.Cache.Inserted.Count() + 1;
            e.Row.ForecastID = $"-{insertedCounter}";
        }

        #region Implementation of IPXPrepareItems

        public MultiDuplicatesSearchEngine<AMForecast> DuplicateFinder { get; set; }

        private bool CanUpdateExistingRecords
        {
            get
            {
                return IsImportFromExcel && PXExecutionContext.Current.Bag.TryGetValue(PXImportAttribute._DONT_UPDATE_EXIST_RECORDS, out var dontUpdateExistRecords) &&
                    false.Equals(dontUpdateExistRecords);
            }
        }

        protected virtual Type[] GetImportAlternativeKeyFields()
        {
            var keys = new List<Type>()
            {
                typeof(AMForecast.inventoryID),
                typeof(AMForecast.siteID),
                typeof(AMForecast.interval),
                typeof(AMForecast.beginDate),
                typeof(AMForecast.customerID)
            };

            if (PXAccess.FeatureInstalled<FeaturesSet.subItem>())
            {
                keys.Add(typeof(AMForecast.subItemID));
            }

            return keys.ToArray();
        }

        public bool PrepareImportRow(string viewName, IDictionary keys, IDictionary values)
        {
            if (string.Compare(viewName, nameof(ForecastRecords), true) != 0 || !CanUpdateExistingRecords || keys == null)
            {
                return true;
            }

            if (DuplicateFinder == null)
            {
                DuplicateFinder = new MultiDuplicatesSearchEngine<AMForecast>(ForecastRecords.Cache, GetImportAlternativeKeyFields(), ForecastRecords.SelectMain());
            }

            var duplicate = DuplicateFinder.Find(values);
            var containsForecastId = keys.Contains(nameof(AMForecast.forecastID));
            if (duplicate != null)
            {
                DuplicateFinder.RemoveItem(duplicate);

                if (containsForecastId)
                {
                    keys[nameof(AMForecast.forecastID)] = duplicate.ForecastID;
                }
                else
                {
                    keys.Add(nameof(AMForecast.forecastID), duplicate.ForecastID);
                }
            }
            else if (containsForecastId)
            {
                var value = keys[nameof(AMForecast.forecastID)] as string;
                var lineExists = !string.IsNullOrWhiteSpace(value) && ForecastRecords.Cache.Locate(new AMForecast { ForecastID = value }) != null;

                if (lineExists)
                {
                    keys.Remove(nameof(AMForecast.forecastID));
                }
            }

            return true;
        }

        public bool RowImporting(string viewName, object row)
        {
            return row == null;
        }

        public bool RowImported(string viewName, object row, object oldRow)
        {
            return oldRow == null;
        }

        public void PrepareItems(string viewName, IEnumerable items)
        {
        }

        #endregion
    }
}