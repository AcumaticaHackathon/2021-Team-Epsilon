using PX.Data;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.IN;
using PX.Objects.SO;
using System;
using System.Collections.Generic;
using System.Text;

namespace PX.Objects.FS
{
    public static class MessageHelper
    {
        public class ErrorInfo
        {
            public int? SOID;
            public int? AppointmentID;
            public string ErrorMessage;
            public bool HeaderError;
        }

        public static int GetRowMessages(PXCache cache, object row, List<string> errors, List<string> warnings, bool includeRowInfo)
        {
            if (cache == null || row == null)
            {
                return 0;
            }

            int errorCount = 0;
            PXFieldState fieldState;

            foreach (string field in cache.Fields)
            {
                try
                {
                    fieldState = (PXFieldState)cache.GetStateExt(row, field);
                }
                catch
                {
                    fieldState = null;
                }

                if (fieldState != null && fieldState.Error != null)
                {
                    if (errors != null)
                    {
                        if (fieldState.ErrorLevel != PXErrorLevel.RowWarning
                            && fieldState.ErrorLevel != PXErrorLevel.Warning
                            && fieldState.ErrorLevel != PXErrorLevel.RowInfo
                        )
                        {
                            errors.Add(fieldState.Error);
                            errorCount++;
                        }
                    }

                    if (warnings != null)
                    {
                        if (fieldState.ErrorLevel == PXErrorLevel.RowWarning
                            || fieldState.ErrorLevel == PXErrorLevel.Warning
                            || (fieldState.ErrorLevel == PXErrorLevel.RowInfo && includeRowInfo == true)
                        )
                        {
                            warnings.Add(fieldState.Error);
                        }
                    }
                }
            }

            return errorCount;
        }

        public static string GetRowMessage(PXCache cache, IBqlTable row, bool getErrors, bool getWarnings)
        {
            List<string> errors = null;
            List<string> warnings = null;

            if (getErrors)
            {
                errors = new List<string>();
            }
            if (getWarnings)
            {
                warnings = new List<string>();
            }

            GetRowMessages(cache, row, errors, warnings, false);

            StringBuilder messageBuilder = new StringBuilder();

            if (errors != null)
            {
                foreach (string message in errors)
                {
                    if (messageBuilder.Length > 0)
                    {
                        messageBuilder.Append(Environment.NewLine);
                    }

                    messageBuilder.Append(message);
                }
            }

            if (warnings != null)
            {
                foreach (string message in warnings)
                {
                    if (messageBuilder.Length > 0)
                    {
                        messageBuilder.Append(Environment.NewLine);
                    }

                    messageBuilder.Append(message);
                }
            }

            return messageBuilder.ToString();
        }

