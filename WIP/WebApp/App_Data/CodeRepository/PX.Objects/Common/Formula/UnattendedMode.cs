using System;
using System.Collections.Generic;

using PX.Data;

namespace PX.Objects.Common
{
    public class UnattendedMode : BqlFormulaEvaluator, IBqlOperand
    {
        public override object Evaluate(PXCache cache, object item, Dictionary<Type, object> pars)
        {
            return cache.Graph.UnattendedMode;
        }
    }
}