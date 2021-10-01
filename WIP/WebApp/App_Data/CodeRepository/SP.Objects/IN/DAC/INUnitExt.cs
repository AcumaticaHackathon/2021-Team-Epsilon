using System;
using System.Collections;
using System.Collections.Generic;
using PX.Objects.IN;
using PX.SM;
using PX.Data;


namespace SP.Objects.IN.DAC
{
    [Serializable]
    public class INUnitExt : PXCacheExtension<INUnit>
    {
        #region Convertion Factor
        public abstract class convertionFactor : PX.Data.IBqlField
        {
        }
        [PXString()]
        [PXUIField(DisplayName = "Conversion Factor")]
        public virtual string ConvertionFactor 
        {
            get
            {
                if (Base.UnitRate == 1)
                    return "Base Unit";
                return Base.UnitRate.ToString();
            }
        }
        #endregion
    }
}