        public static List<ErrorInfo> GetErrorInfo<TranType>(PXCache headerCache, IBqlTable headerRow, PXSelectBase<TranType> detailView, Type extensionType)
            where TranType : class, IBqlTable, new()
        {
            List<ErrorInfo> errorList = new List<ErrorInfo>();
            ErrorInfo errorInfo = null;

            string headerErrorMessage = MessageHelper.GetRowMessage(headerCache, headerRow, true, false);

            if (string.IsNullOrEmpty(headerErrorMessage) == false)
            {
                errorInfo = new ErrorInfo()
                {
                    HeaderError = true,
                    SOID = null,
                    AppointmentID = null,
                    ErrorMessage = headerErrorMessage
                };

                errorList.Add(errorInfo);
            }

            foreach (object row in detailView.Select())
            {
                string errorMessage = MessageHelper.GetRowMessage(detailView.Cache, (TranType)row, true, false);

                if (string.IsNullOrEmpty(errorMessage) == false)
                {
                    if (extensionType != null)
                    {
                        IFSRelatedDoc rowExtension = null;

                        if (extensionType == typeof(SOLine))
                        {
                            rowExtension = detailView.Cache.GetExtension<FSxSOLine>(row);
                        }
                        else if (extensionType == typeof(INTran))
                        {
                            rowExtension = detailView.Cache.GetExtension<FSxINTran>(row);
                        }
                        else if (extensionType == typeof(APTran))
                        {
                            rowExtension = detailView.Cache.GetExtension<FSxAPTran>(row);
                        }
                        else if (extensionType == typeof(ARTran))
                        {
                            ARTran rowARTran = (ARTran)row;
                            rowExtension = FSARTran.PK.Find(detailView.Cache.Graph, rowARTran?.TranType, rowARTran?.RefNbr, rowARTran?.LineNbr);
                        }
                        else 
                        {
                            errorInfo = new ErrorInfo()
                            {
                                HeaderError = false,
                                SOID = null,
                                AppointmentID = null,
                                ErrorMessage = errorMessage + ", "
                            };

                            errorList.Add(errorInfo);
                        }

                        if (rowExtension != null && string.IsNullOrEmpty(rowExtension.SrvOrdType) == false) 
                        {
                            if (string.IsNullOrEmpty(rowExtension.AppointmentRefNbr) == false)
                            {
                                FSAppointment fsAppointmentRow = FSAppointment.PK.Find(headerCache.Graph, 
                                                                                        rowExtension.SrvOrdType, 
                                                                                        rowExtension.AppointmentRefNbr);

                                errorInfo = new ErrorInfo()
                                {
                                    HeaderError = false,
                                    SOID = fsAppointmentRow.SOID,
                                    AppointmentID = fsAppointmentRow.AppointmentID,
                                    ErrorMessage = errorMessage + ", "
                                };

                                errorList.Add(errorInfo);
                            } 
                            else if (string.IsNullOrEmpty(rowExtension.ServiceOrderRefNbr) == false)
                            {
                                FSServiceOrder fsServiceOrderRow = FSServiceOrder.PK.Find(headerCache.Graph,
                                                                                            rowExtension.SrvOrdType,
                                                                                            rowExtension.ServiceOrderRefNbr);

                                errorInfo = new ErrorInfo()
                                {
                                    HeaderError = false,
                                    SOID = fsServiceOrderRow.SOID,
                                    AppointmentID = null,
                                    ErrorMessage = errorMessage + ", "
                                };

                                errorList.Add(errorInfo);
                            }
                        }
                    }
                }
            }

            return errorList;
        }

        public static List<ErrorInfo> GetErrorInfo<TranType>(PXCache headerCache, IBqlTable headerRow, PXSelectBase<TranType> detailView)
            where TranType : class, IBqlTable, new()
        {
            List<ErrorInfo> errorList = new List<ErrorInfo>();
            ErrorInfo errorInfo = null;

            string headerErrorMessage = MessageHelper.GetRowMessage(headerCache, headerRow, true, false);

            if (string.IsNullOrEmpty(headerErrorMessage) == false)
            {
                errorInfo = new ErrorInfo()
                {
                    HeaderError = true,
                    SOID = null,
                    AppointmentID = null,
                    ErrorMessage = headerErrorMessage
                };

                errorList.Add(errorInfo);
            }

            foreach (TranType row in detailView.Select())
            {
                string errorMessage = MessageHelper.GetRowMessage(detailView.Cache, row, true, false);

                if (string.IsNullOrEmpty(errorMessage) == false)
                {
                    errorInfo = new ErrorInfo()
                    {
                        HeaderError = false,
                        SOID = null,
                        AppointmentID = null,
                        ErrorMessage = errorMessage + ", "
                    };

                    errorList.Add(errorInfo);
                }
            }

            return errorList;
        }

        public static string GetLineDisplayHint(PXGraph graph, string lineRefNbr, string lineDescr, int? inventoryID)
        {
            string strHintText = string.Empty;
            if (string.IsNullOrEmpty(lineRefNbr))
                return strHintText;

            strHintText = lineRefNbr;

            if (inventoryID != null)
            {
                InventoryItem item = InventoryItem.PK.Find(graph, inventoryID);
                if (item != null)
                {
                    strHintText += " - ";
                    strHintText += item.InventoryCD.Trim();
                }
            }

            if(string.IsNullOrEmpty(lineDescr) == false)
                strHintText += " (" + lineDescr.Trim() + ")";

            return strHintText;
        }
    }

    public static class StringExtensionMethods
    {
        public static string EnsureEndsWithDot(this string str)
        {
            if (str == null) return string.Empty;

            str = str.Trim();

            if (str == string.Empty) return str;

            if (!str.EndsWith(".")) return str + ".";

            return str;
        }
    }
}
