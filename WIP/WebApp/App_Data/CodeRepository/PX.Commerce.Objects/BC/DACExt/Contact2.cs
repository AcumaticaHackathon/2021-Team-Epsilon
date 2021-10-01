using PX.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Commerce.Objects
{
    public class Contact2 : PX.Objects.CR.Contact
    {
        public new abstract class contactID : PX.Data.BQL.BqlInt.Field<contactID> { }
        public new abstract class bAccountID : PX.Data.BQL.BqlInt.Field<bAccountID> { }
    }
}
