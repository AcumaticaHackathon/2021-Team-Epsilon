using System;
using System.Collections.Generic;
using System.Linq;
using PX.Data;
using PX.Objects.CM;

namespace PX.Objects.Common.GraphExtensions
{
    public class CurySettingsExtension<TGraph, TDAC, TCuryDAC> : PXGraphExtension<TGraph> where TGraph : PXGraph
        where TDAC : class, IBqlTable, new()
        where TCuryDAC : class, IBqlTable, new()
    {
        #region Fields
        protected PXView curySettings;
        protected string curyID = "CuryID";
        protected virtual string ViewName => "CurySettings_" + typeof(TDAC).Name;
        private object updatedRecord;
        #endregion
        
        #region Initialization
        public override void Initialize()
        {
            base.Initialize();
            curySettings = new PXView(Base, false,
                BqlCommand.CreateInstance(ComposeCommand().ToArray()));
            Base.Views.Add(ViewName, curySettings);
            Base.Views.Caches.Add(typeof(TCuryDAC));
			Base.FieldDefaulting.AddHandler(typeof(TCuryDAC), curyID, 
                (sender, args) => args.NewValue = Base.Accessinfo.BaseCuryID ?? CurrencyCollection.GetBaseCurrency()?.CuryID);
        }
        #endregion
        
        #region Events
        
        protected virtual void _(Events.RowUpdated<TCuryDAC> e)
        {
            PXCache cache = Base.Caches<TDAC>();
            var record = PXParentAttribute.SelectParent(e.Cache, e.Row);
            if (updatedRecord != record)
            {
                if (IsTenantBaseCurrency(e.Row) && 
                    GetCurySettingsFields().Any(field =>
                        !Equals(e.Cache.GetValue(e.Row, field), e.Cache.GetValue(e.OldRow, field))))
                {
                    record = cache.CreateCopy(record);
                    foreach (string field in GetCurySettingsFields())
                    {
                        cache.SetValue(record, field, e.Cache.GetValue(e.Row, field));
                    }

                    cache.Update(record);
                }
            }
        }
        protected virtual void _(Events.RowInserted<TCuryDAC> e)
        {
            PXCache cache = Base.Caches<TDAC>();
            var record = PXParentAttribute.SelectParent(e.Cache, e.Row);
            if (updatedRecord != record)
            {
                if (IsTenantBaseCurrency(e.Row))
                {
                    record = cache.CreateCopy(record);
                    foreach (string field in GetCurySettingsFields())
                    {
                        cache.SetValue(record, field, e.Cache.GetValue(e.Row, field));
                    }

                    cache.Update(record);
                }
            }
        }

        protected virtual void _(Events.RowUpdated<TDAC> e)
        {
            PXCache cache = Base.Caches<TCuryDAC>();
            if (GetCurySettingsFields().Any(field =>
                !Equals(e.Cache.GetValue(e.Row, field),
                    e.Cache.GetValue(e.OldRow, field))))
            {
                updatedRecord = e.Row;
                try
                {
                    object record = curySettings.SelectSingleBound(new object[] {updatedRecord},
                        CurrencyCollection.GetBaseCurrency()?.CuryID);
                    if (record == null)
                    {
                        record = cache.Insert();
                    }

                    record = cache.CreateCopy(record);
                    foreach (string str in GetCurySettingsFields())
                    {
                        cache.SetValue(record, str, e.Cache.GetValue(e.Row, str));
                    }

                    cache.Update(record);
                }
                finally
                {
                    updatedRecord = null;
                }
            }
        }
        #endregion

        #region Implementation
        protected virtual List<Type> ComposeCommand()
        {
            PXCache sourceCache = Base.Caches<TDAC>();
            PXCache curyCache = Base.Caches<TCuryDAC>();
            List<Type> list = new List<Type>(15)
            {
                typeof(Select<,>),
                typeof(TCuryDAC)
            };
            for (int i = 0; i < curyCache.Keys.Count; i++)
            {
                string key = curyCache.Keys[i];
                if (list.Count == 2)
                {
                    list.Add(typeof(Where<,,>));
                }
                else if (i < (curyCache.Keys.Count - 2))
                {
                    list.Add(typeof(And<,,>));
                }
                else
                {
                    list.Add(typeof(And<,>));
                }

                list.Add(curyCache.GetBqlField(key));
                list.Add(typeof(Equal<>));
                list.Add((key == curyID) ? typeof(Optional<>) : typeof(Current<>));
                list.Add((key == curyID) ? typeof(AccessInfo.baseCuryID) : sourceCache.GetBqlField(key));
            }

            return list;
        }

        protected virtual IEnumerable<string> GetCurySettingsFields()
        {
            PXCache cache = Base.Caches[typeof(TCuryDAC)];
            return cache.Fields.Where(f => !cache.Keys.Contains(f) && !GraphHelper.IsAuditFieldName(f));
        }
        protected bool IsTenantBaseCurrency(object row) =>
            string.Compare(Base.Caches<TCuryDAC>().GetValue(row, curyID) as string,
                CurrencyCollection.GetBaseCurrency()?.CuryID, StringComparison.InvariantCulture) == 0;

        #endregion
    }
}
 

 
