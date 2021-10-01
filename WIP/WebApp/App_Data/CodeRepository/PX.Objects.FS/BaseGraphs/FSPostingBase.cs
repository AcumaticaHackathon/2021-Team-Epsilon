using PX.Common;
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
using System.Reflection;
using System.Text;
using static PX.Objects.FS.MessageHelper;

namespace PX.Objects.FS
{
    public abstract class FSPostingBase<TGraph> : PXGraphExtension<TGraph>, IInvoiceGraph
        where TGraph : PXGraph
    {
        #region IInvoiceGraph abstracts
        public abstract void CreateInvoice(PXGraph graphProcess, List<DocLineExt> docLines, short invtMult, DateTime? invoiceDate, string invoiceFinPeriodID, OnDocumentHeaderInsertedDelegate onDocumentHeaderInserted, OnTransactionInsertedDelegate onTransactionInserted, PXQuickProcess.ActionFlow quickProcessFlow);

        public abstract FSCreatedDoc PressSave(int batchID, List<DocLineExt> docLines, BeforeSaveDelegate beforeSave);

        public abstract void Clear();

        public abstract PXGraph GetGraph();

        public abstract void DeleteDocument(FSCreatedDoc fsCreatedDocRow);

        public abstract void CleanPostInfo(PXGraph cleanerGraph, FSPostDet fsPostDetRow);

        public abstract void UpdateCostAndPrice(List<DocLineExt> docLines);

        public abstract List<ErrorInfo> GetErrorInfo();

        public bool IsInvoiceProcessRunning { get; set; }
        #endregion

        #region AutoNumbering
        public virtual void CheckAutoNumbering(string numberingID)
        {
            Numbering numbering = null;

            if (numberingID != null)
            {
                numbering = PXSelect<Numbering,
                            Where<
                                Numbering.numberingID, Equal<Required<Numbering.numberingID>>>>
                            .Select(Base, numberingID);
            }

            if (numbering == null)
            {
                throw new PXSetPropertyException(CS.Messages.NumberingIDNull);
            }

            if (numbering.UserNumbering == true)
            {
                throw new PXSetPropertyException(CS.Messages.CantManualNumber, numbering.NumberingID);
            }
        }
        #endregion

        #region Contact & Address
        public class ContactAddressSource
        {
            public string BillingSource;
            public string ShippingSource;
        }

        public virtual PXResult<Location, Contact, Address> GetContactAndAddressFromLocation(PXGraph graph, int? locationID)
        {
            return (PXResult<Location, Contact, Address>)
                   PXSelectJoin<Location,
                   LeftJoin<Contact,
                   On<
                       Contact.contactID, Equal<Location.defContactID>>,
                   LeftJoin<Address,
                   On<
                       Address.addressID, Equal<Location.defAddressID>>>>,
                   Where<
                       Location.locationID, Equal<Required<Location.locationID>>>>
                   .Select(graph, locationID);
        }

        public virtual PXResult<Location, Customer, Contact, Address> GetContactAddressFromDefaultLocation(PXGraph graph, int? bAccountID)
        {
            return (PXResult<Location, Customer, Contact, Address>)
                   PXSelectJoin<Location,
                   InnerJoin<Customer,
                   On<
                       Customer.bAccountID, Equal<Location.bAccountID>,
                       And<Customer.defLocationID, Equal<Location.locationID>>>,
                   LeftJoin<Contact,
                   On<
                       Contact.contactID, Equal<Location.defContactID>>,
                   LeftJoin<Address,
                   On<
                       Address.addressID, Equal<Location.defAddressID>>>>>,
                   Where<
                       Location.bAccountID, Equal<Required<Location.bAccountID>>>>
                   .Select(graph, bAccountID);
        }

        public virtual void GetSrvOrdContactAddress(PXGraph graph, FSServiceOrder fsServiceOrder, out FSContact fsContact, out FSAddress fsAddress)
        {
            fsContact = FSContact.PK.Find(graph, fsServiceOrder.ServiceOrderContactID);

            fsAddress = FSAddress.PK.Find(graph, fsServiceOrder.ServiceOrderAddressID);
        }

        public virtual void SetContactAndAddress(PXGraph graph, FSServiceOrder fsServiceOrderRow)
        {
            int? billCustomerID = fsServiceOrderRow.BillCustomerID;
            int? billLocationID = fsServiceOrderRow.BillLocationID;
            Customer billCustomer = SharedFunctions.GetCustomerRow(graph, billCustomerID);

            ContactAddressSource contactAddressSource = GetBillingContactAddressSource(graph, fsServiceOrderRow, billCustomer);

            if (contactAddressSource == null
                || (contactAddressSource != null && string.IsNullOrEmpty(contactAddressSource.BillingSource)))
            {
                throw new PXException(TX.Error.MISSING_CUSTOMER_BILLING_ADDRESS_SOURCE);
            }

            IAddress addressRow = null;
            IContact contactRow = null;

            switch (contactAddressSource.BillingSource)
            {
                case ID.Send_Invoices_To.BILLING_CUSTOMER_BILL_TO:

                    contactRow = GetIContact(
                        PXSelect<Contact,
                        Where<
                            Contact.contactID, Equal<Required<Contact.contactID>>>>
                        .Select(graph, billCustomer.DefBillContactID));

                    addressRow = GetIAddress(
                        PXSelect<Address,
                        Where<
                            Address.addressID, Equal<Required<Address.addressID>>>>
                        .Select(graph, billCustomer.DefBillAddressID));

                    break;
                case ID.Send_Invoices_To.SO_BILLING_CUSTOMER_LOCATION:

                    PXResult<Location, Contact, Address> locData = GetContactAndAddressFromLocation(graph, billLocationID);

                    if (locData != null)
                    {
                        addressRow = GetIAddress(locData);
                        contactRow = GetIContact(locData);
                    }

                    break;
                case ID.Send_Invoices_To.SERVICE_ORDER_ADDRESS:
                    GetSrvOrdContactAddress(graph, fsServiceOrderRow, out FSContact fsContact, out FSAddress fsAddress);
                    contactRow = fsContact;
                    addressRow = fsAddress;

                    break;
                default:
                    PXResult<Location, Customer, Contact, Address> defaultLocData = GetContactAddressFromDefaultLocation(graph, billCustomerID);

                    if (defaultLocData != null)
                    {
                        addressRow = GetIAddress(defaultLocData);
                        contactRow = GetIContact(defaultLocData);
                    }

                    break;
            }

            if (addressRow == null)
            {
                throw new PXException(PXMessages.LocalizeFormatNoPrefix(TX.Error.ADDRESS_CONTACT_CANNOT_BE_NULL, TX.MessageParm.ADDRESS), PXErrorLevel.Error);
            }

            if (contactRow == null)
            {
                throw new PXException(PXMessages.LocalizeFormatNoPrefix(TX.Error.ADDRESS_CONTACT_CANNOT_BE_NULL, TX.MessageParm.CONTACT), PXErrorLevel.Error);
            }

            if (graph is SOOrderEntry)
            {
                SOOrderEntry SOgraph = (SOOrderEntry)graph;

                SOBillingContact billContact = new SOBillingContact();
                SOBillingAddress billAddress = new SOBillingAddress();

                InvoiceHelper.CopyContact(billContact, contactRow);
                billContact.CustomerID = SOgraph.customer.Current.BAccountID;
                billContact.RevisionID = 0;


                InvoiceHelper.CopyAddress(billAddress, addressRow);
                billAddress.CustomerID = SOgraph.customer.Current.BAccountID;
                billAddress.CustomerAddressID = SOgraph.customer.Current.DefAddressID;
                billAddress.RevisionID = 0;

                billContact.IsDefaultContact = false;
                billAddress.IsDefaultAddress = false;

                SOgraph.Billing_Contact.Current = billContact = SOgraph.Billing_Contact.Insert(billContact);
                SOgraph.Billing_Address.Current = billAddress = SOgraph.Billing_Address.Insert(billAddress);

                SOgraph.Document.Current.BillAddressID = billAddress.AddressID;
                SOgraph.Document.Current.BillContactID = billContact.ContactID;

                addressRow = null;
                contactRow = null;

                GetShippingContactAddress(graph, contactAddressSource.ShippingSource, billCustomerID, fsServiceOrderRow, out contactRow, out addressRow);

                if (addressRow == null)
                {
                    throw new PXException(PXMessages.LocalizeFormatNoPrefix(TX.Error.ADDRESS_CONTACT_CANNOT_BE_NULL, TX.WildCards.SHIPPING_ADDRESS), PXErrorLevel.Error);
                }

                if (contactRow == null)
                {
                    throw new PXException(PXMessages.LocalizeFormatNoPrefix(TX.Error.ADDRESS_CONTACT_CANNOT_BE_NULL, TX.WildCards.SHIPPING_CONTACT), PXErrorLevel.Error);
                }

                SOShippingContact shipContact = new SOShippingContact();
                SOShippingAddress shipAddress = new SOShippingAddress();

                InvoiceHelper.CopyContact(shipContact, contactRow);
                shipContact.CustomerID = SOgraph.customer.Current.BAccountID;
                shipContact.RevisionID = 0;

                InvoiceHelper.CopyAddress(shipAddress, addressRow);
                shipAddress.CustomerID = SOgraph.customer.Current.BAccountID;
                shipAddress.CustomerAddressID = SOgraph.customer.Current.DefAddressID;
                shipAddress.RevisionID = 0;

                shipContact.IsDefaultContact = false;
                shipAddress.IsDefaultAddress = false;

                SOgraph.Shipping_Contact.Current = shipContact = SOgraph.Shipping_Contact.Insert(shipContact);
                SOgraph.Shipping_Address.Current = shipAddress = SOgraph.Shipping_Address.Insert(shipAddress);

                SOgraph.Document.Current.ShipAddressID = shipAddress.AddressID;
                SOgraph.Document.Current.ShipContactID = shipContact.ContactID;
            }
            else if (graph is ARInvoiceEntry)
            {
                ARInvoiceEntry ARgraph = (ARInvoiceEntry)graph;

                ARContact arContact = new ARContact();
                ARAddress arAddress = new ARAddress();

                InvoiceHelper.CopyContact(arContact, contactRow);
                arContact.CustomerID = ARgraph.customer.Current.BAccountID;
                arContact.RevisionID = 0;
                arContact.IsDefaultContact = false;

                InvoiceHelper.CopyAddress(arAddress, addressRow);
                arAddress.CustomerID = ARgraph.customer.Current.BAccountID;
                arAddress.CustomerAddressID = ARgraph.customer.Current.DefAddressID;
                arAddress.RevisionID = 0;
                arAddress.IsDefaultBillAddress = false;

                ARgraph.Billing_Contact.Current = arContact = ARgraph.Billing_Contact.Update(arContact);
                ARgraph.Billing_Address.Current = arAddress = ARgraph.Billing_Address.Update(arAddress);

                ARgraph.Document.Current.BillAddressID = arAddress.AddressID;
                ARgraph.Document.Current.BillContactID = arContact.ContactID;

                addressRow = null;
                contactRow = null;

                GetShippingContactAddress(graph, contactAddressSource.ShippingSource, billCustomerID, fsServiceOrderRow, out contactRow, out addressRow);

                if (addressRow == null)
                {
                    throw new PXException(PXMessages.LocalizeFormatNoPrefix(TX.Error.ADDRESS_CONTACT_CANNOT_BE_NULL, TX.WildCards.SHIPPING_ADDRESS), PXErrorLevel.Error);
                }

                if (contactRow == null)
                {
                    throw new PXException(PXMessages.LocalizeFormatNoPrefix(TX.Error.ADDRESS_CONTACT_CANNOT_BE_NULL, TX.WildCards.SHIPPING_CONTACT), PXErrorLevel.Error);
                }

                ARShippingContact shipContact = new ARShippingContact();
                ARShippingAddress shipAddress = new ARShippingAddress();

                InvoiceHelper.CopyContact(shipContact, contactRow);
                shipContact.CustomerID = ARgraph.customer.Current.BAccountID;
                shipContact.RevisionID = 0;

                InvoiceHelper.CopyAddress(shipAddress, addressRow);
                shipAddress.CustomerID = ARgraph.customer.Current.BAccountID;
                shipAddress.CustomerAddressID = ARgraph.customer.Current.DefAddressID;
                shipAddress.RevisionID = 0;

                shipContact.IsDefaultContact = false;
                shipAddress.IsDefaultAddress = false;

                ARgraph.Shipping_Contact.Current = shipContact = ARgraph.Shipping_Contact.Insert(shipContact);
                ARgraph.Shipping_Address.Current = shipAddress = ARgraph.Shipping_Address.Insert(shipAddress);

                ARgraph.Document.Current.ShipAddressID = shipAddress.AddressID;
                ARgraph.Document.Current.ShipContactID = shipContact.ContactID;
            }
        }

        public virtual ContactAddressSource GetBillingContactAddressSource(PXGraph graph, FSServiceOrder fsServiceOrderRow, Customer billCustomer)
        {
            FSSetup fSSetupRow = PXSelect<FSSetup>.Select(graph);
            ContactAddressSource contactAddressSource = null;

            if (fSSetupRow != null)
            {
                contactAddressSource = new ContactAddressSource();

                if (fSSetupRow.CustomerMultipleBillingOptions == true)
                {
                    FSCustomerBillingSetup customerBillingSetup = PXSelect<FSCustomerBillingSetup,
                                                                  Where<
                                                                      FSCustomerBillingSetup.customerID, Equal<Required<FSCustomerBillingSetup.customerID>>,
                                                                  And<
                                                                      FSCustomerBillingSetup.srvOrdType, Equal<Required<FSCustomerBillingSetup.srvOrdType>>,
                                                                  And<
                                                                      FSCustomerBillingSetup.active, Equal<True>>>>>
                                                                  .Select(graph, billCustomer.BAccountID, fsServiceOrderRow.SrvOrdType);

                    if (customerBillingSetup != null)
                    {
                        contactAddressSource.BillingSource = customerBillingSetup.SendInvoicesTo;
                        contactAddressSource.ShippingSource = customerBillingSetup.BillShipmentSource;
                    }
                }
                else if (fSSetupRow.CustomerMultipleBillingOptions == false && billCustomer != null)
                {
                    FSxCustomer fsxCustomerRow = PXCache<Customer>.GetExtension<FSxCustomer>(billCustomer);
                    contactAddressSource.BillingSource = fsxCustomerRow.SendInvoicesTo;
                    contactAddressSource.ShippingSource = fsxCustomerRow.BillShipmentSource;
                }
            }

            return contactAddressSource;
        }

        public virtual void GetShippingContactAddress(PXGraph graph,
                                                      string contactAddressSource,
                                                      int? billCustomerID,
                                                      FSServiceOrder fsServiceOrderRow,
                                                      out IContact contactRow,
                                                      out IAddress addressRow)
        {
            contactRow = null;
            addressRow = null;
            PXResult<Location, Contact, Address> locData = null;

            switch (contactAddressSource)
            {
                // The name of the following constant and its corresponding label
                // does not correspond with the data sought.
                case ID.Ship_To.BILLING_CUSTOMER_BILL_TO:
                    PXResult<Location, Customer, Contact, Address> defaultLocData = null;
                    defaultLocData = GetContactAddressFromDefaultLocation(graph, billCustomerID);

                    contactRow = GetIContact(defaultLocData);
                    addressRow = GetIAddress(defaultLocData);

                    break;

                case ID.Ship_To.SERVICE_ORDER_ADDRESS:
                    GetSrvOrdContactAddress(graph, fsServiceOrderRow, out FSContact fsContact, out FSAddress fsAddress);
                    contactRow = fsContact;
                    addressRow = fsAddress;

                    break;

                case ID.Ship_To.SO_CUSTOMER_LOCATION:
                    locData = GetContactAndAddressFromLocation(graph, fsServiceOrderRow.LocationID);
                    contactRow = GetIContact(locData);
                    addressRow = GetIAddress(locData);

                    break;

                case ID.Ship_To.SO_BILLING_CUSTOMER_LOCATION:
                    locData = GetContactAndAddressFromLocation(graph, fsServiceOrderRow.BillLocationID);
                    contactRow = GetIContact(locData);
                    addressRow = GetIAddress(locData);

                    break;
            }
        }

        public virtual IAddress GetIAddress(Address source)
        {
            if (source == null)
            {
                return null;
            }

            var dest = new CRAddress();

            dest.BAccountID = source.BAccountID;
            dest.RevisionID = source.RevisionID;
            dest.IsDefaultAddress = false;
            dest.AddressLine1 = source.AddressLine1;
            dest.AddressLine2 = source.AddressLine2;
            dest.AddressLine3 = source.AddressLine3;
            dest.City = source.City;
            dest.CountryID = source.CountryID;
            dest.State = source.State;
            dest.PostalCode = source.PostalCode;

            dest.IsValidated = source.IsValidated;

            return dest;
        }

        public virtual IContact GetIContact(Contact source)
        {
            if (source == null)
            {
                return null;
            }

            var dest = new CRContact();

            dest.BAccountID = source.BAccountID;
            dest.RevisionID = source.RevisionID;
            dest.IsDefaultContact = false;
            dest.FullName = source.FullName;
            dest.Salutation = source.Salutation;
            dest.Title = source.Title;
            dest.Phone1 = source.Phone1;
            dest.Phone1Type = source.Phone1Type;
            dest.Phone2 = source.Phone2;
            dest.Phone2Type = source.Phone2Type;
            dest.Phone3 = source.Phone3;
            dest.Phone3Type = source.Phone3Type;
            dest.Fax = source.Fax;
            dest.FaxType = source.FaxType;
            dest.Email = source.EMail;
            dest.NoteID = null;

            dest.Attention = source.Attention;

            return dest;
        }
        #endregion

        #region Invoice Logics
        /// <summary>
        /// Sets the SubID for AR or SalesSubID for SO.
        /// </summary>
        public virtual void SetCombinedSubID(PXGraph graph,
                                            PXCache sender,
                                            ARTran tranARRow,
                                            APTran tranAPRow,
                                            SOLine tranSORow,
                                            FSSetup fsSetupRow,
                                            int? branchID,
                                            int? inventoryID,
                                            int? customerLocationID,
                                            int? branchLocationID)
        {
            if (branchID == null || inventoryID == null || customerLocationID == null || branchLocationID == null)
            {
                throw new PXException(TX.Error.SOME_SUBACCOUNT_SEGMENT_SOURCE_IS_NOT_SPECIFIED);
            }

            if ((tranARRow != null && tranARRow.AccountID != null) || (tranAPRow != null && tranAPRow.AccountID != null) || (tranSORow != null && tranSORow.SalesAcctID != null))
            {
                InventoryItem inventoryItemRow = InventoryItem.PK.Find(graph, inventoryID);

                Location companyLocationRow = PXSelectJoin<Location,
                                              InnerJoin<BAccountR,
                                              On<
                                                  Location.bAccountID, Equal<BAccountR.bAccountID>,
                                                  And<Location.locationID, Equal<BAccountR.defLocationID>>>,
                                              InnerJoin<GL.Branch,
                                              On<
                                                  BAccountR.bAccountID, Equal<GL.Branch.bAccountID>>>>,
                                              Where<
                                                  GL.Branch.branchID, Equal<Required<ARTran.branchID>>>>
                                              .Select(graph, branchID);

                Location customerLocationRow = PXSelect<Location,
                                               Where<
                                                   Location.locationID, Equal<Required<Location.locationID>>>>
                                               .Select(graph, customerLocationID);

                FSBranchLocation fsBranchLocationRow = FSBranchLocation.PK.Find(graph, branchLocationID);

                int? customer_SubID = customerLocationRow.CSalesSubID;
                int? item_SubID = inventoryItemRow.SalesSubID;
                int? company_SubID = companyLocationRow.CMPSalesSubID;
                int? branchLocation_SubID = fsBranchLocationRow.SubID;

                object value;

                try
                {
                    if (tranARRow != null)
                    {
                        value = SubAccountMaskAttribute.MakeSub<ARSetup.salesSubMask>(graph,
                                                                                      fsSetupRow.ContractCombineSubFrom,
                                                                                      new object[] { customer_SubID, item_SubID, company_SubID, branchLocation_SubID },
                                                                                      new Type[] { typeof(Location.cSalesSubID), typeof(InventoryItem.salesSubID), typeof(Location.cMPSalesSubID), typeof(FSBranchLocation.subID) },
                                                                                      true);

                        sender.RaiseFieldUpdating<ARTran.subID>(tranARRow, ref value);
                        tranARRow.SubID = (int?)value;
                    }
                    else if (tranAPRow != null)
                    {
                        value = SubAccountMaskAttribute.MakeSub<APSetup.expenseSubMask>(graph,
                                                                                        fsSetupRow.ContractCombineSubFrom,
                                                                                        new object[] { customer_SubID, item_SubID, company_SubID, branchLocation_SubID },
                                                                                        new Type[] { typeof(Location.cSalesSubID), typeof(InventoryItem.salesSubID), typeof(Location.cMPSalesSubID), typeof(FSBranchLocation.subID) },
                                                                                        true);

                        sender.RaiseFieldUpdating<APTran.subID>(tranSORow, ref value);
                        tranAPRow.SubID = (int?)value;
                    }
                    else if (tranSORow != null)
                    {
                        value = SubAccountMaskAttribute.MakeSub<SOOrderType.salesSubMask>(graph,
                                                                                          fsSetupRow.ContractCombineSubFrom,
                                                                                          new object[] { customer_SubID, item_SubID, company_SubID, branchLocation_SubID },
                                                                                          new Type[] { typeof(Location.cSalesSubID), typeof(InventoryItem.salesSubID), typeof(Location.cMPSalesSubID), typeof(FSBranchLocation.subID) },
                                                                                          true);

                        sender.RaiseFieldUpdating<SOLine.salesSubID>(tranSORow, ref value);
                        tranSORow.SalesSubID = (int?)value;
                    }
                }
                catch (PXException)
                {
                    if (tranARRow != null)
                    {
                        tranARRow.SubID = null;
                    }
                    else if (tranSORow != null)
                    {
                        tranSORow.SalesSubID = null;
                    }
                }
            }
        }

        /// <summary>
        /// Sets the SubID for AR or SalesSubID for SO.
        /// </summary>
        public virtual void SetCombinedSubID(PXGraph graph,
                                            PXCache sender,
                                            ARTran tranARRow,
                                            APTran tranAPRow,
                                            SOLine tranSORow,
                                            FSSrvOrdType fsSrvOrdTypeRow,
                                            int? branchID,
                                            int? inventoryID,
                                            int? customerLocationID,
                                            int? branchLocationID,
                                            int? salesPersonID,
                                            bool isService)
        {
            if (string.IsNullOrEmpty(fsSrvOrdTypeRow.CombineSubFrom) == true)
            {
                throw new PXException(TX.Error.SALES_SUB_MASK_UNDEFINED_IN_SERVICE_ORDER_TYPE, fsSrvOrdTypeRow.SrvOrdType);
            }

            if (branchID == null || inventoryID == null || customerLocationID == null || branchLocationID == null)
            {
                throw new PXException(TX.Error.SOME_SUBACCOUNT_SEGMENT_SOURCE_IS_NOT_SPECIFIED);
            }

            if ((tranARRow != null && tranARRow.AccountID != null) || (tranAPRow != null && tranAPRow.AccountID != null) || (tranSORow != null && tranSORow.SalesAcctID != null))
            {
                SharedClasses.SubAccountIDTupla subAcctIDs = SharedFunctions.GetSubAccountIDs(graph, fsSrvOrdTypeRow, inventoryID, branchID, customerLocationID, branchLocationID, salesPersonID, isService);

                object value;

                try
                {
                    if (tranARRow != null)
                    {
                        value = SubAccountMaskAttribute.MakeSub<ARSetup.salesSubMask>(graph,
                                                                                      fsSrvOrdTypeRow.CombineSubFrom,
                                                                                      new object[] { subAcctIDs.branchLocation_SubID, subAcctIDs.branch_SubID, subAcctIDs.inventoryItem_SubID, subAcctIDs.customerLocation_SubID, subAcctIDs.postingClass_SubID, subAcctIDs.salesPerson_SubID, subAcctIDs.srvOrdType_SubID, subAcctIDs.warehouse_SubID },
                                                                                      new Type[] { typeof(FSBranchLocation.subID), typeof(Location.cMPSalesSubID), typeof(InventoryItem.salesSubID), typeof(Location.cSalesSubID), typeof(INPostClass.salesSubID), typeof(SalesPerson.salesSubID), typeof(FSSrvOrdType.subID), isService ? typeof(INSite.salesSubID) : typeof(InventoryItem.salesSubID) });

                        sender.RaiseFieldUpdating<ARTran.subID>(tranARRow, ref value);
                        tranARRow.SubID = (int?)value;
                    }
                    else if (tranAPRow != null)
                    {
                        value = SubAccountMaskAttribute.MakeSub<APSetup.expenseSubMask>(graph,
                                                                                        fsSrvOrdTypeRow.CombineSubFrom,
                                                                                        new object[] { subAcctIDs.branchLocation_SubID, subAcctIDs.branch_SubID, subAcctIDs.inventoryItem_SubID, subAcctIDs.customerLocation_SubID, subAcctIDs.postingClass_SubID, subAcctIDs.salesPerson_SubID, subAcctIDs.srvOrdType_SubID, subAcctIDs.warehouse_SubID },
                                                                                        new Type[] { typeof(FSBranchLocation.subID), typeof(Location.cMPSalesSubID), typeof(InventoryItem.salesSubID), typeof(Location.cSalesSubID), typeof(INPostClass.salesSubID), typeof(SalesPerson.salesSubID), typeof(FSSrvOrdType.subID), isService ? typeof(INSite.salesSubID) : typeof(InventoryItem.salesSubID) });

                        sender.RaiseFieldUpdating<APTran.subID>(tranSORow, ref value);
                        tranAPRow.SubID = (int?)value;
                    }
                    else if (tranSORow != null)
                    {
                        value = SubAccountMaskAttribute.MakeSub<SOOrderType.salesSubMask>(graph,
                                                                                          fsSrvOrdTypeRow.CombineSubFrom,
                                                                                          new object[] { subAcctIDs.branchLocation_SubID, subAcctIDs.branch_SubID, subAcctIDs.inventoryItem_SubID, subAcctIDs.customerLocation_SubID, subAcctIDs.postingClass_SubID, subAcctIDs.salesPerson_SubID, subAcctIDs.srvOrdType_SubID, subAcctIDs.warehouse_SubID },
                                                                                          new Type[] { typeof(FSBranchLocation.subID), typeof(Location.cMPSalesSubID), typeof(InventoryItem.salesSubID), typeof(Location.cSalesSubID), typeof(INPostClass.salesSubID), typeof(SalesPerson.salesSubID), typeof(FSSrvOrdType.subID), isService ? typeof(INSite.salesSubID) : typeof(InventoryItem.salesSubID) });

                        sender.RaiseFieldUpdating<SOLine.salesSubID>(tranSORow, ref value);
                        tranSORow.SalesSubID = (int?)value;
                    }
                }
                catch (PXException)
                {
                    if (tranARRow != null)
                    {
                        tranARRow.SubID = null;
                    }
                    else if (tranSORow != null)
                    {
                        tranSORow.SalesSubID = null;
                    }
                }
            }
        }

        /// <summary>
        /// Returns the TermID from the Vendor or Customer.
        /// </summary>
        public virtual string GetTermsIDFromCustomerOrVendor(PXGraph graph, int? customerID, int? vendorID)
        {
            if (customerID != null)
            {
                Customer customerRow = PXSelect<Customer,
                                       Where<
                                            Customer.bAccountID, Equal<Required<Customer.bAccountID>>>>
                                       .Select(graph, customerID);

                return customerRow?.TermsID;
            }
            else if (vendorID != null)
            {
                Vendor vendorRow = PXSelect<Vendor,
                                   Where<
                                        Vendor.bAccountID, Equal<Required<Vendor.bAccountID>>>>
                                   .Select(graph, vendorID);

                return vendorRow?.TermsID;
            }

            return null;
        }

        public virtual Exception GetErrorInfoInLines(List<ErrorInfo> errorInfoList, Exception e)
        {
            StringBuilder errorMsgBuilder = new StringBuilder();

            errorMsgBuilder.Append(e.Message.EnsureEndsWithDot() + " ");

            foreach (ErrorInfo errorInfo in errorInfoList)
            {
                errorMsgBuilder.Append(errorInfo.ErrorMessage.EnsureEndsWithDot() + " ");
            }

            return new PXException(errorMsgBuilder.ToString().TrimEnd());
        }
 
        /// <summary>
        /// Cleans the posting information <c>(FSCreatedDoc, FSPostRegister, FSPostDoc, FSPostInfo, FSPostDet, FSPostBatch)</c> for the document created by the billing process.
        /// </summary>
        public virtual void CleanPostingInfoLinkedToDoc(object createdDoc)
        {
            if (createdDoc == null)
            {
                return;
            }

            PXGraph cleanerGraph = new PXGraph();

            string postTo = null;
            string createdDocType = null;
            string createdRefNbr = null;
            PXResultset<FSPostDet> fsPostDetRows = null;
            bool needToCheckForAnotherCreatedDoc = false;

            if (createdDoc is SOOrder)
            {
                postTo = ID.Batch_PostTo.SO;

                var soOrder = (SOOrder)createdDoc;
                createdDocType = soOrder.OrderType;
                createdRefNbr = soOrder.RefNbr;

                fsPostDetRows = PXSelect<FSPostDet,
                                Where<FSPostDet.sOOrderType, Equal<Required<FSPostDet.sOOrderType>>,
                                    And<FSPostDet.sOOrderNbr, Equal<Required<FSPostDet.sOOrderNbr>>>>>
                                .Select(cleanerGraph, createdDocType, createdRefNbr);
            }
            else if (createdDoc is SOInvoice)
            {
                postTo = ID.Batch_PostTo.SI;

                var soInvoice = (SOInvoice)createdDoc;
                createdDocType = soInvoice.DocType;
                createdRefNbr = soInvoice.RefNbr;

                fsPostDetRows = PXSelect<FSPostDet,
                                Where<FSPostDet.sOInvDocType, Equal<Required<FSPostDet.sOInvDocType>>,
                                    And<FSPostDet.sOInvRefNbr, Equal<Required<FSPostDet.sOInvRefNbr>>>>>
                                .Select(cleanerGraph, createdDocType, createdRefNbr);
            }
            else if (createdDoc is ARInvoice)
            {
                postTo = ID.Batch_PostTo.AR;

                var arInvoice = (ARInvoice)createdDoc;
                createdDocType = arInvoice.DocType;
                createdRefNbr = arInvoice.RefNbr;

                fsPostDetRows = PXSelect<FSPostDet,
                                Where<FSPostDet.arDocType, Equal<Required<FSPostDet.arDocType>>,
                                    And<FSPostDet.arRefNbr, Equal<Required<FSPostDet.arRefNbr>>>>>
                                .Select(cleanerGraph, createdDocType, createdRefNbr);
            }
            else if (createdDoc is APInvoice)
            {
                postTo = ID.Batch_PostTo.AP;

                var apInvoice = (APInvoice)createdDoc;
                createdDocType = apInvoice.DocType;
                createdRefNbr = apInvoice.RefNbr;

                fsPostDetRows = PXSelect<FSPostDet,
                                Where<FSPostDet.apDocType, Equal<Required<FSPostDet.apDocType>>,
                                    And<FSPostDet.apRefNbr, Equal<Required<FSPostDet.apRefNbr>>>>>
                                .Select(cleanerGraph, createdDocType, createdRefNbr);
            }
            else if (createdDoc is PMRegister)
            {
                postTo = ID.Batch_PostTo.PM;

                var pmRegister = (PMRegister)createdDoc;
                createdDocType = pmRegister.Module;
                createdRefNbr = pmRegister.RefNbr;

                fsPostDetRows = PXSelect<FSPostDet,
                                Where<FSPostDet.pMDocType, Equal<Required<FSPostDet.pMDocType>>,
                                    And<FSPostDet.pMRefNbr, Equal<Required<FSPostDet.pMRefNbr>>>>>
                                .Select(cleanerGraph, createdDocType, createdRefNbr);

                needToCheckForAnotherCreatedDoc = true;
            }
            else if (createdDoc is INRegister)
            {
                postTo = ID.Batch_PostTo.IN;

                var inRegister = (INRegister)createdDoc;
                createdDocType = inRegister.DocType;
                createdRefNbr = inRegister.RefNbr;

                fsPostDetRows = PXSelect<FSPostDet,
                                Where<FSPostDet.iNDocType, Equal<Required<FSPostDet.iNDocType>>,
                                    And<FSPostDet.iNRefNbr, Equal<Required<FSPostDet.iNRefNbr>>>>>
                                .Select(cleanerGraph, createdDocType, createdRefNbr);

                needToCheckForAnotherCreatedDoc = true;
            }
            else
            {
                throw new NotImplementedException();
            }

            // We get the FSPostRegister rows before delete them
            // because we need them to update the ServiceOrders/Appointments
            List<FSPostRegister> postRegisterRows = PXSelect<FSPostRegister,
                    Where<FSPostRegister.postedTO, Equal<Required<FSPostRegister.postedTO>>,
                        And<FSPostRegister.postDocType, Equal<Required<FSPostRegister.postDocType>>,
                        And<FSPostRegister.postRefNbr, Equal<Required<FSPostRegister.postRefNbr>>>>>>.
                    Select(cleanerGraph, postTo, createdDocType, createdRefNbr).RowCast<FSPostRegister>().ToList();

            PXDatabase.Delete<FSCreatedDoc>(
                new PXDataFieldRestrict<FSCreatedDoc.postTo>(postTo),
                new PXDataFieldRestrict<FSCreatedDoc.createdDocType>(createdDocType),
                new PXDataFieldRestrict<FSCreatedDoc.createdRefNbr>(createdRefNbr));

            PXDatabase.Delete<FSPostRegister>(
                new PXDataFieldRestrict<FSPostRegister.postedTO>(postTo),
                new PXDataFieldRestrict<FSPostRegister.postDocType>(createdDocType),
                new PXDataFieldRestrict<FSPostRegister.postRefNbr>(createdRefNbr));

            PXDatabase.Delete<FSPostDoc>(
                new PXDataFieldRestrict<FSPostDoc.postedTO>(postTo),
                new PXDataFieldRestrict<FSPostDoc.postDocType>(createdDocType),
                new PXDataFieldRestrict<FSPostDoc.postRefNbr>(createdRefNbr));

            AppointmentEntry apptGraph = PXGraph.CreateInstance<AppointmentEntry>();

            //FSPostInfo Details should be cleared before header flags.
            if (fsPostDetRows != null && fsPostDetRows.Count > 0)
            {
                if (fsPostDetRows.RowCast<FSPostDet>().GroupBy(e => e.BatchID).Count() > 1)
                {
                    throw new NotImplementedException();
                }

                int? batchID = fsPostDetRows.FirstOrDefault().GetItem<FSPostDet>().BatchID;

                PXCache postDetCache = cleanerGraph.Caches[typeof(FSPostDet)];

                foreach (FSPostDet fsPostDetRow in fsPostDetRows)
                {
                    CleanPostInfo(cleanerGraph, fsPostDetRow);

                    postDetCache.Delete(fsPostDetRow);
                }

                postDetCache.Persist(PXDBOperation.Delete);

                UpdatePostingBatch(cleanerGraph, batchID);
            }

            foreach (FSPostRegister postRegister in postRegisterRows)
            {
                FSPostRegister otherPostRegister = null;

                if (needToCheckForAnotherCreatedDoc == true)
                {
                    otherPostRegister = PXSelect<FSPostRegister,
                        Where2<
                            Where<FSPostRegister.entityType, Equal<Required<FSPostRegister.entityType>>,
                                And<FSPostRegister.srvOrdType, Equal<Required<FSPostRegister.srvOrdType>>,
                                And<FSPostRegister.refNbr, Equal<Required<FSPostRegister.refNbr>>>>>,
                            And<Where<FSPostRegister.postedTO, NotEqual<Required<FSPostRegister.postedTO>>,
                                Or<FSPostRegister.postDocType, NotEqual<Required<FSPostRegister.postDocType>>,
                                Or<FSPostRegister.postRefNbr, NotEqual<Required<FSPostRegister.postRefNbr>>>>>>>>.
                        Select(cleanerGraph,
                                postRegister.EntityType, postRegister.SrvOrdType, postRegister.RefNbr,
                                postRegister.PostedTO, postRegister.PostDocType, postRegister.PostRefNbr);
                }

                if (otherPostRegister == null)
                {
                    CleanPostingInfoOnServiceOrderAppointment(cleanerGraph, apptGraph, postRegister);
                }
            }
        }

        public static PXResultset<FSPostRegister> GetOtherPostRegistersRelated(PXGraph graph, string srvOrdType, string serviceOrderRefNbr, FSPostRegister postRegisterToIgnore)
        {
            if (srvOrdType == null || serviceOrderRefNbr == null)
            {
                return null;
            }

            string postTo = null;
            string createdDocType = null;
            string createdRefNbr = null;

            if (postRegisterToIgnore != null)
            {
                postTo = postRegisterToIgnore.PostedTO;
                createdDocType = postRegisterToIgnore.PostDocType;
                createdRefNbr = postRegisterToIgnore.PostRefNbr;
            }

            return PXSelectJoin<FSPostRegister,
                    LeftJoin<FSAppointment,
                        On<FSPostRegister.entityType, Equal<FSPostRegister.entityType.Values.Appointment>,
                            And<FSAppointment.srvOrdType, Equal<FSPostRegister.srvOrdType>,
                            And<FSAppointment.refNbr, Equal<FSPostRegister.refNbr>>>>>,
                    Where2<
                        Where<
                            Where2<
                                Where<FSPostRegister.entityType, Equal<FSPostRegister.entityType.Values.Service_Order>,
                                    And<FSPostRegister.srvOrdType, Equal<Required<FSPostRegister.srvOrdType>>,
                                    And<FSPostRegister.refNbr, Equal<Required<FSPostRegister.refNbr>>>>>,
                                Or<Where<FSAppointment.refNbr, IsNotNull,
                                    And<FSAppointment.srvOrdType, Equal<Required<FSAppointment.srvOrdType>>,
                                    And<FSAppointment.soRefNbr, Equal<Required<FSAppointment.soRefNbr>>>>>>>>,
                        And<Where<FSPostRegister.postedTO, NotEqual<Required<FSPostRegister.postedTO>>,
                            Or<FSPostRegister.postDocType, NotEqual<Required<FSPostRegister.postDocType>>,
                            Or<FSPostRegister.postRefNbr, NotEqual<Required<FSPostRegister.postRefNbr>>>>>>>>.
                    Select(graph,
                            srvOrdType, serviceOrderRefNbr,
                            srvOrdType, serviceOrderRefNbr,
                            postTo, createdDocType, createdRefNbr);
        }

        private static void CleanPostingInfoOnServiceOrderAppointment(PXGraph graph,
                                                                      AppointmentEntry apptGraph,
                                                                      FSPostRegister postRegister)
        {
            if (postRegister == null)
            {
                return;
            }

            string entityType = postRegister.EntityType;
            string srvOrdType = postRegister.SrvOrdType;
            string refNbr = postRegister.RefNbr;

            if (entityType == FSPostRegister.entityType.Values.APPOINTMENT)
            {
                apptGraph.Clear(PXClearOption.ClearAll);

                FSAppointment appt = apptGraph.AppointmentRecords.Current = apptGraph.AppointmentRecords
                                                                          .Search<FSAppointment.refNbr>
                                                                          (refNbr, srvOrdType);

                appt = (FSAppointment)apptGraph.AppointmentRecords.Cache.CreateCopy(appt);

                appt.PostingStatusAPARSO = ID.Status_Posting.PENDING_TO_POST;
                appt.PendingAPARSOPost = true;

                FSAppointment.Events.Select(ev => ev.AppointmentUnposted).FireOn(apptGraph, appt);
                apptGraph.AppointmentRecords.Cache.Update(appt);
                apptGraph.AppointmentRecords.Cache.SetValue<FSAppointment.finPeriodID>(appt, null);
                apptGraph.Save.Press();

                if (GetOtherPostRegistersRelated(graph, postRegister.SrvOrdType, appt.SORefNbr, postRegister).Count == 0)
                {
                    entityType = FSPostRegister.entityType.Values.SERVICE_ORDER;
                    refNbr = appt.SORefNbr;
                }
                else
                {
                    return;
                }
            }

            if (entityType == FSPostRegister.entityType.Values.SERVICE_ORDER)
            {
                // TODO: Updating the ServiceOrder in this way we are not considering concurrency issues.
                // At the moment of this update a Billing Process By Appointment can be updated the record.

                PXUpdate<
                    Set<FSServiceOrder.finPeriodID, Null,
                    Set<FSServiceOrder.postedBy, Null,
                    Set<FSServiceOrder.pendingAPARSOPost, True,
                    Set<FSServiceOrder.billed, False>>>>,
                FSServiceOrder,
                Where<
                    FSServiceOrder.srvOrdType, Equal<Required<FSServiceOrder.srvOrdType>>,
                    And<FSServiceOrder.refNbr, Equal<Required<FSServiceOrder.refNbr>>>>>
                .Update(graph, srvOrdType, refNbr);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private static void UpdatePostingBatch(PXGraph graph, int? batchID)
        {
            if (batchID == null)
            {
                return;
            }

            var postBatchView = new PXSelect<FSPostBatch,
                    Where<FSPostBatch.batchID, Equal<Required<FSPostBatch.batchID>>>>(graph);
            FSPostBatch postBatchRow = postBatchView.Select(batchID);

            if (postBatchRow == null)
            {
                return;
            }

            PXCache postDetCache = graph.Caches[typeof(FSPostDet)];
            postDetCache.ClearQueryCache();

            PXCache postDocCache = graph.Caches[typeof(FSPostDoc)];
            postDocCache.ClearQueryCache();

            int docCount = PXSelectReadonly<FSPostDoc,
                    Where<FSPostDoc.batchID, Equal<Required<FSPostDoc.batchID>>>>.
                    Select(graph, batchID).Count();

            if (docCount == 0)
            {
                postBatchView.Delete(postBatchRow);
                postBatchView.Cache.Persist(postBatchRow, PXDBOperation.Delete);
            }
            else
            {
                postBatchRow.QtyDoc = docCount;
                postBatchView.Update(postBatchRow);
                postBatchView.Cache.Persist(postBatchRow, PXDBOperation.Update);
            }
        }

        /// <summary>
        /// Cleans the posting information <c>(FSContractPostDoc, FSContractPostDet, FSContractPostBatch, FSContractPostRegister)</c> 
        /// when erasing the entire posted document (SO, AR) coming from a contract.
        /// </summary>
        public virtual void CleanContractPostingInfoLinkedToDoc(object postedDoc)
        {
            if (postedDoc == null)
            {
                return;
            }

            PXGraph cleanerGraph = new PXGraph();

            string createdDocType = null;
            string createdRefNbr = null;
            string postTo = null;

            if (postedDoc is SOOrder)
            {
                var soOrder = (SOOrder)postedDoc;
                createdDocType = soOrder.OrderType;
                createdRefNbr = soOrder.RefNbr;
                postTo = ID.Contract_PostTo.SALES_ORDER_MODULE;
            }
            else if (postedDoc is ARInvoice)
            {
                var arInvoice = (ARInvoice)postedDoc;
                createdDocType = arInvoice.DocType;
                createdRefNbr = arInvoice.RefNbr;
                postTo = ID.Contract_PostTo.ACCOUNTS_RECEIVABLE_MODULE;
            }
            else
            {
                throw new NotImplementedException();
            }

            PXResultset<FSContractPostDoc> fsContractPostDocRow = PXSelect<FSContractPostDoc,
                                                                  Where<
                                                                      FSContractPostDoc.postDocType, Equal<Required<FSContractPostDoc.postDocType>>,
                                                                  And<
                                                                      FSContractPostDoc.postRefNbr, Equal<Required<FSContractPostDoc.postRefNbr>>,
                                                                  And<
                                                                      FSContractPostDoc.postedTO, Equal<Required<FSContractPostDoc.postedTO>>>>>>
                                                                  .Select(cleanerGraph, createdDocType, createdRefNbr, postTo);

            if (fsContractPostDocRow.Count > 0)
            {
                int? contractPostBatchID = fsContractPostDocRow.FirstOrDefault().GetItem<FSContractPostDoc>().ContractPostBatchID;
                int? contractPostDocID = fsContractPostDocRow.FirstOrDefault().GetItem<FSContractPostDoc>().ContractPostDocID;
                int? serviceContractID = fsContractPostDocRow.FirstOrDefault().GetItem<FSContractPostDoc>().ServiceContractID;
                int? contractPeriodID = fsContractPostDocRow.FirstOrDefault().GetItem<FSContractPostDoc>().ContractPeriodID;

                PXDatabase.Delete<FSContractPostRegister>(
                    new PXDataFieldRestrict<FSContractPostRegister.contractPostBatchID>(contractPostBatchID),
                    new PXDataFieldRestrict<FSContractPostRegister.postedTO>(postTo),
                    new PXDataFieldRestrict<FSContractPostRegister.postDocType>(createdDocType),
                    new PXDataFieldRestrict<FSContractPostRegister.postRefNbr>(createdRefNbr));

                PXDatabase.Delete<FSContractPostDoc>(
                    new PXDataFieldRestrict<FSContractPostDoc.contractPostDocID>(contractPostDocID),
                    new PXDataFieldRestrict<FSContractPostDoc.postedTO>(postTo),
                    new PXDataFieldRestrict<FSContractPostDoc.postDocType>(createdDocType),
                    new PXDataFieldRestrict<FSContractPostDoc.postRefNbr>(createdRefNbr));

                PXDatabase.Delete<FSContractPostDet>(
                    new PXDataFieldRestrict<FSContractPostDet.contractPostDocID>(contractPostDocID));

                PXUpdate<
                    Set<FSContractPeriod.invoiced, False,
                    Set<FSContractPeriod.status, ListField_Status_ContractPeriod.Pending>>,
                FSContractPeriod,
                Where<
                    FSContractPeriod.serviceContractID, Equal<Required<FSContractPeriod.serviceContractID>>,
                And<
                    FSContractPeriod.contractPeriodID, Equal<Required<FSContractPeriod.contractPeriodID>>>>>
                .Update(cleanerGraph, serviceContractID, contractPeriodID);

                ContractPostBatchMaint contractPostBatchgraph = PXGraph.CreateInstance<ContractPostBatchMaint>();
                contractPostBatchgraph.ContractBatchRecords.Current = contractPostBatchgraph.ContractBatchRecords.Search<FSContractPostDoc.contractPostBatchID>(contractPostBatchID);

                if (contractPostBatchgraph.ContractPostDocRecords.Select().Count() == 0)
                {
                    contractPostBatchgraph.ContractBatchRecords.Delete(contractPostBatchgraph.ContractBatchRecords.Current);
                }

                contractPostBatchgraph.Save.Press();
            }
        }

        public virtual void CreateBillHistoryRowsForDocument(PXGraph graph,
                                string childEntityType, string childDocType, string childRefNbr,
                                string parentEntityType, string parentDocType, string parentRefNbr)
        {
            var serviceContractGroups = PXSelect<FSARTran,
                    Where<FSARTran.tranType, Equal<Required<FSARTran.tranType>>,
                        And<FSARTran.refNbr, Equal<Required<FSARTran.refNbr>>>>>
                    .Select(graph, childDocType, childRefNbr)
                    .RowCast<FSARTran>()
                    .GroupBy(x => x.ServiceContractRefNbr);

            if (serviceContractGroups.Count() > 0)
            {
                PXCache cacheFSBillHistory = graph.Caches[typeof(FSBillHistory)];
                PXCache cacheFSARTran = graph.Caches[typeof(FSARTran)];

                foreach (var serviceContractGroup in serviceContractGroups)
                {
                    var serviceContract = serviceContractGroup.First();

                    if (serviceContract.ServiceContractRefNbr != null)
                    {
                        FSARTran tempFSARTran = (FSARTran)cacheFSARTran.CreateCopy(serviceContract);
                        tempFSARTran.SrvOrdType = null;
                        tempFSARTran.ServiceOrderRefNbr = null;
                        tempFSARTran.AppointmentRefNbr = null;

                        CreateBillHistoryRowsFromARTran(cacheFSBillHistory, tempFSARTran,
                                    childEntityType, childDocType, childRefNbr,
                                    parentEntityType, parentDocType, parentRefNbr,
                                    false);

                        if (parentEntityType != null && parentDocType != null && parentRefNbr != null)
                        {
                            var billHistoryRows = PXSelect<FSBillHistory,
                                Where<FSBillHistory.childEntityType, Equal<Required<FSBillHistory.childEntityType>>,
                                    And<FSBillHistory.childDocType, Equal<Required< FSBillHistory.childDocType>>,
                                    And<FSBillHistory.childRefNbr, Equal<Required< FSBillHistory.childRefNbr>>,
                                    And<FSBillHistory.srvOrdType, IsNotNull>>>>>
                                .Select(graph, parentEntityType, parentDocType, parentRefNbr);

                            tempFSARTran = new FSARTran();

                            tempFSARTran.ServiceContractRefNbr = serviceContract.ServiceContractRefNbr;

                            foreach (FSBillHistory billHistory in billHistoryRows)
                            {
                                tempFSARTran.SrvOrdType = billHistory.SrvOrdType;
                                tempFSARTran.ServiceOrderRefNbr = billHistory.ServiceOrderRefNbr;
                                tempFSARTran.AppointmentRefNbr = billHistory.AppointmentRefNbr;

                                CreateBillHistoryRowsFromARTran(cacheFSBillHistory, tempFSARTran,
                                        childEntityType, childDocType, childRefNbr,
                                        parentEntityType, parentDocType, parentRefNbr,
                                        false);
                            }
                        }
                    }
                    else
                    {
                        var fsDocuments = serviceContractGroup.Distinct(x => (x.SrvOrdType, x.ServiceOrderRefNbr, x.AppointmentRefNbr));

                        foreach (var fsDocument in fsDocuments)
                        {
                            CreateBillHistoryRowsFromARTran(cacheFSBillHistory, fsDocument,
                                    childEntityType, childDocType, childRefNbr,
                                    parentEntityType, parentDocType, parentRefNbr,
                                    false);
                        }
                    }
                }

                cacheFSBillHistory.Persist(PXDBOperation.Insert);
            }
        }

        public virtual FSBillHistory CreateBillHistoryRowsFromARTran(PXCache billHistoryCache, FSARTran fsARTranRow,
                                        string childEntityType, string childDocType, string childRefNbr,
                                        string parentEntityType, string parentDocType, string parentRefNbr,
                                        bool verifyIfExists)
        {
            if (parentEntityType == FSEntityType.SalesOrder)
            {
                if (fsARTranRow.SOOrderType == null || fsARTranRow.SOOrderNbr == null)
                {
                    return null;
                }
            }

            FSBillHistory newRow = new FSBillHistory();

            newRow.SrvOrdType = fsARTranRow.SrvOrdType;
            newRow.ServiceOrderRefNbr = fsARTranRow.ServiceOrderRefNbr;
            newRow.AppointmentRefNbr = fsARTranRow.AppointmentRefNbr;
            newRow.ServiceContractRefNbr = fsARTranRow.ServiceContractRefNbr;
            newRow.ParentEntityType = parentEntityType;
            newRow.ParentDocType = parentEntityType == FSEntityType.SalesOrder && parentDocType == null ? fsARTranRow.SOOrderType : parentDocType;
            newRow.ParentRefNbr = parentEntityType == FSEntityType.SalesOrder && parentRefNbr == null ? fsARTranRow.SOOrderNbr : parentRefNbr;
            newRow.ChildEntityType = childEntityType;
            newRow.ChildDocType = childDocType;
            newRow.ChildRefNbr = childRefNbr;

            if (verifyIfExists == true)
            {
                FSBillHistory existingRow = FSBillHistory.UK.FindDirty(billHistoryCache.Graph,
                   newRow.SrvOrdType, newRow.ServiceOrderRefNbr, newRow.AppointmentRefNbr,
                   newRow.ParentEntityType, newRow.ParentDocType, newRow.ParentRefNbr,
                   newRow.ChildEntityType, newRow.ChildDocType, newRow.ChildRefNbr);

                if (existingRow != null)
                {
                    return existingRow;
                }
            }

            return (FSBillHistory)billHistoryCache.Insert(newRow);
        }

        public virtual void CleanPostingInfoFromSOCreditMemo(PXGraph graph, SOInvoice crmSOInvoiceRow)
        {
            if (crmSOInvoiceRow.DocType != ARDocType.CreditMemo)
            {
                return;
            }

            var crmARTranRows = PXSelect<ARTran,
                                Where<
                                    ARTran.tranType, Equal<Required<ARTran.tranType>>,
                                And<
                                    ARTran.refNbr, Equal<Required<ARTran.refNbr>>>>>
                                .Select(graph, crmSOInvoiceRow.DocType, crmSOInvoiceRow.RefNbr)
                                .RowCast<ARTran>()
                                .GroupBy(x => (x.OrigInvoiceType, x.OrigInvoiceNbr))
                                .Select(y => y.OrderByDescending(z => z.RefNbr).First())
                                .ToList();

            foreach (var crmARTranRow in crmARTranRows)
            {
                var origARTranRow = PXSelect<ARTran,
                                    Where<
                                        ARTran.tranType, Equal<Required<ARTran.tranType>>,
                                    And<
                                        ARTran.refNbr, Equal<Required<ARTran.refNbr>>>>>
                                    .Select(graph, crmARTranRow.OrigInvoiceType, crmARTranRow.OrigInvoiceNbr)
                                    .RowCast<ARTran>()
                                    .Where(_ => _.SOOrderType != null && _.SOOrderNbr != null)
                                    .GroupBy(x => (x.SOOrderType, x.SOOrderNbr))
                                    .Select(y => y.OrderByDescending(z => z.SOOrderNbr).First())
                                    .FirstOrDefault();

                //Sales Order
                if (origARTranRow != null && string.IsNullOrEmpty(origARTranRow.SOOrderNbr) == false)
                {
                    object fsCreatedDoc = (SOOrder)PXSelect<SOOrder,
                                                   Where<
                                                       SOOrder.orderType, Equal<Required<SOOrder.orderType>>,
                                                   And<
                                                       SOOrder.orderNbr, Equal<Required<SOOrder.orderNbr>>>>>
                                                   .Select(graph, origARTranRow.SOOrderType, origARTranRow.SOOrderNbr)
                                                   .FirstOrDefault();

                    var soGraph = PXGraph.CreateInstance<SOOrderEntry>();
                    SM_SOOrderEntry soExt = soGraph.GetExtension<SM_SOOrderEntry>();

                    soExt.CleanPostingInfoLinkedToDoc(fsCreatedDoc);
                    soExt.CleanContractPostingInfoLinkedToDoc(fsCreatedDoc);
                }
                else
                {
                    object fsCreatedDoc = (SOInvoice)PXSelect<SOInvoice,
                                                     Where<
                                                         SOInvoice.docType, Equal<Required<SOInvoice.docType>>,
                                                     And<
                                                         SOInvoice.refNbr, Equal<Required<SOInvoice.refNbr>>>>>
                                                     .Select(graph, crmARTranRow.OrigInvoiceType, crmARTranRow.OrigInvoiceNbr);

                    CleanPostingInfoLinkedToDoc(fsCreatedDoc);
                }
            }
        }
        #endregion

        #region DACHelper
        public virtual void SetExtensionVisibleInvisible<DAC>(PXCache cache, PXRowSelectedEventArgs e, bool isVisible, bool isGrid)
            where DAC : PXCacheExtension
        {
            foreach (string fieldName in GetFieldsName(typeof(DAC)))
            {
                PXUIFieldAttribute.SetVisible(cache, null, fieldName, isVisible);
            }
        }

        public virtual List<string> GetFieldsName(Type dacType)
        {
            List<string> fieldList = new List<string>();

            foreach (PropertyInfo prop in dacType.GetProperties())
            {
                if (prop.GetCustomAttributes(true).Where(atr => atr is SkipSetExtensionVisibleInvisibleAttribute).Count() == 0)
                {
                    fieldList.Add(prop.Name);
                }
            }

            return fieldList;
        }
        #endregion
    }
}
