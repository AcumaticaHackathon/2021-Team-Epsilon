using Autofac;
using PX.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Web.Compilation;

namespace PX.Objects.IN.Matrix.GraphExtensions
{
    public class ExtensionSorting : Module
    {
        protected override void Load(ContainerBuilder builder) => builder.RunOnApplicationStart(() =>
            PXBuildManager.SortExtensions += list => PXBuildManager.PartialSort(list, _order)
            );

        private static readonly Dictionary<Type, int> _order = new Dictionary<Type, int>
        {
            {typeof(CreateMatrixItemsTabExt), 1 },
            {typeof(ApplyToMatrixItemsExt), 2 }
        };
    }
}
