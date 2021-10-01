using PX.Data;
using PX.Objects.AR;
using PX.Objects.SO;
using System;
using System.Collections.Generic;

namespace PX.Objects.FS
{
    public class FSEntityType
    {
        public class ListAttribute : PXStringListAttribute
        {
            public ListAttribute() : base(
                new[]
                {
                    Pair(ServiceOrder, TX.BillHistory_EntityType.ServiceOrder),
                    Pair(Appointment, TX.BillHistory_EntityType.Appointment),
                    Pair(ServiceContract, TX.BillHistory_EntityType.ServiceContract),
                    Pair(SalesOrder, TX.BillHistory_EntityType.SalesOrder),
                    Pair(SOInvoice, TX.BillHistory_EntityType.SOInvoice),
                    Pair(ARInvoice, TX.BillHistory_EntityType.ARInvoice),
                    Pair(APInvoice, TX.BillHistory_EntityType.APInvoice),
                    Pair(PMRegister, TX.BillHistory_EntityType.PMRegister),
                    Pair(INReceipt, TX.BillHistory_EntityType.INReceipt),
                    Pair(INIssue, TX.BillHistory_EntityType.INIssue),
                    Pair(SOCreditMemo, TX.BillHistory_EntityType.SOCreditMemo),
                    Pair(ARCreditMemo, TX.BillHistory_EntityType.ARCreditMemo),
                })
            { }
        }

        public const string ServiceOrder = "FSSO";
        public const string Appointment = "FSAP";
        public const string ServiceContract = "FSSC";
        public const string SalesOrder = "PXSO";
        public const string SOInvoice = "PXSI";
        public const string ARInvoice = "PXAR";
        public const string APInvoice = "PXAP";
        public const string PMRegister = "PXPM";
        public const string INReceipt = "PXIR";
        public const string INIssue = "PXIS";
        public const string SOCreditMemo = "PXSM";
        public const string ARCreditMemo = "PXAM";

        public class serviceOrder : PX.Data.BQL.BqlString.Constant<serviceOrder>
        {
            public serviceOrder() : base(ServiceOrder)
            {
            }
        }

        public class appointment : PX.Data.BQL.BqlString.Constant<appointment>
        {
            public appointment() : base(Appointment)
            {
            }
        }

        public class serviceContract : PX.Data.BQL.BqlString.Constant<serviceContract>
        {
            public serviceContract() : base(ServiceContract)
            {
            }
        }

        public class salesOrder : PX.Data.BQL.BqlString.Constant<salesOrder>
        {
            public salesOrder() : base(SalesOrder)
            {
            }
        }

        public class soInvoice : PX.Data.BQL.BqlString.Constant<soInvoice>
        {
            public soInvoice() : base(SOInvoice)
            {
            }
        }

        public class arInvoice : PX.Data.BQL.BqlString.Constant<arInvoice>
        {
            public arInvoice() : base(ARInvoice)
            {
            }
        }

        public class apInvoice : PX.Data.BQL.BqlString.Constant<apInvoice>
        {
            public apInvoice() : base(APInvoice)
            {
            }
        }

        public class pmRegister : PX.Data.BQL.BqlString.Constant<pmRegister>
        {
            public pmRegister() : base(PMRegister)
            {
            }
        }

        public class inIssue : PX.Data.BQL.BqlString.Constant<inIssue>
        {
            public inIssue() : base(INIssue)
            {
            }
        }

        public class inReceipt : PX.Data.BQL.BqlString.Constant<inReceipt>
        {
            public inReceipt() : base(INReceipt)
            {
            }
        }

        public class soCreditMemo : PX.Data.BQL.BqlString.Constant<soCreditMemo>
        {
            public soCreditMemo() : base(SOCreditMemo)
            {
            }
        }

        public class arCreditMemo : PX.Data.BQL.BqlString.Constant<arCreditMemo>
        {
            public arCreditMemo() : base(ARCreditMemo)
            {
            }
        }
    }
}
