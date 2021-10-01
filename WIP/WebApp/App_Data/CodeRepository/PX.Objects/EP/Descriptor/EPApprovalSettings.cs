using System;
using System.Collections.Generic;
using System.Linq;
using PX.Api;
using PX.Data;
using PX.Data.EP;
using PX.Objects.CN.Common.Extensions;
using PX.Objects.Common.Extensions;

namespace PX.Objects.EP
{
    public class EPApprovalSettings<SetupApproval> : IPrefetchable
        where SetupApproval : IBqlTable

    {
        public static Type IsApprovalDisabled<DocType, DocTypes>(params string[] availableDocTypes)
            where DocType : IBqlField => Slot.IsApprovalDisableCommand<DocType, DocTypes>(availableDocTypes);

        public static Type IsApprovalDisabled<DocType, DocTypes, StateCondition>(params string[] availableDocTypes)
            where DocType : IBqlField 
            where StateCondition : IBqlUnary => 
            ComposeAnd<StateCondition>(Slot.IsApprovalDisableCommand<DocType, DocTypes>(availableDocTypes));
        
        public static Type ComposeAnd<AddCondition>(Type baseCondition)
            where AddCondition : IBqlUnary =>
                BqlCommand.Compose(typeof(Where2<,>),
                baseCondition,
                typeof(And<>),
                typeof(AddCondition));
        
        public static List<string> ApprovedDocTypes => Slot.DocTypes;
        
        private static EPApprovalSettings<SetupApproval> Slot => PXDatabase
            .GetSlot<EPApprovalSettings<SetupApproval>>(typeof(SetupApproval).FullName, typeof(SetupApproval));
        private List<string> DocTypes { get; set; }

        void IPrefetchable.Prefetch()
        {
            DocTypes = new List<string>();
            if (!PXAccess.FeatureInstalled<CS.FeaturesSet.approvalWorkflow>()) return;
            foreach (PXDataRecord rec in PXDatabase.SelectMulti<SetupApproval>(
                new PXDataField<AP.APSetupApproval.docType>(),
                new PXDataFieldValue<AP.APSetupApproval.isActive>(true)))
            {
                DocTypes.Add(rec.GetString(0));
            }
        }

        private Type IsApprovalDisableCommand<DocType, DocTypeList>(params string[] availableDocTypes)
            where DocType : IBqlField
        {
            Type command = null;
            
            Type type = typeof(DocTypeList);
            var constans = new Dictionary<string, Type>();
            foreach (var constant in
                type
                    .GetNestedTypes()
                    .Where(t => typeof(IConstant).IsAssignableFrom(t)))
            {
                if (Activator.CreateInstance(constant) is IConstant c)
                {
                    var key = c.Value.ToString();
                    if(!constans.ContainsKey(key))
                        constans.Add(key, constant);
                }
            }
            
            foreach (string docType in DocTypes
                .Where(e => availableDocTypes.Length == 0 || availableDocTypes.Contains(e)))
            {
                if (constans.TryGetValue(docType, out var constType))
                {
                    command = (command == null)
                        ? BqlCommand.Compose(typeof(Where<,>), typeof(DocType), typeof(Equal<>), constType)
                        : BqlCommand.Compose(typeof(Where<,,>), typeof(DocType), typeof(Equal<>), constType,
                            typeof(Or<>),
                            command);
                }
            }
            
            return command == null
                ? typeof(Where<True, Equal<True>>)
                : BqlCommand.Compose(typeof(Not<>), command);
        }
    }
}