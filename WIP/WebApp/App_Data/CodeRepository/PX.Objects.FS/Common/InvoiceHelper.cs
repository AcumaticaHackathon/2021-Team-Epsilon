using PX.Data;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.IN;
using PX.Objects.PM;
using PX.Objects.SO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static PX.Objects.FS.MessageHelper;

namespace PX.Objects.FS
{
    public class InvoiceHelper 
    {
        public static IInvoiceGraph CreateInvoiceGraph(string targetScreen)
        {
            if (targetScreen == ID.Batch_PostTo.SO)
            {
                if (PXAccess.FeatureInstalled<FeaturesSet.distributionModule>())
                {
                    return PXGraph.CreateInstance<SOOrderEntry>().GetExtension<SM_SOOrderEntry>();
                }
                else
                {
                    throw new PXException(TX.Error.DISTRIBUTION_MODULE_IS_DISABLED);
                }
            }
            else if (targetScreen == ID.Batch_PostTo.SI)
            {
                if (PXAccess.FeatureInstalled<FeaturesSet.distributionModule>() && PXAccess.FeatureInstalled<FeaturesSet.advancedSOInvoices>())
                {
                    return PXGraph.CreateInstance<SOInvoiceEntry>().GetExtension<SM_SOInvoiceEntry>();
                }
                else
                {
                    throw new PXException(TX.Error.ADVANCED_SO_INVOICE_IS_DISABLED);
                }
            }
            else if (targetScreen == ID.Batch_PostTo.AR)
            {
                return PXGraph.CreateInstance<ARInvoiceEntry>().GetExtension<SM_ARInvoiceEntry>();
            }
            else if (targetScreen == ID.Batch_PostTo.AP)
            {
                return PXGraph.CreateInstance<APInvoiceEntry>().GetExtension<SM_APInvoiceEntry>();
            }
            else if (targetScreen == ID.Batch_PostTo.PM)
            {
                return PXGraph.CreateInstance<RegisterEntry>().GetExtension<SM_RegisterEntry>();
            }
            else if (targetScreen == ID.Batch_PostTo.IN)
            {
                return PXGraph.CreateInstance<INIssueEntry>().GetExtension<SM_INIssueEntry>();
            }
            else
            {
                throw new PXException(TX.Error.POSTING_MODULE_IS_INVALID, targetScreen);
            }
        }

        public static bool AreAppointmentsPostedInSO(PXGraph graph, int? sOID)
        {
            if (sOID == null)
            {
                return false;
            }

            return PXSelectReadonly<FSAppointment,
                   Where<
                       FSAppointment.pendingAPARSOPost, Equal<False>,
                   And<
                       FSAppointment.sOID, Equal<Required<FSAppointment.sOID>>>>>
                   .Select(graph, sOID).Count() > 0;
        }

        public static void CopyContact(IContact dest, IContact source)
        {
            CS.ContactAttribute.CopyContact(dest, source);

            //Copy fields that are missing in the previous method
            dest.Attention = source.Attention;
        }

        public static void CopyAddress(IAddress dest, IAddress source)
        {
            AddressAttribute.Copy(dest, source);

            //Copy fields that are missing in the previous method
            dest.IsValidated = source.IsValidated;
        }
    }
}
