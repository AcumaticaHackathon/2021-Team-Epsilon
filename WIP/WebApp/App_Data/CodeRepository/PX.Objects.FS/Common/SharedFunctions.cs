using PX.Common;
using PX.Data;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.CT;
using PX.Objects.EP;
using PX.Objects.FS.Scheduler;
using PX.Objects.IN;
using PX.Objects.PM;
using PX.Objects.PO;
using PX.Objects.SO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace PX.Objects.FS
{
    public static class SharedFunctions
    {
        public enum MonthsOfYear
        {
            January = 1, February, March, April, May, June, July, August, September, October, November, December
        }

        public enum SOAPDetOriginTab
        {
            Services, InventoryItems
        }

        #region Acumatica Related Functions
        /// <summary>
        /// Retrieves an InventoryItem row by its ID.
        /// </summary>
        public static InventoryItem GetInventoryItemRow(PXGraph graph, int? inventoryID)
        {
            if (inventoryID == null)
            {
                return null;
            }

            InventoryItem inventoryItemRow = InventoryItem.PK.Find(graph, inventoryID);

            return inventoryItemRow;
        }
        #endregion

        #region WeekCodes
        /// <summary>
        /// Split a string by commas and returns the result as a list of strings.
        /// </summary>
        public static List<string> SplitWeekcodeByComma(string weekCodes)
        {
            List<string> returnListWeekCodes = new List<string>();
            var weekCodesArray = weekCodes.Split(',');
            foreach (string weekcode in weekCodesArray)
            {
                returnListWeekCodes.Add(weekcode.Trim());
            }

            return returnListWeekCodes;
        }

        /// <summary>
        /// Split a string in chars and returns the result as a list of strings.
        /// </summary>
        public static List<string> SplitWeekcodeInChars(string weekcode)
        {
            List<string> returnListWeekCodeByLetters = new List<string>();
            for (int i = 0; i < weekcode.Length; i++)
            {
                returnListWeekCodeByLetters.Add(weekcode.Substring(i, 1));
            }

            return returnListWeekCodeByLetters;
        }

        /// <summary>
        /// Validates if a Week Code is less than or equal to 4.
        /// </summary>
        public static bool IsAValidWeekCodeLength(string weekcode)
        {
            if (weekcode.Length <= 4)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Validates if a specific Char is valid for a Week Code (1-4), (A-B), (C-F), (S-Z).
        /// </summary>
        public static bool IsAValidCharForWeekCode(string charToCompare)
        {
            Regex rgx = new Regex(@"^[1-4]?[a-bA-B]?[c-fC-F]?[s-zS-Z]?$");

            return rgx.IsMatch(charToCompare);
        }

        /// <summary>
        /// Validates if a Week Code is valid for a schedule time and list of Week Code(s) given.
        /// </summary>
        public static bool WeekCodeIsValid(string sourceWeekcodes, DateTime? scheduleTime, PXGraph graph)
        {
            PXResultset<FSWeekCodeDate> bqlResultSet = new PXResultset<FSWeekCodeDate>();
            List<object> weekCodeArgs = new List<object>();
            PXSelectBase<FSWeekCodeDate> commandFilter = new PXSelect<FSWeekCodeDate,
                                                             Where<
                                                                   FSWeekCodeDate.weekCodeDate, Equal<Required<FSWeekCodeDate.weekCodeDate>>>>
                                                             (graph);

            weekCodeArgs.Add(scheduleTime);

            Regex rgxP1 = new Regex(@"^[1-4]$");
            Regex rgxP2 = new Regex(@"^[a-bA-B]$");
            Regex rgxP3 = new Regex(@"^[c-fC-F]$");
            Regex rgxP4 = new Regex(@"^[s-zS-Z]$");

            List<string> weekcodes = SharedFunctions.SplitWeekcodeByComma(sourceWeekcodes);

            foreach (string weekcode in weekcodes)
            {
                List<string> charsInWeekCode = SharedFunctions.SplitWeekcodeInChars(weekcode);
                string p1, p2, p3, p4;
                p1 = p2 = p3 = p4 = "%";

                foreach (string letter in charsInWeekCode)
                {
                    string letterAux = letter.ToUpper();

                    if (rgxP1.IsMatch(letterAux))
                    {
                        p1 = letterAux;
                    }
                    else if (rgxP2.IsMatch(letterAux))
                    {
                        p2 = letterAux;
                    }
                    else if (rgxP3.IsMatch(letterAux))
                    {
                        p3 = letterAux;
                    }
                    else if (rgxP4.IsMatch(letterAux))
                    {
                        p4 = letterAux;
                    }
                }

                commandFilter.WhereOr<
                            Where2<
                                Where<
                                    FSWeekCodeDate.weekCodeP1, Like<Required<FSWeekCodeDate.weekCodeP1>>,
                                    Or<FSWeekCodeDate.weekCodeP1, Like<Required<FSWeekCodeDate.weekCodeP1>>,
                                    Or<FSWeekCodeDate.weekCodeP1, IsNull>>>,
                                And2<
                                    Where<
                                        FSWeekCodeDate.weekCodeP2, Like<Required<FSWeekCodeDate.weekCodeP2>>,
                                        Or<FSWeekCodeDate.weekCodeP2, Like<Required<FSWeekCodeDate.weekCodeP2>>,
                                        Or<FSWeekCodeDate.weekCodeP2, IsNull>>>,
                                And2<
                                    Where<
                                        FSWeekCodeDate.weekCodeP3, Like<Required<FSWeekCodeDate.weekCodeP3>>,
                                        Or<FSWeekCodeDate.weekCodeP3, Like<Required<FSWeekCodeDate.weekCodeP3>>,
                                        Or<FSWeekCodeDate.weekCodeP3, IsNull>>>,
                                And<
                                    Where<
                                        FSWeekCodeDate.weekCodeP4, Like<Required<FSWeekCodeDate.weekCodeP4>>,
                                        Or<FSWeekCodeDate.weekCodeP4, Like<Required<FSWeekCodeDate.weekCodeP4>>,
                                        Or<FSWeekCodeDate.weekCodeP4, IsNull>>>>>>>
                                >();

                weekCodeArgs.Add(p1);
                weekCodeArgs.Add(p1.ToLower());
                weekCodeArgs.Add(p2);
                weekCodeArgs.Add(p2.ToLower());
                weekCodeArgs.Add(p3);
                weekCodeArgs.Add(p3.ToLower());
                weekCodeArgs.Add(p4);
                weekCodeArgs.Add(p4.ToLower());
            }

            bqlResultSet = commandFilter.Select(weekCodeArgs.ToArray());

            return bqlResultSet.Count > 0;
        }

        #endregion

        /// <summary>
        /// Returns the day of the week depending on the ID [dayID]. Sunday is (0) and Monday (6).
        /// </summary>
        public static DayOfWeek getDayOfWeekByID(int dayID)
        {
            switch (dayID)
            {
                case 0:
                    return DayOfWeek.Sunday;
                case 1:
                    return DayOfWeek.Monday;
                case 2:
                    return DayOfWeek.Tuesday;
                case 3:
                    return DayOfWeek.Wednesday;
                case 4:
                    return DayOfWeek.Thursday;
                case 5:
                    return DayOfWeek.Friday;
                case 6:
                    return DayOfWeek.Saturday;
                default:
                    return DayOfWeek.Monday;
            }
        }

        /// <summary>
        /// Returns the month of the year depending on the ID [dayID]. January is (1) and December (12).
        /// </summary>
        public static MonthsOfYear getMonthOfYearByID(int monthID)
        {
            switch (monthID)
            {
                case 1:
                    return MonthsOfYear.January;
                case 2:
                    return MonthsOfYear.February;
                case 3:
                    return MonthsOfYear.March;
                case 4:
                    return MonthsOfYear.April;
                case 5:
                    return MonthsOfYear.May;
                case 6:
                    return MonthsOfYear.June;
                case 7:
                    return MonthsOfYear.July;
                case 8:
                    return MonthsOfYear.August;
                case 9:
                    return MonthsOfYear.September;
                case 10:
                    return MonthsOfYear.October;
                case 11:
                    return MonthsOfYear.November;
                default:
                    return MonthsOfYear.December;
            }
        }

        /// <summary>
        /// Returns the month in string of the year depending on the ID [dayID]. January is (JAN) and December (DEC).
        /// </summary>
        public static string GetMonthOfYearInStringByID(int monthID)
        {
            switch (monthID)
            {
                case 1:
                    return TX.ShortMonths.JANUARY;
                case 2:
                    return TX.ShortMonths.FEBRUARY;
                case 3:
                    return TX.ShortMonths.MARCH;
                case 4:
                    return TX.ShortMonths.APRIL;
                case 5:
                    return TX.ShortMonths.MAY;
                case 6:
                    return TX.ShortMonths.JUNE;
                case 7:
                    return TX.ShortMonths.JULY;
                case 8:
                    return TX.ShortMonths.AUGUST;
                case 9:
                    return TX.ShortMonths.SEPTEMBER;
                case 10:
                    return TX.ShortMonths.OCTOBER;
                case 11:
                    return TX.ShortMonths.NOVEMBER;
                default:
                    return TX.ShortMonths.DECEMBER;
            }
        }

        /// <summary>
        /// Calculates the beginning of the week for the specific <c>date</c> using the <c>startOfWeek</c> as reference.
        /// </summary>
        public static DateTime StartOfWeek(DateTime date, DayOfWeek startOfWeek)
        {
            int diff = date.DayOfWeek - startOfWeek;

            if (diff < 0)
            {
                diff += 7;
            }

            return date.AddDays(-1 * diff).Date;
        }

        /// <summary>
        /// Calculates the end of the week for the specific <c>date</c> using the <c>startOfWeek</c> as reference.
        /// </summary>
        public static DateTime EndOfWeek(DateTime date, DayOfWeek startOfWeek)
        {
            DateTime start = StartOfWeek(date, startOfWeek);

            return start.AddDays(6);
        }

        /// <summary>
        /// Verifies that the EndDate is greater than the StartDate.
        /// </summary>
        public static bool IsValidDateRange(DateTime? startDate, DateTime? endDate)
        {
            if (startDate == null
                        || endDate == null)
            {
                return false;
            }

            if (startDate > endDate)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates if the appointment scheduled day of the week belongs to the defined executions days of the given route.
        /// Also if it is valid sets the begin time of the route for the given week day.
        /// </summary>
        /// <param name="fsRouteRow">FSRoute row.</param>
        /// <param name="appointmentScheduledDayOfWeek">Monday Sunday.</param>
        /// <param name="beginTimeOnWeekDay">Begin time of the route in a specific day of week.</param>
        /// <returns>true if the route runs in the given week day, otherwise returns false.</returns>
        public static bool EvaluateExecutionDay(FSRoute fsRouteRow, DayOfWeek appointmentScheduledDayOfWeek, ref DateTime? beginTimeOnWeekDay)
        {
            if (fsRouteRow == null)
            {
                return false;
            }

            switch (appointmentScheduledDayOfWeek)
            {
                case DayOfWeek.Monday:
                    if ((bool)fsRouteRow.ActiveOnMonday)
                    {
                        beginTimeOnWeekDay = fsRouteRow.BeginTimeOnMonday;
                        return true;
                    }

                    break;
                case DayOfWeek.Tuesday:
                    if ((bool)fsRouteRow.ActiveOnTuesday)
                    {
                        beginTimeOnWeekDay = fsRouteRow.BeginTimeOnTuesday;
                        return true;
                    }

                    break;
                case DayOfWeek.Wednesday:
                    if ((bool)fsRouteRow.ActiveOnWednesday)
                    {
                        beginTimeOnWeekDay = fsRouteRow.BeginTimeOnWednesday;
                        return true;
                    }

                    break;
                case DayOfWeek.Thursday:
                    if ((bool)fsRouteRow.ActiveOnThursday)
                    {
                        beginTimeOnWeekDay = fsRouteRow.BeginTimeOnThursday;
                        return true;
                    }

                    break;
                case DayOfWeek.Friday:
                    if ((bool)fsRouteRow.ActiveOnFriday)
                    {
                        beginTimeOnWeekDay = fsRouteRow.BeginTimeOnFriday;
                        return true;
                    }

                    break;
                case DayOfWeek.Saturday:
                    if ((bool)fsRouteRow.ActiveOnSaturday)
                    {
                        beginTimeOnWeekDay = fsRouteRow.BeginTimeOnSaturday;
                        return true;
                    }

                    break;
                case DayOfWeek.Sunday:
                    if ((bool)fsRouteRow.ActiveOnSunday)
                    {
                        beginTimeOnWeekDay = fsRouteRow.BeginTimeOnSunday;
                        return true;
                    }

                    break;
            }

            return false;
        }

        /// <summary>
        /// Throw the Exception depending on the result of the EvaluateExecutionDay function.
        /// </summary>
        /// <param name="fsRouteRow">FSRoute row.</param>
        /// <param name="appointmentScheduledDayOfWeek">Monday Sunday.</param>
        /// <param name="beginTimeOnWeekDay">Begin time of the route in a specific day of week.</param>
        public static void ValidateExecutionDay(FSRoute fsRouteRow, DayOfWeek appointmentScheduledDayOfWeek, ref DateTime? beginTimeOnWeekDay)
        {
            if (!SharedFunctions.EvaluateExecutionDay(fsRouteRow, appointmentScheduledDayOfWeek, ref beginTimeOnWeekDay))
            {
                throw new PXException(TX.Error.INVALID_ROUTE_EXECUTION_DAYS_FOR_APPOINTMENT, fsRouteRow.RouteCD);
            }
        }

        /// <summary>
        /// Sets the given ScreenID to a format separated by dots
        /// SetScreenIDToDotFormat("SD300200") will return  "SD.300.200".
        /// </summary>
        /// <param name="screenID">8 characters ScreenID.</param>
        /// <returns>The given ScreenID in a dot separated format.</returns>
        public static string SetScreenIDToDotFormat(string screenID)
        {
            if (screenID.Length < 8 || screenID.Length > 8)
            {
                throw new PXException(TX.Error.SCREENID_INCORRECT_FORMAT);
            }

            return screenID.Substring(0, 2) + "." + screenID.Substring(2, 2) + "." + screenID.Substring(4, 2) + "." + screenID.Substring(6);
        }

        /// <summary>
        /// Get an appointment complete address from its service order.
        /// </summary>
        /// <returns>Returns a string containing the complete address of the appointment.</returns>
        public static string GetAppointmentAddress(FSAddress fsAddressRow)
        {
            if (fsAddressRow == null)
            {
                return string.Empty;
            }

            return SharedFunctions.GetAddressForGeolocation(fsAddressRow.PostalCode,
                                                            fsAddressRow.AddressLine1,
                                                            fsAddressRow.AddressLine2,
                                                            fsAddressRow.City,
                                                            fsAddressRow.State,
                                                            fsAddressRow.CountryID);
        }

        /// <summary>
        /// Get a complete address from a branch location.
        /// </summary>
        /// <returns>Returns a string containing the complete address of the branch location.</returns>
        public static string GetBranchLocationAddress(PXGraph graph, FSBranchLocation fsBranchLocationRow)
        {
            if (fsBranchLocationRow == null)
            {
                return string.Empty;
            }

            FSAddress fsAddressRow = FSAddress.PK.Find(graph, fsBranchLocationRow.BranchLocationAddressID);

            return SharedFunctions.GetAddressForGeolocation(fsAddressRow.PostalCode,
                                                            fsAddressRow.AddressLine1,
                                                            fsAddressRow.AddressLine2,
                                                            fsAddressRow.City,
                                                            fsAddressRow.State,
                                                            fsAddressRow.CountryID);
        }

        public static string GetAddressForGeolocation(string postalCode, string addressLine1, string addressLine2, string city, string state, string countryID)
        {
            string addressText = string.Empty;
            bool firstValue = true;

            if (!string.IsNullOrEmpty(addressLine1))
            {
                addressText = (firstValue == true) ? addressLine1.Trim() : addressText + ", " + addressLine1.Trim();
                firstValue = false;
            }

            if (!string.IsNullOrEmpty(addressLine2))
            {
                addressText = (firstValue == true) ? addressLine2.Trim() : addressText + ", " + addressLine2.Trim();
                firstValue = false;
            }

            if (!string.IsNullOrEmpty(city))
            {
                addressText = (firstValue == true) ? city.Trim() : addressText + ", " + city.Trim();
                firstValue = false;
            }

            if (!string.IsNullOrEmpty(state))
            {
                addressText = (firstValue == true) ? state.Trim() : addressText + ", " + state.Trim();
                firstValue = false;
            }

            if (!string.IsNullOrEmpty(postalCode))
            {
                addressText = (firstValue == true) ? postalCode.Trim() : addressText + ", " + postalCode.Trim();
                firstValue = false;
            }

            if (!string.IsNullOrEmpty(countryID))
            {
                addressText = (firstValue == true) ? countryID.Trim() : addressText + ", " + countryID.Trim();
                firstValue = false;
            }

            return addressText;
        }

        /// <summary>
        /// Extracts time info from 'date' field.
        /// </summary>
        /// <param name="date">DateTime field from where the time info is extracted.</param>
        /// <returns>A string with the following format: HH:MM AM/PM.</returns>
        public static string GetTimeStringFromDate(DateTime? date)
        {
            if (date.HasValue == false)
            {
                return TX.Error.SCHEDULED_DATE_UNAVAILABLE;
            }

            return date.Value.ToString("hh:mm tt");
        }

        /// <summary>
        /// Get the BAccountType based on the staffMemberID.
        /// </summary>
        public static string GetBAccountType(PXGraph graph, int? staffMemberID)
        {
            if (staffMemberID == null)
            {
                throw new PXException(TX.Error.STAFF_MEMBER_INCONSISTENCY);
            }

            BAccount bAccountRow = PXSelect<BAccount,
                                   Where<
                                        BAccount.bAccountID, Equal<Required<BAccount.bAccountID>>>>
                                   .Select(graph, staffMemberID);

            if (bAccountRow != null)
            {
                if (bAccountRow.Type == BAccountType.CombinedType || bAccountRow.Type == BAccountType.VendorType)
                {
                    return BAccountType.VendorType;
                }
                else if (bAccountRow.Type == BAccountType.EmpCombinedType || bAccountRow.Type == BAccountType.EmployeeType)
                {
                    return BAccountType.EmployeeType;
                }
                else
                {
                    throw new PXException(TX.Error.BACCOUNT_TYPE_DOES_NOT_MATCH_WITH_STAFF_MEMBERS_OPTIONS);
                }
            }
            else
            {
                throw new PXException(TX.Error.STAFF_MEMBER_INCONSISTENCY);
            }
        }

        #region GetItemWithList Methods

        /// <summary>
        /// Gets a SharedClasses.ItemList type list of recordID's of a field (FieldSearch) belonging to a list of items (FieldList) 
        /// from a table (Table) with a Join condition (Join) and a where clause (TWhere).
        /// </summary>
        /// <typeparam name="Table">Main table for the BQL.</typeparam>
        /// <typeparam name="Join">Join for the BQL.</typeparam>
        /// <typeparam name="FieldList">Search field for the select in BQL.</typeparam>
        /// <typeparam name="FieldSearch">Row filter field.</typeparam>
        /// <typeparam name="TWhere">Where BQL conditions.</typeparam>
        public static List<SharedClasses.ItemList> GetItemWithList<Table, Join, FieldList, FieldSearch, TWhere>(PXGraph graph, List<int?> fieldList, params object[] paramObjects)
            where Table : class, IBqlTable, new()
            where Join : IBqlJoin, new()
            where FieldList : IBqlField
            where FieldSearch : IBqlField
            where TWhere : IBqlWhere, new()
        {
            if (fieldList.Count == 0)
            {
                return new List<SharedClasses.ItemList>();
            }

            List<object> objectList = fieldList.Cast<object>().ToList();

            if (paramObjects.Count() > 0)
            {
                objectList = paramObjects.Concat(objectList).ToList();
            }

            BqlCommand fsTableBql = new Select2<Table, Join>();
            fsTableBql = fsTableBql.WhereAnd<TWhere>();
            fsTableBql = fsTableBql.WhereAnd(InHelper<FieldList>.Create(objectList.Count));

            return PopulateItemList<Table, FieldList, FieldSearch>(graph, fsTableBql, objectList);
        }

        /// <summary>
        /// Gets a SharedClasses.ItemList type list of recordID's of a field (FieldSearch) belonging to a list of items (FieldList) 
        /// from a table (Table) with a where clause (TWhere).
        /// </summary>
        /// <typeparam name="Table">Main table for the BQL.</typeparam>
        /// <typeparam name="FieldList">Search field for the select in BQL.</typeparam>
        /// <typeparam name="FieldSearch">Row filter field.</typeparam>
        /// <typeparam name="TWhere">Where BQL conditions.</typeparam>
        public static List<SharedClasses.ItemList> GetItemWithList<Table, FieldList, FieldSearch, TWhere>(PXGraph graph, List<int?> fieldList, params object[] paramObjects)
            where Table : class, IBqlTable, new()
            where FieldList : IBqlField
            where FieldSearch : IBqlField
            where TWhere : IBqlWhere, new()
        {
            if (fieldList.Count == 0)
            {
                return new List<SharedClasses.ItemList>();
            }

            List<object> objectList = fieldList.Cast<object>().ToList();

            if (paramObjects.Count() > 0)
            {
                objectList = paramObjects.Concat(objectList).ToList();
            }

            BqlCommand fsTableBql = new Select<Table>();
            fsTableBql = fsTableBql.WhereAnd<TWhere>();
            fsTableBql = fsTableBql.WhereAnd(InHelper<FieldList>.Create(objectList.Count));

            return PopulateItemList<Table, FieldList, FieldSearch>(graph, fsTableBql, objectList);
        }

        /// <summary>
        /// Gets a SharedClasses.ItemList type list of recordID's of a field (FieldSearch) belonging to a list of items (FieldList) 
        /// from a table (Table).
        /// </summary>
        /// <typeparam name="Table">Main table for the BQL.</typeparam>
        /// <typeparam name="FieldList">Search field for the select in BQL.</typeparam>
        /// <typeparam name="FieldSearch">Row filter field.</typeparam>
        public static List<SharedClasses.ItemList> GetItemWithList<Table, FieldList, FieldSearch>(PXGraph graph, List<int?> fieldList)
            where Table : class, IBqlTable, new()
            where FieldList : IBqlField
            where FieldSearch : IBqlField
        {
            if (fieldList.Count == 0)
            {
                return new List<SharedClasses.ItemList>();
            }

            List<object> objectList = fieldList.Cast<object>().ToList();

            BqlCommand fsTableBql = new Select<Table>();
            fsTableBql = fsTableBql.WhereAnd(InHelper<FieldList>.Create(objectList.Count));

            return PopulateItemList<Table, FieldList, FieldSearch>(graph, fsTableBql, objectList);
        }

        /// <summary>
        /// Populate a SharedClasses.ItemList type list with recordID's of a field (FieldSearch) belonging to a list of items (FieldList) 
        /// from a table (Table).
        /// </summary>
        /// <typeparam name="Table">Main table for the BQL.</typeparam>
        /// <typeparam name="FieldList">Search field for the select in BQL.</typeparam>
        /// <typeparam name="FieldSearch">Row filter field.</typeparam>
        private static List<SharedClasses.ItemList> PopulateItemList<Table, FieldList, FieldSearch>(PXGraph graph, BqlCommand fsTableBql, List<object> fieldList)
            where Table : class, IBqlTable, new()
            where FieldList : IBqlField
            where FieldSearch : IBqlField
        {
            List<SharedClasses.ItemList> itemList = new List<SharedClasses.ItemList>();

            PXView fsTableView = new PXView(graph, true, fsTableBql);
            var fsTableRows = fsTableView.SelectMulti(fieldList.ToArray());

            string fieldListName = Regex.Replace(typeof(FieldList).Name, "^[a-z]", m => m.Value.ToUpper());
            string fieldSearchName = Regex.Replace(typeof(FieldSearch).Name, "^[a-z]", m => m.Value.ToUpper());

            Type[] tables = fsTableBql.GetTables();
            bool withJoin = tables.Count() > 1;

            foreach (var row in fsTableRows)
            {
                Table objectRow;

                if (withJoin)
                {
                    PXResult<Table> bqlResult = (PXResult<Table>)row;
                    objectRow = (Table)bqlResult;
                }
                else
                {
                    objectRow = (Table)row;
                }

                var fieldListValue = typeof(Table).GetProperty(fieldListName).GetValue(objectRow);
                var fieldSearchValue = typeof(Table).GetProperty(fieldSearchName).GetValue(objectRow);

                var item = itemList.Where(list => list.itemID == (int?)fieldListValue).FirstOrDefault();
                if (item != null)
                {
                    item.list.Add(fieldSearchValue);
                }
                else
                {
                    SharedClasses.ItemList newItem = new SharedClasses.ItemList((int?)fieldListValue);
                    newItem.list.Add(fieldSearchValue);
                    itemList.Add(newItem);
                }
            }

            return itemList;
        }
        #endregion

        /// <summary>
        /// Checks if the given Business Account identifier is a prospect type.
        /// </summary>
        /// <param name="graph">Context graph.</param>
        /// <param name="bAccountID">Business Account identifier.</param>
        /// <returns>True is the Business Account is a Prospect.</returns>
        public static bool isThisAProspect(PXGraph graph, int? bAccountID)
        {
            BAccount bAccountRow = PXSelect<BAccount,
                                   Where<
                                       BAccount.bAccountID, Equal<Required<BAccount.bAccountID>>,
                                       And<BAccount.type, Equal<BAccountType.prospectType>>>>
                                   .Select(graph, bAccountID);

            return bAccountRow != null;
        }

        /// <summary>
        /// Validates the Actual Start/End date times for a given Route Document.
        /// </summary>
        /// <param name="cache">Route Document Cache.</param>
        /// <param name="fsRouteDocumentRow">Route Document row.</param>
        /// <param name="businessDate">Current graph business date.</param>
        public static void CheckRouteActualDateTimes(PXCache cache, FSRouteDocument fsRouteDocumentRow, DateTime? businessDate)
        {
            if (fsRouteDocumentRow.ActualStartTime == null
                || fsRouteDocumentRow.ActualEndTime == null)
            {
                return;
            }

            PXSetPropertyException exception = null;
            DateTime startTime = (DateTime)fsRouteDocumentRow.ActualStartTime;
            DateTime endTime = (DateTime)fsRouteDocumentRow.ActualEndTime;

            if (startTime > endTime)
            {
                exception = new PXSetPropertyException(TX.Error.END_TIME_LESSER_THAN_START_TIME, PXErrorLevel.RowError);
            }

            cache.RaiseExceptionHandling<FSRouteDocument.actualStartTime>(fsRouteDocumentRow, startTime, exception);
            cache.RaiseExceptionHandling<FSRouteDocument.actualEndTime>(fsRouteDocumentRow, endTime, exception);
        }

        /// <summary>
        /// Tries to parse the <c>newValue</c> to DateTime?. When the <c>newValue</c> is string and the DateTime TryParse is not possible returns null. Otherwise returns (DateTime?) <c>newValue</c>.
        /// </summary>
        public static DateTime? TryParseHandlingDateTime(PXCache cache, object newValue)
        {
            if (newValue == null)
            {
                return null;
            }

            DateTime valFromString;

            if (newValue is string)
            {
                if (DateTime.TryParse((string)newValue, cache.Graph.Culture, DateTimeStyles.None, out valFromString))
                {
                    return valFromString;
                }
                else
                {
                    return null;
                }
            }

            return (DateTime?)newValue;
        }

        /// <summary>
        /// Create an Equipment from a sold Inventory Item.
        /// </summary>
        /// <param name="graphSMEquipmentMaint"> Equipment graph.</param>
        /// <param name="soldInventoryItemRow">Sold Inventory Item data.</param>
        public static FSEquipment CreateSoldEquipment(SMEquipmentMaint graphSMEquipmentMaint, SoldInventoryItem soldInventoryItemRow, ARTran arTranRow, FSARTran fsARTranRow, SOLine soLineRow, string action, InventoryItem inventoryItemRow)
        {
            FSEquipment fsEquipmentRow = new FSEquipment();
            fsEquipmentRow.OwnerType = ID.OwnerType_Equipment.CUSTOMER;
            fsEquipmentRow.RequireMaintenance = true;
            
            fsEquipmentRow.SiteID = soldInventoryItemRow.SiteID;

            if (inventoryItemRow != null)
            {
                FSxEquipmentModel fsxEquipmentModelRow = PXCache<InventoryItem>.GetExtension<FSxEquipmentModel>(inventoryItemRow);
                fsEquipmentRow.EquipmentTypeID = fsxEquipmentModelRow?.EquipmentTypeID;
            }

            //Sales Info
            fsEquipmentRow.SalesOrderType = soldInventoryItemRow.SOOrderType;
            fsEquipmentRow.SalesOrderNbr = soldInventoryItemRow.SOOrderNbr;

            //Customer Info    
            fsEquipmentRow.OwnerID = soldInventoryItemRow.CustomerID;
            fsEquipmentRow.CustomerID = soldInventoryItemRow.CustomerID;
            fsEquipmentRow.CustomerLocationID = soldInventoryItemRow.CustomerLocationID;

            //Lot/Serial Info.
            fsEquipmentRow.INSerialNumber = soldInventoryItemRow.LotSerialNumber;
            fsEquipmentRow.SerialNumber = soldInventoryItemRow.LotSerialNumber;

            //Source info.
            fsEquipmentRow.SourceType = ID.SourceType_Equipment.AR_INVOICE;
            fsEquipmentRow.SourceDocType = soldInventoryItemRow.DocType;
            fsEquipmentRow.SourceRefNbr = soldInventoryItemRow.InvoiceRefNbr;
            fsEquipmentRow.ARTranLineNbr = soldInventoryItemRow.InvoiceLineNbr;

            //Installation Info
            if (fsARTranRow != null)
            {
                fsEquipmentRow.InstSrvOrdType = fsARTranRow.SrvOrdType;
                fsEquipmentRow.InstServiceOrderRefNbr = fsARTranRow.ServiceOrderRefNbr;
                fsEquipmentRow.InstAppointmentRefNbr = fsARTranRow.AppointmentRefNbr;
            }

            if (action == ID.Equipment_Action.REPLACING_TARGET_EQUIPMENT)
            {
                if (fsARTranRow != null)
                {
                    //Equipment Replaced
                    fsEquipmentRow.EquipmentReplacedID = fsARTranRow.SMEquipmentID;
                }
            }

            fsEquipmentRow = graphSMEquipmentMaint.EquipmentRecords.Insert(fsEquipmentRow);
            graphSMEquipmentMaint.EquipmentRecords.SetValueExt<FSEquipment.inventoryID>(fsEquipmentRow, soldInventoryItemRow.InventoryID);
            graphSMEquipmentMaint.EquipmentRecords.SetValueExt<FSEquipment.dateInstalled>(fsEquipmentRow, soldInventoryItemRow.DocDate);
            graphSMEquipmentMaint.EquipmentRecords.SetValueExt<FSEquipment.salesDate>(fsEquipmentRow, soldInventoryItemRow.SOOrderDate != null ? soldInventoryItemRow.SOOrderDate : soldInventoryItemRow.DocDate);
            graphSMEquipmentMaint.EquipmentRecords.SetValueExt<FSEquipment.descr>(fsEquipmentRow, (soldInventoryItemRow.Descr == null) ? soldInventoryItemRow.InventoryCD : soldInventoryItemRow.Descr);

            if (soldInventoryItemRow.Descr != null)
            {
                object inventoryItem = PXSelectorAttribute.Select<FSEquipment.inventoryID>(graphSMEquipmentMaint.EquipmentRecords.Cache, graphSMEquipmentMaint.EquipmentRecords.Current);
                PXDBLocalizableStringAttribute.CopyTranslations<InventoryItem.descr, FSEquipment.descr>(graphSMEquipmentMaint, inventoryItem, fsEquipmentRow);
            }

            //Attributes
            graphSMEquipmentMaint.Answers.CopyAllAttributes(graphSMEquipmentMaint.EquipmentRecords.Current, inventoryItemRow);

            //Image
            PXNoteAttribute.CopyNoteAndFiles(graphSMEquipmentMaint.Caches[typeof(InventoryItem)], inventoryItemRow, graphSMEquipmentMaint.Caches[typeof(FSEquipment)], graphSMEquipmentMaint.EquipmentRecords.Current, false, true);
            fsEquipmentRow.ImageUrl = inventoryItemRow.ImageUrl;

            graphSMEquipmentMaint.Save.Press();
            fsEquipmentRow = graphSMEquipmentMaint.EquipmentRecords.Current;

            if (fsEquipmentRow != null && arTranRow != null && fsARTranRow != null)
            {
                foreach (FSEquipmentComponent fsEquipmentComponentRow in graphSMEquipmentMaint.EquipmentWarranties.Select())
                {
                    fsEquipmentComponentRow.SalesOrderNbr = arTranRow.SOOrderNbr;
                    fsEquipmentComponentRow.SalesOrderType = arTranRow.SOOrderType;
                    fsEquipmentComponentRow.InstSrvOrdType = fsARTranRow.SrvOrdType;
                    fsEquipmentComponentRow.InstServiceOrderRefNbr = fsARTranRow.ServiceOrderRefNbr;
                    fsEquipmentComponentRow.InstAppointmentRefNbr = fsARTranRow.AppointmentRefNbr;
                    fsEquipmentComponentRow.InvoiceRefNbr = soldInventoryItemRow.InvoiceRefNbr;
                    graphSMEquipmentMaint.EquipmentWarranties.Update(fsEquipmentComponentRow);
                    graphSMEquipmentMaint.EquipmentWarranties.SetValueExt<FSEquipmentComponent.installationDate>(fsEquipmentComponentRow, soldInventoryItemRow.DocDate);
                    graphSMEquipmentMaint.EquipmentWarranties.SetValueExt<FSEquipmentComponent.salesDate>(fsEquipmentComponentRow, soLineRow != null && soLineRow.OrderDate != null ? soLineRow.OrderDate : arTranRow.TranDate);
                }

                graphSMEquipmentMaint.Save.Press();
            }

            return fsEquipmentRow;
        }

        /// <summary>
        /// Retrieves an Equipment row by its ID.
        /// </summary>
        public static FSEquipment GetEquipmentRow(PXGraph graph, int? smEquipmentID)
        {
            if (smEquipmentID == null)
            {
                return null;
            }

            FSEquipment fSEquipmentRow = PXSelect<FSEquipment,
                                         Where<
                                             FSEquipment.SMequipmentID, Equal<Required<FSEquipment.SMequipmentID>>>>
                                         .Select(graph, smEquipmentID);

            return fSEquipmentRow;
        }

        /// <summary>
        /// Checks whether there is or not any generation process associated with scheduleID.
        /// </summary>
        /// <returns>True if there is a generation process, otherwise it returns False.</returns>
        public static bool isThereAnyGenerationProcessForThisSchedule(PXCache cache, int? scheduleID)
        {
            bool anyGenerationProcess = true;

            if (scheduleID > 0)
            {
                int scheduleProcessed = PXSelect<FSContractGenerationHistory,
                                        Where<
                                              FSContractGenerationHistory.scheduleID, Equal<Required<FSContractGenerationHistory.scheduleID>>>>
                                        .SelectWindowed(cache.Graph, 0, 1, scheduleID).Count;

                anyGenerationProcess = scheduleProcessed != 0;
            }

            return anyGenerationProcess;
        }

        /// <summary>
        /// Shows a warning message if the current schedule has not been processed yet.
        /// </summary>
        /// <returns>True if there is a generation process, otherwise it returns False.</returns>
        public static bool ShowWarningScheduleNotProcessed(PXCache cache, FSSchedule fsScheduleRow)
        {
            bool anyGenerationProcess = SharedFunctions.isThereAnyGenerationProcessForThisSchedule(cache, fsScheduleRow.ScheduleID);

            if (anyGenerationProcess == false)
            {
                cache.RaiseExceptionHandling<FSSchedule.refNbr>(fsScheduleRow,
                                                                fsScheduleRow.RefNbr,
                                                                new PXSetPropertyException(TX.Warning.SCHEDULE_WILL_NOT_AFFECT_SYSTEM_UNTIL_GENERATION_OCCURS, PXErrorLevel.RowWarning));
            }

            return anyGenerationProcess;
        }

        /// <summary>
        /// Gets the name of the specified field with the default option to capitalize its first letter.
        /// </summary>
        /// <typeparam name="field">Field from where to get the name.</typeparam>
        /// <param name="capitalizedFirstLetter">Flag to indicate if the first letter is capital.</param>
        /// <returns>Returns the field's name.</returns>
        public static string GetFieldName<field>(bool capitalizedFirstLetter = true)
            where field : IBqlField
        {
            string fieldName = typeof(field).Name;

            if (capitalizedFirstLetter == true)
            {
                fieldName = fieldName.First().ToString().ToUpper() + fieldName.Substring(1);
            }

            return fieldName;
        }

        /// <summary>
        /// Copy all common fields from a source row to a target row skipping special fields like key fields and Acumatica creation/update fields.
        /// Optionally you can pass a list of field names to exclude of the copy.
        /// </summary>
        /// <param name="cacheTarget">The cache of the target row.</param>
        /// <param name="rowTarget">The target row.</param>
        /// <param name="cacheSource">The cache of the source row.</param>
        /// <param name="rowSource">The source row.</param>
        /// <param name="excludeFields">List of field names to exclude of the copy.</param>
        public static void CopyCommonFields(PXCache cacheTarget, IBqlTable rowTarget, PXCache cacheSource, IBqlTable rowSource, params string[] excludeFields)
        {
            bool skipField;
            string fieldName;

            foreach (Type bqlField in cacheTarget.BqlFields)
            {
                fieldName = bqlField.Name;

                if (excludeFields != null && Array.Exists<string>(excludeFields, element => element.Equals(fieldName, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                if (fieldName.Contains("_"))
                {
                    continue;
                }

                if (cacheSource.Fields.Exists(element => element.Equals(fieldName, StringComparison.OrdinalIgnoreCase)))
                {
                    skipField = false;

                    foreach (PXEventSubscriberAttribute attr in cacheTarget.GetAttributes(fieldName))
                    {
                        if (attr is PXDBIdentityAttribute
                            || (attr is PXDBFieldAttribute && ((PXDBFieldAttribute)attr).IsKey == true)
                            || attr is PXDBCreatedByIDAttribute || attr is PXDBCreatedByScreenIDAttribute || attr is PXDBCreatedDateTimeAttribute
                            || attr is PXDBLastModifiedByIDAttribute || attr is PXDBLastModifiedByScreenIDAttribute || attr is PXDBLastModifiedDateTimeAttribute
                            || attr is PXDBTimestampAttribute)
                        {
                            skipField = true;
                            break;
                        }
                    }

                    if (skipField)
                    {
                        continue;
                    }

                    cacheTarget.SetValue(rowTarget, fieldName, cacheSource.GetValue(rowSource, fieldName));
                }
            }
        }

        public static int ReplicateCacheExceptions(PXCache cache, IBqlTable row, PXCache cacheWithExceptions, IBqlTable rowWithExceptions)
        {
            return ReplicateCacheExceptions(cache, row, null, null, cacheWithExceptions, rowWithExceptions);
        }
        
        // TODO: Refactor this method to use PXUIFieldAttribute.GetErrors
        public static int ReplicateCacheExceptions(PXCache cache1, IBqlTable row1, PXCache cache2, IBqlTable row2, PXCache cacheWithExceptions, IBqlTable rowWithExceptions)
        {
            int errorCount = 0;
            PXFieldState fieldState;

            List<string> uiFields = SharedFunctions.GetUIFields(cache1, row1);
            bool fieldWithoutMapping = false;
            string lastErrorMessage = string.Empty;

            foreach (string field in cache1.Fields)
            {
                try
                {
                    fieldState = (PXFieldState)cacheWithExceptions.GetStateExt(rowWithExceptions, field);
                }
                catch
                {
                    fieldState = null;
                }

                if (fieldState != null && fieldState.Error != null)
                {
                    PXException exception = new PXSetPropertyException(fieldState.Error, fieldState.ErrorLevel);

                    cache1.RaiseExceptionHandling(field, row1, null, exception);
                    if (cache2 != null && row2 != null)
                    {
                        cache2.RaiseExceptionHandling(field, row2, null, exception);
                    }

                    if (fieldState.ErrorLevel != PXErrorLevel.RowInfo && fieldState.ErrorLevel != PXErrorLevel.RowWarning && fieldState.ErrorLevel != PXErrorLevel.Warning)
                    {
                        errorCount++;

                        if (uiFields.Any(e => e.Equals(field, StringComparison.OrdinalIgnoreCase)) == false)
                        {
                            fieldWithoutMapping = true;
                            lastErrorMessage = fieldState.Error;
                        }
                    }
                }
            }

            if (fieldWithoutMapping == true)
            {
                throw new PXException(TX.Error.ErrorUpdatingTheServiceOrderWithTheMessageX, lastErrorMessage);
            }

            return errorCount;
        }

        public static List<string> GetUIFields(PXCache cache, object data)
        {
            var ret = new List<string>();
            foreach (IPXInterfaceField attr in cache.GetAttributes(data, null).OfType<IPXInterfaceField>())
            {
                ret.Add(((PXEventSubscriberAttribute)attr).FieldName);
            }
            return ret;
        }

        /// <summary>
        /// Get the web methods file path.
        /// </summary>
        public static string GetWebMethodPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            string pagePrefix = "/" + ID.AcumaticaFolderName.PAGE + "/";
            string modulePrefix = ID.Module.SERVICE_DISPATCH + "/";

            int pageIndex = path.LastIndexOf(pagePrefix.ToLower()) != -1 ? path.LastIndexOf(pagePrefix.ToLower()) : path.LastIndexOf(pagePrefix);

            if (pageIndex == -1)
            {
                pageIndex = 0;
            }
            else
            {
                pageIndex = pageIndex + pagePrefix.Length;
            }

            int index = path.IndexOf(modulePrefix.ToLower(), pageIndex) != -1 ? path.IndexOf(modulePrefix.ToLower(), pageIndex) : path.IndexOf(modulePrefix, pageIndex);

            if (index == -1)
            {
                return string.Empty;
            }

            return path.Substring(0, index + ID.Module.SERVICE_DISPATCH.Length) + "/" + ID.ScreenID.WEB_METHOD + ".aspx";
        }

        public static string GetMapApiKey(PXGraph graph)
        {
            FSSetup fsSetupRow = PXSelect<FSSetup>.Select(graph);

            return fsSetupRow != null ? fsSetupRow.MapApiKey : string.Empty;
        }

        public static XmlNamespaceManager GenerateXmlNameSpace(ref XmlNamespaceManager nameSpace)
        {
            nameSpace.AddNamespace(string.Format("{0}", ID.MapsConsts.XML_SCHEMA),
                string.Format("{0}", ID.MapsConsts.XML_SCHEMA_URI));
            return nameSpace;
        }

        public static string parseSecsDurationToString(int duration)
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(duration);
            string totalDuration = null;

            if (timeSpan.Hours > 0)
            {
                totalDuration += timeSpan.Hours.ToString() + " hour";

                if (timeSpan.Hours > 1)
                    totalDuration += "s ";
                else
                    totalDuration += " ";

                if (timeSpan.Seconds >= 30)
                    totalDuration += (timeSpan.Minutes + 1).ToString() + " min";
                else
                    totalDuration += timeSpan.Minutes.ToString() + " min";

                if (timeSpan.Minutes > 1)
                    totalDuration += "s";
            }
            else
            {
                if (timeSpan.Seconds >= 30)
                    totalDuration += (timeSpan.Minutes + 1).ToString() + " min";
                else
                    totalDuration += timeSpan.Minutes.ToString() + " min";

                if (timeSpan.Minutes > 1)
                    totalDuration += "s";
            }

            return totalDuration;
        }

        public static bool isFSSetupSet(PXGraph graph)
        {
            if (PXAccess.FeatureInstalled<FeaturesSet.serviceManagementModule>() == false)
            {
                return false;
            }

            FSSetup fsSetupRow = PXSelect<FSSetup>.SelectWindowed(graph, 0, 1);

            return fsSetupRow != null;
        }

        public static Customer GetCustomerRow(PXGraph graph, int? customerID)
        {
            if (customerID == null)
            {
                return null;
            }

            return PXSelect<Customer,
                   Where<
                        Customer.bAccountID, Equal<Required<Customer.bAccountID>>>>
                   .Select(graph, customerID);
        }

        public static TimeSpan GetTimeSpanDiff(DateTime begin, DateTime end)
        {
            begin = new DateTime(begin.Year, begin.Month, begin.Day, begin.Hour, begin.Minute, 0);
            end = new DateTime(end.Year, end.Month, end.Day, end.Hour, end.Minute, 0);

            return (TimeSpan)(end - begin);
        }

        public static DateTime? RemoveTimeInfo(DateTime? date)
        {
            if (date != null)
            {
                int hour = date.Value.Hour;
                date = date.Value.AddHours(-hour);

                int minute = date.Value.Minute;
                date = date.Value.AddMinutes(-minute);
            }

            return date;
        }

        public static string GetCompleteCoordinate(decimal? longitude, decimal? latitude)
        {
            if (longitude == null || latitude == null)
            {
                return string.Empty;
            }

            return "[" + longitude + "],[" + latitude + "]";
        }

        public static bool IsNotAllowedBillingOptionsModification(FSBillingCycle fsBillingCycleRow)
        {
            if (fsBillingCycleRow != null)
            {
                return (fsBillingCycleRow.BillingCycleType == ID.Billing_Cycle_Type.PURCHASE_ORDER && fsBillingCycleRow.GroupBillByLocations == false)
                            || (fsBillingCycleRow.BillingCycleType == ID.Billing_Cycle_Type.WORK_ORDER && fsBillingCycleRow.GroupBillByLocations == false)
                                || (fsBillingCycleRow.BillingCycleType == ID.Billing_Cycle_Type.TIME_FRAME && fsBillingCycleRow.GroupBillByLocations == false);
            }

            return true;
        }

        #region Equipment
        public static bool AreEquipmentFieldsValid(PXCache cache, int? inventoryID, int? targetEQ, object newTargetEQLineNbr, string equipmentAction, ref string errorMessage)
        {
            if (!PXAccess.FeatureInstalled<FeaturesSet.equipmentManagementModule>())
            {
                return true;
            }

            errorMessage = string.Empty;

            if (inventoryID != null)
            {
                InventoryItem inventoryItemRow = SharedFunctions.GetInventoryItemRow(cache.Graph, inventoryID);
                FSxEquipmentModel fsxEquipmentModel = PXCache<InventoryItem>.GetExtension<FSxEquipmentModel>(inventoryItemRow);

                switch (equipmentAction)
                {
                    case ID.Equipment_Action.SELLING_TARGET_EQUIPMENT:

                        if (fsxEquipmentModel.EquipmentItemClass != ID.Equipment_Item_Class.MODEL_EQUIPMENT)
                        {
                            errorMessage = TX.Error.EQUIPMENT_ACTION_MODEL_EQUIPMENT_REQUIRED;
                            return false;
                        }

                        break;

                    case ID.Equipment_Action.CREATING_COMPONENT:

                        if (fsxEquipmentModel.EquipmentItemClass != ID.Equipment_Item_Class.COMPONENT)
                        {
                            errorMessage = TX.Error.EQUIPMENT_ACTION_COMPONENT_REQUIRED;
                            return false;
                        }
                        else if (newTargetEQLineNbr == null && targetEQ == null)
                        {
                            errorMessage = TX.Error.EQUIPMENT_ACTION_TARGET_EQUIP_OR_NEW_TARGET_EQUIP_REQUIRED;
                            return false;
                        }

                        break;

                    case ID.Equipment_Action.REPLACING_COMPONENT:

                        if (fsxEquipmentModel.EquipmentItemClass != ID.Equipment_Item_Class.COMPONENT)
                        {
                            errorMessage = TX.Error.EQUIPMENT_ACTION_COMPONENT_REQUIRED;
                            return false;
                        }
                        else if (targetEQ == null)
                        {
                            errorMessage = TX.Error.EQUIPMENT_ACTION_TARGET_EQUIP_OR_NEW_TARGET_EQUIP_REQUIRED;
                            return false;
                        }

                        break;

                    case ID.Equipment_Action.REPLACING_TARGET_EQUIPMENT:

                        if (fsxEquipmentModel.EquipmentItemClass != ID.Equipment_Item_Class.MODEL_EQUIPMENT)
                        {
                            errorMessage = TX.Error.EQUIPMENT_ACTION_MODEL_EQUIPMENT_REQUIRED;
                            return false;
                        }
                        else if (newTargetEQLineNbr == null && targetEQ == null)
                        {
                            errorMessage = TX.Error.EQUIPMENT_ACTION_TARGET_EQUIP_OR_NEW_TARGET_EQUIP_REQUIRED;
                            return false;
                        }

                        break;
                }
            }

            return true;
        }

        public static void UpdateEquipmentFields(PXGraph graph, PXCache cache, Object row, int? inventoryID, bool updateQty = true)
        {
            if (!PXAccess.FeatureInstalled<FeaturesSet.equipmentManagementModule>())
            {
                return;
            }

            SetEquipmentActionFromInventory(graph, row, inventoryID);
            SetEquipmentFieldEnablePersistingCheck(cache, row, updateQty: updateQty);
        }

        public static void SetEquipmentActionFromInventory(PXGraph graph, object row, int? inventoryID)
        {
            if (inventoryID == null || PXAccess.FeatureInstalled<FeaturesSet.equipmentManagementModule>() == false)
            {
                return;
            }

            string equipmentItemClass = null;

            string equipmentAction = ID.Equipment_Action.NONE;
            FSxEquipmentModel fsxEquipmentModelRow = PXCache<InventoryItem>.GetExtension<FSxEquipmentModel>(SharedFunctions.GetInventoryItemRow(graph, inventoryID));
            equipmentItemClass = fsxEquipmentModelRow?.EquipmentItemClass;

            if (row is SOLine
                && SharedFunctions.SetScreenIDToDotFormat(ID.ScreenID.SALES_ORDER) != graph.Accessinfo.ScreenID)
            {
                equipmentAction = ID.Equipment_Action.NONE;
            }
            else 
            {
                if (fsxEquipmentModelRow != null && fsxEquipmentModelRow.EQEnabled == true)
                {
                    equipmentAction = getEquipmentModelAction(graph, fsxEquipmentModelRow);
                }
            }

            if (row is SOLine)
            {
                SOLine soLineRow = row as SOLine;
                FSxSOLine fsxSOLineRow = PXCache<SOLine>.GetExtension<FSxSOLine>(soLineRow);
                fsxSOLineRow.EquipmentAction = equipmentAction;
                fsxSOLineRow.Comment = string.Empty;
                fsxSOLineRow.SMEquipmentID = null;
                fsxSOLineRow.NewEquipmentLineNbr = null;
                fsxSOLineRow.ComponentID = null;
                fsxSOLineRow.EquipmentComponentLineNbr = null;
                fsxSOLineRow.EquipmentItemClass = equipmentItemClass;
            }
            else if (row is FSSODet)
            {
                FSSODet fsSODetRow = row as FSSODet;
                fsSODetRow.EquipmentAction = equipmentAction;
                fsSODetRow.SMEquipmentID = null;
                fsSODetRow.NewTargetEquipmentLineNbr = null;
                fsSODetRow.ComponentID = null;
                fsSODetRow.EquipmentLineRef = null;
                fsSODetRow.Comment = null;
                fsSODetRow.EquipmentItemClass = equipmentItemClass;
            }
            else if (row is FSAppointmentDet)
            {
                FSAppointmentDet fsAppointmentDetRow = row as FSAppointmentDet;
                fsAppointmentDetRow.EquipmentAction = equipmentAction;
                fsAppointmentDetRow.SMEquipmentID = null;
                fsAppointmentDetRow.NewTargetEquipmentLineNbr = null;
                fsAppointmentDetRow.ComponentID = null;
                fsAppointmentDetRow.EquipmentLineRef = null;
                fsAppointmentDetRow.Comment = null;
                fsAppointmentDetRow.EquipmentItemClass = equipmentItemClass;
            }
            else if (row is FSScheduleDet)
            {
                FSScheduleDet fsScheduleDetRow = row as FSScheduleDet;
                equipmentAction = equipmentAction != ID.Equipment_Action_Base.SELLING_TARGET_EQUIPMENT ? ID.Equipment_Action_Base.NONE : equipmentAction;

                fsScheduleDetRow.EquipmentAction = equipmentAction;
                fsScheduleDetRow.SMEquipmentID = null;
                fsScheduleDetRow.ComponentID = null;
                fsScheduleDetRow.EquipmentItemClass = equipmentItemClass;
            }
        }

        public static string getEquipmentModelAction(PXGraph graph, FSxEquipmentModel fsxEquipmentModelRow)
        {
            if (fsxEquipmentModelRow != null)
            {
                switch (fsxEquipmentModelRow.EquipmentItemClass)
                {
                    case ID.Equipment_Item_Class.COMPONENT:
                        return ID.Equipment_Action.CREATING_COMPONENT;
                    case ID.Equipment_Item_Class.MODEL_EQUIPMENT:
                        return ID.Equipment_Action.SELLING_TARGET_EQUIPMENT;
                    default:
                        return ID.Equipment_Action.NONE;
                }
            }

            return null;
        }

        public static void SetEquipmentFieldEnablePersistingCheck(PXCache cache, object row, bool updateQty = true)
        {
            if (!PXAccess.FeatureInstalled<FeaturesSet.equipmentManagementModule>())
            {
                return;
            }

            if (row is SOLine)
            {
                SOLine soLineRow = row as SOLine;
                FSxSOLine fsxSOLineRow = cache.GetExtension<FSxSOLine>(soLineRow);

                SetEquipmentFieldEnablePersistingCheck(cache,
                                      soLineRow,
                                      soLineRow.InventoryID,
                                      fsxSOLineRow.EquipmentItemClass,
                                      null,
                                      fsxSOLineRow.EquipmentAction,
                                      fsxSOLineRow.NewEquipmentLineNbr,
                                      fsxSOLineRow.SMEquipmentID,
                                      fsxSOLineRow.ComponentID,
                                      typeof(FSxSOLine.equipmentAction).Name,
                                      typeof(FSxSOLine.sMEquipmentID).Name,
                                      typeof(FSxSOLine.newEquipmentLineNbr).Name,
                                      typeof(FSxSOLine.componentID).Name,
                                      typeof(FSxSOLine.equipmentComponentLineNbr).Name,
                                      typeof(FSxSOLine.comment).Name,
                                      typeof(SOLine.orderQty).Name,
                                      updateQty);
            }
            else if (row is FSSODet)
            {
                FSSODet fsSODetRow = row as FSSODet;

                SetEquipmentFieldEnablePersistingCheck(cache,
                                      fsSODetRow,
                                      fsSODetRow.InventoryID,
                                      fsSODetRow.EquipmentItemClass,
                                      fsSODetRow.LineType,
                                      fsSODetRow.EquipmentAction,
                                      fsSODetRow.NewTargetEquipmentLineNbr,
                                      fsSODetRow.SMEquipmentID,
                                      fsSODetRow.ComponentID,
                                      typeof(FSSODet.equipmentAction).Name,
                                      typeof(FSSODet.SMequipmentID).Name,
                                      typeof(FSSODet.newTargetEquipmentLineNbr).Name,
                                      typeof(FSSODet.componentID).Name,
                                      typeof(FSSODet.equipmentLineRef).Name,
                                      typeof(FSSODet.comment).Name,
                                      typeof(FSSODet.estimatedQty).Name);
            }
            else if (row is FSAppointmentDet)
            {
                FSAppointmentDet fsAppointmentDet = row as FSAppointmentDet;

                SetEquipmentFieldEnablePersistingCheck(cache,
                                      fsAppointmentDet,
                                      fsAppointmentDet.InventoryID,
                                      fsAppointmentDet.EquipmentItemClass,
                                      fsAppointmentDet.LineType,
                                      fsAppointmentDet.EquipmentAction,
                                      fsAppointmentDet.NewTargetEquipmentLineNbr,
                                      fsAppointmentDet.SMEquipmentID,
                                      fsAppointmentDet.ComponentID,
                                      typeof(FSAppointmentDet.equipmentAction).Name,
                                      typeof(FSAppointmentDet.SMequipmentID).Name,
                                      typeof(FSAppointmentDet.newTargetEquipmentLineNbr).Name,
                                      typeof(FSAppointmentDet.componentID).Name,
                                      typeof(FSAppointmentDet.equipmentLineRef).Name,
                                      typeof(FSAppointmentDet.comment).Name,
                                      typeof(FSAppointmentDet.actualQty).Name,
                                      updateQty);
            }
            else if (row is FSScheduleDet)
            {
                FSScheduleDet fsScheduleDet = row as FSScheduleDet;

                SetEquipmentFieldEnablePersistingCheck(cache,
                                      fsScheduleDet,
                                      fsScheduleDet.InventoryID,
                                      fsScheduleDet.EquipmentItemClass,
                                      fsScheduleDet.LineType,
                                      fsScheduleDet.EquipmentAction,
                                      null,
                                      fsScheduleDet.SMEquipmentID,
                                      fsScheduleDet.ComponentID,
                                      typeof(FSScheduleDet.equipmentAction).Name,
                                      typeof(FSScheduleDet.SMequipmentID).Name,
                                      null,
                                      typeof(FSScheduleDet.componentID).Name,
                                      typeof(FSScheduleDet.equipmentLineRef).Name,
                                      null,
                                      typeof(FSScheduleDet.qty).Name);
            }
        }

        public static void ResetEquipmentFields(PXCache cache, object row)
        {
            if (row is SOLine)
            {
                SOLine soLineRow = row as SOLine;
                FSxSOLine fsxSOLineRow = cache.GetExtension<FSxSOLine>(soLineRow);

                fsxSOLineRow.SMEquipmentID = null;
                fsxSOLineRow.NewEquipmentLineNbr = null;
                fsxSOLineRow.ComponentID = null;
                fsxSOLineRow.EquipmentComponentLineNbr = null;
            }
            else if (row is FSSODet)
            {
                FSSODet fsSODetRow = row as FSSODet;

                fsSODetRow.SMEquipmentID = null;
                fsSODetRow.NewTargetEquipmentLineNbr = null;
                fsSODetRow.ComponentID = null;
                fsSODetRow.EquipmentLineRef = null;
            }
            else if (row is FSAppointmentDet)
            {
                FSAppointmentDet fsAppointmentDetRow = row as FSAppointmentDet;

                fsAppointmentDetRow.SMEquipmentID = null;
                fsAppointmentDetRow.NewTargetEquipmentLineNbr = null;
                fsAppointmentDetRow.ComponentID = null;
                fsAppointmentDetRow.EquipmentLineRef = null;
            }
            else if (row is FSScheduleDet)
            {
                FSScheduleDet fsScheduleDetRow = row as FSScheduleDet;

                fsScheduleDetRow.SMEquipmentID = null;
                fsScheduleDetRow.ComponentID = null;
                fsScheduleDetRow.EquipmentLineRef = null;
            }
        }

        public static void SetEquipmentFieldEnablePersistingCheck(PXCache cache, object row, int? inventoryID, string EquipmentItemClass, string lineType, string eQAction, object newTargetEQLineNbr, int? sMEquipmentID, int? componentID, string eQActionFieldName, string sMEquipmentIDFieldName, string newTargetEQFieldName, string componentIDFieldName, string equipmentLineRefFieldName, string commentFieldName, string quantityFieldName, bool updateQty = true)
        {
            if (inventoryID != null || lineType == ID.LineType_ALL.SERVICE)
            {
                bool enableComponentID = false;
                bool enableTargetEquipment = false;
                bool enableNewTargetEquipmentNbr = false;
                bool enableEquipmentLineRef = sMEquipmentID != null;
                bool enableQty = true;
                bool isComponentIDRequired = false;
                bool isEquipmentLineRefRequired = false;
                bool enableComment = false;

                if (EquipmentItemClass != null)
                {
                    switch (eQAction)
                    {
                        case ID.Equipment_Action.SELLING_TARGET_EQUIPMENT:

                            if (EquipmentItemClass != ID.Equipment_Item_Class.MODEL_EQUIPMENT)
                            {
                                cache.RaiseExceptionHandling(eQActionFieldName, row, eQAction, new PXSetPropertyException(TX.Error.EQUIPMENT_ACTION_MODEL_EQUIPMENT_REQUIRED));
                            }

                            break;

                        case ID.Equipment_Action.REPLACING_TARGET_EQUIPMENT:

                            if (EquipmentItemClass != ID.Equipment_Item_Class.MODEL_EQUIPMENT)
                            {
                                cache.RaiseExceptionHandling(eQActionFieldName, row, eQAction, new PXSetPropertyException(TX.Error.EQUIPMENT_ACTION_MODEL_EQUIPMENT_REQUIRED));
                            }
                            else
                            {
                                enableTargetEquipment = true;
                                enableNewTargetEquipmentNbr = enableEquipmentLineRef = enableQty = false;
                            }

                            break;

                        case ID.Equipment_Action.CREATING_COMPONENT:
                        case ID.Equipment_Action.UPGRADING_COMPONENT:
                        case ID.Equipment_Action.REPLACING_COMPONENT:

                            if (EquipmentItemClass != ID.Equipment_Item_Class.COMPONENT)
                            {
                                cache.RaiseExceptionHandling(eQActionFieldName, row, eQAction, new PXSetPropertyException(TX.Error.EQUIPMENT_ACTION_COMPONENT_REQUIRED));
                            }
                            else
                            {
                                enableComponentID = enableTargetEquipment = enableNewTargetEquipmentNbr = isComponentIDRequired = enableComment = true;
                                enableQty = false;

                                if (eQAction == ID.Equipment_Action.UPGRADING_COMPONENT)
                                {
                                    enableTargetEquipment = false;
                                }
                                else if (eQAction == ID.Equipment_Action.REPLACING_COMPONENT)
                                {
                                    enableNewTargetEquipmentNbr = false;
                                    enableEquipmentLineRef = true;
                                    isEquipmentLineRefRequired = true;
                                }
                            }

                            break;

                        case ID.Equipment_Action.NONE:
                            if (EquipmentItemClass == ID.Equipment_Item_Class.CONSUMABLE
                                    || EquipmentItemClass == ID.Equipment_Item_Class.PART_OTHER_INVENTORY)
                            {
                            enableTargetEquipment = enableNewTargetEquipmentNbr = true;
                            enableComponentID = newTargetEQLineNbr != null || sMEquipmentID != null;
                            }

                            isComponentIDRequired = false;
                            break;
                    }
                }

                if (eQAction == ID.Equipment_Action.NONE
                        && (lineType == ID.LineType_ALL.SERVICE ||
                            lineType == ID.LineType_ALL.NONSTOCKITEM ||
                            lineType == ID.LineType_ALL.INVENTORY_ITEM))
                {
                    enableComponentID = enableTargetEquipment = enableNewTargetEquipmentNbr = true;
                    isEquipmentLineRefRequired = enableEquipmentLineRef && componentID != null;
                }

                PXUIFieldAttribute.SetEnabled(cache, row, sMEquipmentIDFieldName, enableTargetEquipment && newTargetEQLineNbr == null);
                PXUIFieldAttribute.SetEnabled(cache, row, componentIDFieldName, enableComponentID);

                if (row is FSSODet && cache.Graph.Accessinfo.ScreenID != SharedFunctions.SetScreenIDToDotFormat(ID.ScreenID.SERVICE_ORDER)
                    || row is FSAppointmentDet && cache.Graph.Accessinfo.ScreenID != SharedFunctions.SetScreenIDToDotFormat(ID.ScreenID.APPOINTMENT))
                {
                    PXDefaultAttribute.SetPersistingCheck(cache, componentIDFieldName, row, PXPersistingCheck.Nothing);
                }
                else
                {
                    PXDefaultAttribute.SetPersistingCheck(cache, componentIDFieldName, row, isComponentIDRequired ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);
                }

                if (newTargetEQFieldName != null)
                {
                    PXUIFieldAttribute.SetEnabled(cache, row, newTargetEQFieldName, enableNewTargetEquipmentNbr
                                                                                        && sMEquipmentID == null);
                }

                if (equipmentLineRefFieldName != null)
                {
                    PXUIFieldAttribute.SetEnabled(cache, row, equipmentLineRefFieldName, enableEquipmentLineRef);
                    if (row is FSSODet && cache.Graph.Accessinfo.ScreenID != SharedFunctions.SetScreenIDToDotFormat(ID.ScreenID.SERVICE_ORDER)
                        || row is FSAppointmentDet && cache.Graph.Accessinfo.ScreenID != SharedFunctions.SetScreenIDToDotFormat(ID.ScreenID.APPOINTMENT))
                    {
                        PXDefaultAttribute.SetPersistingCheck(cache, equipmentLineRefFieldName, row, PXPersistingCheck.Nothing);
                    }
                    else
                    {
                        PXDefaultAttribute.SetPersistingCheck(cache, equipmentLineRefFieldName, row, isEquipmentLineRefRequired ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);
                    }

                }

                if (commentFieldName != null)
                {
                    PXUIFieldAttribute.SetEnabled(cache, row, commentFieldName, enableComment && componentID != null);
                }

                if (lineType != ID.LineType_ALL.SERVICE
                        && enableQty == false 
                        && cache.Graph.IsCopyPasteContext == false)
                {
                    if (updateQty)
                    {
                        cache.SetValueExt(row, quantityFieldName, 1.0m);
                    }
                    PXUIFieldAttribute.SetEnabled(cache, row, quantityFieldName, enableQty);
                }
            }

            if (newTargetEQFieldName != null &&
                (lineType == ID.LineType_ALL.COMMENT
                || lineType == ID.LineType_ALL.INSTRUCTION))
            {
                PXUIFieldAttribute.SetEnabled(cache, row, newTargetEQFieldName, sMEquipmentID == null);
            }
        }

        #endregion

        /// <summary>
        /// Creates note record in Note table in the RowInserted event.
        /// </summary>
        public static void InitializeNote(PXCache cache, PXRowInsertedEventArgs e)
        {
            if (string.IsNullOrEmpty(cache.Graph.PrimaryView))
            {
                return;
            }

            var noteCache = cache.Graph.Caches[typeof(Note)];
            var oldDirty = noteCache.IsDirty;
            PXNoteAttribute.GetNoteID(cache, e.Row, EntityHelper.GetNoteField(cache.Graph.Views[cache.Graph.PrimaryView].Cache.GetItemType()));
            noteCache.IsDirty = oldDirty;
        }

        public static int? GetCurrentEmployeeID(PXCache cache)
        {
            EPEmployee epEmployeeRow = EmployeeMaint.GetCurrentEmployee(cache.Graph);
            if (epEmployeeRow == null)
            {
                return null;
            }

            FSxEPEmployee fsxepEmployeeRow = PXCache<EPEmployee>.GetExtension<FSxEPEmployee>(epEmployeeRow);

            if (fsxepEmployeeRow.SDEnabled == true && epEmployeeRow.VStatus != VendorStatus.Inactive)
            {
                return epEmployeeRow.BAccountID;
            }

            return null;
        }

        public static DateTime? GetNextExecution(PXCache cache, FSSchedule fsScheduleRow)
        {
            bool expired = false;
            var generator = new TimeSlotGenerator();
            List<Scheduler.Schedule> mapScheduleResults = new List<Scheduler.Schedule>();

            mapScheduleResults = MapFSScheduleToSchedule.convertFSScheduleToSchedule(cache, fsScheduleRow, fsScheduleRow.LastGeneratedElementDate, ID.RecordType_ServiceContract.ROUTE_SERVICE_CONTRACT);
            return generator.GenerateNextOccurrence(mapScheduleResults, fsScheduleRow.LastGeneratedElementDate ?? fsScheduleRow.StartDate.Value, fsScheduleRow.EndDate, out expired);
        }

        public static bool? IsDisableFixScheduleAction(PXGraph graph)
        {
            FSSetup fsSetupRow = PXSelect<FSSetup>.Select(graph);

            return fsSetupRow.DisableFixScheduleAction;
        }

        public static string WarnUserWithSchedulesWithoutNextExecution(PXGraph graph, PXCache cache, PXAction fixButton, out bool warning)
        {
            PXGraph tempGraph = new PXGraph();
            warning = false;

            string warningMessage = "";

            if (IsDisableFixScheduleAction(graph) == false)
            {
                int count = SchedulesWithoutNextExecution(tempGraph);

                if (count > 0)
                {
                    warning = true;
                    fixButton.SetVisible(true);
                    warningMessage = TX.Warning.SCHEDULES_WITHOUT_NEXT_EXECUTION;
                }
            }

            return warningMessage;
        }

        public static int SchedulesWithoutNextExecution(PXGraph tempGraph)
        {
            return PXSelectReadonly<FSSchedule,
                   Where<
                       FSSchedule.nextExecutionDate, IsNull,
                       And<FSSchedule.active, Equal<True>>>>
                   .Select(tempGraph).Count;
        }

        public static void UpdateSchedulesWithoutNextExecution(PXGraph callerGraph, PXCache cache)
        {
            PXLongOperation.StartOperation(
                callerGraph,
                delegate
                {
                    using (PXTransactionScope ts = new PXTransactionScope())
                    {
                        var resultSet = PXSelectReadonly<FSSchedule,
                                        Where<
                                            FSSchedule.nextExecutionDate, IsNull,
                                            And<FSSchedule.active, Equal<True>>>>
                                        .Select(callerGraph);

                        foreach (FSSchedule fsScheduleRow in resultSet)
                        {
                            PXUpdate<
                                Set<FSSchedule.nextExecutionDate, Required<FSSchedule.nextExecutionDate>>,
                            FSSchedule,
                            Where<
                                FSSchedule.scheduleID, Equal<Required<FSSchedule.scheduleID>>>>
                            .Update(callerGraph, SharedFunctions.GetNextExecution(cache, fsScheduleRow), fsScheduleRow.ScheduleID);
                        }

                        PXUpdate<
                            Set<FSSetup.disableFixScheduleAction, Required<FSSetup.disableFixScheduleAction>>,
                        FSSetup>
                        .Update(callerGraph, true);

                        ts.Complete();
                    }
                });
        }

        public static bool GetEnableSeasonSetting(PXGraph graph, FSSchedule fsScheduleRow, FSSetup fsSetupRow = null)
        {
            bool enableSeasons = false;

            if (fsScheduleRow is FSRouteContractSchedule)
            {
                FSRouteSetup fsRouteSetupRow = ServiceManagementSetup.GetServiceManagementRouteSetup(graph);

                if (fsRouteSetupRow != null)
                {
                    enableSeasons = fsRouteSetupRow.EnableSeasonScheduleContract == true;
                }
            }
            else
            {
                if (fsSetupRow == null)
                {
                    fsSetupRow = ServiceManagementSetup.GetServiceManagementSetup(graph);
                }

                if (fsSetupRow != null)
                {
                    enableSeasons = fsSetupRow.EnableSeasonScheduleContract == true;
                }
            }

            return enableSeasons;
        }

        public static SharedClasses.SubAccountIDTupla GetSubAccountIDs(PXGraph graph, FSSrvOrdType fsSrvOrdTypeRow, int? inventoryID, int? branchID, int? locationID, int? branchLocationID, int? salesPersonID, bool isService = true)
        {
            INSite inSite = null;
            INPostClass inPostClassRow = null;
            SalesPerson salesPersonRow = null;

            InventoryItem inventoryItemRow = SharedFunctions.GetInventoryItemRow(graph, inventoryID);

            Location companyLocationRow = PXSelectJoin<Location,
                                          InnerJoin<BAccountR,
                                          On<
                                              Location.bAccountID, Equal<BAccountR.bAccountID>,
                                              And<Location.locationID, Equal<BAccountR.defLocationID>>>,
                                          InnerJoin<GL.Branch,
                                          On<
                                              BAccountR.bAccountID, Equal<GL.Branch.bAccountID>>>>,
                                          Where<
                                              GL.Branch.branchID, Equal<Required<GL.Branch.branchID>>>>
                                          .Select(graph, branchID);

            Location customerLocationRow = PXSelect<Location,
                                           Where<
                                               Location.locationID, Equal<Required<Location.locationID>>>>
                                           .Select(graph, locationID);

            FSBranchLocation fsBranchLocationRow = FSBranchLocation.PK.Find(graph, branchLocationID);

            int? inSiteID = isService ? fsBranchLocationRow?.DfltSiteID : inventoryItemRow?.DfltSiteID;

            inSite = PXSelect<INSite,
                     Where<
                         INSite.siteID, Equal<Required<INSite.siteID>>>>
                     .Select(graph, inSiteID);

            inPostClassRow = PXSelect<INPostClass,
                             Where<
                                 INPostClass.postClassID, Equal<Required<INPostClass.postClassID>>>>
                             .Select(graph, inventoryItemRow?.PostClassID);

            salesPersonRow = PXSelect<SalesPerson,
                             Where<
                                 SalesPerson.salesPersonID, Equal<Required<SalesPerson.salesPersonID>>>>
                             .Select(graph, salesPersonID);

            if (customerLocationRow == null
                    || inventoryItemRow == null
                        || fsSrvOrdTypeRow == null
                            || companyLocationRow == null
                                || fsBranchLocationRow == null)
            {
                return null;
            }
            else
            {
                int? branchLocation_SubID = fsBranchLocationRow?.SubID;
                int? branch_SubID = companyLocationRow?.CMPSalesSubID;
                int? inventoryItem_SubID = inventoryItemRow?.SalesSubID;
                int? customerLocation_SubID = customerLocationRow?.CSalesSubID;
                int? postingClass_SubID = inPostClassRow?.SalesSubID;
                int? salesPerson_SubID = salesPersonRow?.SalesSubID;
                int? srvOrdType_SubID = fsSrvOrdTypeRow?.SubID;
                int? warehouse_SubID = inSite?.SalesSubID;

                return new SharedClasses.SubAccountIDTupla(branchLocation_SubID, branch_SubID, inventoryItem_SubID, customerLocation_SubID, postingClass_SubID, salesPerson_SubID, srvOrdType_SubID, warehouse_SubID);
            }
        }

        public static bool ConcatenateNote(PXCache srcCache, PXCache dstCache, object srcObj, object dstObj)
        {
            string dstNote = PXNoteAttribute.GetNote(dstCache, dstObj);

            if (dstNote != string.Empty && dstNote != null)
            {
                string srcNote = PXNoteAttribute.GetNote(srcCache, srcObj);
                dstNote += System.Environment.NewLine + System.Environment.NewLine + srcNote;
                PXNoteAttribute.SetNote(dstCache, dstObj, dstNote);

                return false;
            }

            return true;
        }

        public static void CopyNotesAndFiles(PXCache srcCache, PXCache dstCache, object srcObj, object dstObj, bool? copyNotes, bool? copyFiles)
        {
            if (copyNotes == true)
            {
                copyNotes = ConcatenateNote(srcCache, dstCache, srcObj, dstObj);
            }

            PXNoteAttribute.CopyNoteAndFiles(srcCache, srcObj, dstCache, dstObj, copyNotes: copyNotes, copyFiles: copyFiles);
        }

        public static void CopyNotesAndFiles(PXCache cache, FSSrvOrdType fsSrvOrdTypeRow, object document, int? customerID, int? locationID)
        {
            bool alreadyAssignedNotesAndAttachments = false;

            if (fsSrvOrdTypeRow.CopyNotesFromCustomer == true
                    || fsSrvOrdTypeRow.CopyAttachmentsFromCustomer == true)
            {
                CustomerMaint customerMaintGraph = PXGraph.CreateInstance<CustomerMaint>();
                customerMaintGraph.BAccount.Current = customerMaintGraph.BAccount.Search<Customer.bAccountID>(customerID);

                CopyNotesAndFiles(customerMaintGraph.BAccount.Cache,
                                  cache,
                                  customerMaintGraph.BAccount.Current,
                                  document,
                                  fsSrvOrdTypeRow.CopyNotesFromCustomer,
                                  fsSrvOrdTypeRow.CopyAttachmentsFromCustomer);

                alreadyAssignedNotesAndAttachments = true;
            }

            if (fsSrvOrdTypeRow.CopyNotesFromCustomerLocation == true
                || fsSrvOrdTypeRow.CopyAttachmentsFromCustomerLocation == true)
            {
                CustomerLocationMaint customerLocationMaintGraph = PXGraph.CreateInstance<CustomerLocationMaint>();
                Customer customerRow = GetCustomerRow(cache.Graph, customerID);
                customerLocationMaintGraph.Location.Current = customerLocationMaintGraph.Location.Search<Location.locationID>
                                                        (locationID, customerRow.AcctCD);

                CopyNotesAndFiles(customerLocationMaintGraph.Location.Cache,
                                  cache,
                                  customerLocationMaintGraph.Location.Current,
                                  document,
                                  fsSrvOrdTypeRow.CopyNotesFromCustomerLocation,
                                  fsSrvOrdTypeRow.CopyAttachmentsFromCustomerLocation);

                alreadyAssignedNotesAndAttachments = true;
            }

            if (document.GetType() == typeof(FSAppointment)
                && (fsSrvOrdTypeRow.CopyNotesToAppoinment == true
                    || fsSrvOrdTypeRow.CopyAttachmentsToAppoinment == true))
            {
                FSServiceOrder fsServiceOrderRow = PXSelect<FSServiceOrder,
                                                   Where<
                                                       FSServiceOrder.sOID, Equal<Required<FSServiceOrder.sOID>>>>
                                                   .Select(cache.Graph, ((FSAppointment)document).SOID);

                string note = null;

                if (alreadyAssignedNotesAndAttachments == true)
                {
                    note = PXNoteAttribute.GetNote(cache, document);
                }

                bool needCopyNotes = (note == string.Empty || note == null) && fsSrvOrdTypeRow.CopyNotesToAppoinment.Value;

                CopyNotesAndFiles(new PXCache<FSServiceOrder>(cache.Graph),
                                  cache,
                                  fsServiceOrderRow,
                                  document,
                                  needCopyNotes,
                                  fsSrvOrdTypeRow.CopyAttachmentsToAppoinment);
            }
        }

        public static void CopyNotesAndFiles(PXCache dstCache, object lineDocument, IDocLine srcLineDocument, FSSrvOrdType fsSrvOrdTypeRow)
        {
            if (fsSrvOrdTypeRow.CopyLineNotesToInvoice == true
                || fsSrvOrdTypeRow.CopyLineAttachmentsToInvoice == true)
            {
                if (srcLineDocument.SourceTable == ID.TablePostSource.FSSO_DET)
                {
                    PXCache<FSSODet> cacheFSSODet = new PXCache<FSSODet>(dstCache.Graph);
                    CopyNotesAndFiles(cacheFSSODet, dstCache, srcLineDocument, lineDocument, fsSrvOrdTypeRow.CopyLineNotesToInvoice, fsSrvOrdTypeRow.CopyLineAttachmentsToInvoice);
                }
                else
                {
                    PXCache<FSAppointmentDet> cacheApp = new PXCache<FSAppointmentDet>(dstCache.Graph);
                    CopyNotesAndFiles(cacheApp, dstCache, srcLineDocument, lineDocument, fsSrvOrdTypeRow.CopyLineNotesToInvoice, fsSrvOrdTypeRow.CopyLineAttachmentsToInvoice);
                }
            }
        }

        public static PXResultset<FSPostBatch> GetPostBachByProcessID(PXGraph graph, Guid currentProcessID)
        {
            return (PXResultset<FSPostBatch>)PXSelectJoinGroupBy<FSPostBatch,
                                             InnerJoin<FSPostDoc,
                                             On<
                                                 FSPostDoc.batchID, Equal<FSPostBatch.batchID>>>,
                                             Where<
                                                 FSPostDoc.processID, Equal<Required<FSPostDoc.processID>>>,
                                             Aggregate<
                                                 GroupBy<FSPostBatch.batchID>>>
                                             .Select(graph, currentProcessID);
        }

        public static void ServiceContractDynamicDropdown(PXCache cache, FSServiceContract fsServiceContractRow)
        {
            if(fsServiceContractRow != null)
            {
                switch (fsServiceContractRow.RecordType)
                {
                    case ID.RecordType_ServiceContract.SERVICE_CONTRACT:

                        if (fsServiceContractRow.BillingType == ID.Contract_BillingType.STANDARDIZED_BILLINGS)
                        {
                            PXStringListAttribute.SetList<FSServiceContract.scheduleGenType>(cache, fsServiceContractRow, new Tuple<string, string>[]
                            {
                                new Tuple<string, string> (ID.ScheduleGenType_ServiceContract.SERVICE_ORDER, TX.ScheduleGenType_ServiceContract.SERVICE_ORDER),
                                new Tuple<string, string> (ID.ScheduleGenType_ServiceContract.APPOINTMENT, TX.ScheduleGenType_ServiceContract.APPOINTMENT),
                                new Tuple<string, string> (ID.ScheduleGenType_ServiceContract.NONE, TX.ScheduleGenType_ServiceContract.NONE)
                            });
                        }
                        else
                        {
                            PXStringListAttribute.SetList<FSServiceContract.scheduleGenType>(cache, fsServiceContractRow, new Tuple<string, string>[]
                            {
                                new Tuple<string, string> (ID.ScheduleGenType_ServiceContract.SERVICE_ORDER, TX.ScheduleGenType_ServiceContract.SERVICE_ORDER),
                                new Tuple<string, string> (ID.ScheduleGenType_ServiceContract.APPOINTMENT, TX.ScheduleGenType_ServiceContract.APPOINTMENT)
                            });
                        }
                        break;

                    case ID.RecordType_ServiceContract.ROUTE_SERVICE_CONTRACT:

                        if (fsServiceContractRow.BillingType == ID.Contract_BillingType.STANDARDIZED_BILLINGS)
                        {
                            PXStringListAttribute.SetList<FSServiceContract.scheduleGenType>(cache, fsServiceContractRow, new Tuple<string, string>[]
                            {
                                new Tuple<string, string> (ID.ScheduleGenType_ServiceContract.APPOINTMENT, TX.ScheduleGenType_ServiceContract.APPOINTMENT),
                                new Tuple<string, string> (ID.ScheduleGenType_ServiceContract.NONE, TX.ScheduleGenType_ServiceContract.NONE)
                            });
                        }
                        else
                        {
                            PXStringListAttribute.SetList<FSServiceContract.scheduleGenType>(cache, fsServiceContractRow, new Tuple<string, string>[]
                            {
                                new Tuple<string, string> (ID.ScheduleGenType_ServiceContract.APPOINTMENT, TX.ScheduleGenType_ServiceContract.APPOINTMENT)
                            });
                        }
                        break;

                    default:
                        break;
                }
            }
        }

        public static void DefaultGenerationType(PXCache cache, FSServiceContract fsServiceContractRow, PXFieldDefaultingEventArgs e)
        {
            if (fsServiceContractRow != null)
            {
                if (fsServiceContractRow.RecordType != null)
                {
                    switch (fsServiceContractRow.RecordType)
                    {
                        case ID.RecordType_ServiceContract.SERVICE_CONTRACT:
                            e.NewValue = ID.ScheduleGenType_ServiceContract.SERVICE_ORDER;
                            e.Cancel = true;
                            break;

                        case ID.RecordType_ServiceContract.ROUTE_SERVICE_CONTRACT:
                            e.NewValue = ID.ScheduleGenType_ServiceContract.APPOINTMENT;
                            e.Cancel = true;
                            break;

                        default:
                            break;
                    }
                }
            }
        }

        internal static int? GetEquipmentComponentID(PXGraph graph, int? smEquipmentID, int? equipmentLineNbr)
        {
            FSEquipmentComponent FSEquipmentComponentRow = PXSelect<FSEquipmentComponent,
                                                           Where<
                                                               FSEquipmentComponent.SMequipmentID, Equal<Required<FSEquipmentComponent.SMequipmentID>>,
                                                               And<FSEquipmentComponent.lineNbr, Equal<Required<FSEquipmentComponent.lineNbr>>>>>
                                                           .Select(graph, smEquipmentID, equipmentLineNbr);

            if (FSEquipmentComponentRow == null)
            {
                return null;
            }
            else
            {
                return FSEquipmentComponentRow.ComponentID;
            }
        }

        public static void SetVisibleEnableProjectTask<projectTaskField>(PXCache cache, object row, int? projectID)
            where projectTaskField : IBqlField
        {
            bool nonProject = ProjectDefaultAttribute.IsNonProject(projectID);
            PXUIFieldAttribute.SetVisible<projectTaskField>(cache, row, !nonProject);
            PXUIFieldAttribute.SetEnabled<projectTaskField>(cache, row, !nonProject);
            PXUIFieldAttribute.SetRequired<projectTaskField>(cache, !nonProject);
            PXDefaultAttribute.SetPersistingCheck<projectTaskField>(cache, row, !nonProject ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);
        }

        public static void SetEnableCostCodeProjectTask<projectTaskField, costCodeField>(PXCache cache, object row, string lineType, int? projectID)
            where projectTaskField : IBqlField
            where costCodeField : IBqlField
        {
            bool nonProject = ProjectDefaultAttribute.IsNonProject(projectID);

            bool enableField = lineType != ID.LineType_ServiceContract.COMMENT
                                && lineType != ID.LineType_ServiceContract.INSTRUCTION;

            PXUIFieldAttribute.SetEnabled<projectTaskField>(cache, row, !nonProject && enableField);
            PXUIFieldAttribute.SetEnabled<costCodeField>(cache, row, projectID != null && enableField);

            PXUIFieldAttribute.SetRequired<projectTaskField>(cache, !nonProject && enableField);
            PXDefaultAttribute.SetPersistingCheck<projectTaskField>(cache, row, !nonProject && enableField ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);
        }

        public static Dictionary<string, string> GetCalendarMessages()
        {
            var dict = new Dictionary<string, string>();

            foreach (var prop in typeof(TX.CalendarMessages).GetFields())
            {
                dict[prop.Name] = PXLocalizer.Localize(prop.GetValue(null).ToString(), typeof(TX.CalendarMessages).FullName);
            }

            int index = 0;
            string invariantName;

            foreach (string dayName in DateTimeFormatInfo.CurrentInfo.DayNames)
            {
                invariantName = DateTimeFormatInfo.InvariantInfo.DayNames[index];
                dict[invariantName] = dayName;
                index++;
            }

            index = 0;
            foreach (string abbrDayName in DateTimeFormatInfo.CurrentInfo.AbbreviatedDayNames)
            {
                invariantName = "abbr_" + DateTimeFormatInfo.InvariantInfo.DayNames[index];
                dict[invariantName] = abbrDayName;
                index++;
            }

            index = 0;
            foreach (string monthName in DateTimeFormatInfo.CurrentInfo.MonthNames)
            {
                invariantName = DateTimeFormatInfo.InvariantInfo.MonthNames[index];
                dict[invariantName] = monthName;
                index++;
            }

            index = 0;
            foreach (string abbrMonthName in DateTimeFormatInfo.CurrentInfo.AbbreviatedMonthNames)
            {
                invariantName = "abbr_" + DateTimeFormatInfo.InvariantInfo.MonthNames[index];
                dict[invariantName] = abbrMonthName;
                index++;
            }

            return dict;
        }

        public static void ValidatePostToByFeatures<postToField>(PXCache cache, object row, string postTo)
            where postToField : IBqlField
        {
            PXCache<FeaturesSet> featureSetCache = new PXCache<FeaturesSet>(cache.Graph);

            if (postTo == ID.Contract_PostTo.SALES_ORDER_MODULE
                && PXAccess.FeatureInstalled<FeaturesSet.distributionModule>() == false)
            {
                cache.RaiseExceptionHandling<postToField>(
                    row,
                    postTo,
                    new PXSetPropertyException(PXMessages.LocalizeFormat(
                        TX.Error.POST_TO_MISSING_FEATURES,
                        TX.Contract_PostTo.SALES_ORDER_MODULE,
                        PXUIFieldAttribute.GetDisplayName<FeaturesSet.distributionModule>(featureSetCache)),
                    PXErrorLevel.Error));
            }
            else if (postTo == ID.Contract_PostTo.SALES_ORDER_INVOICE
                        && PXAccess.FeatureInstalled<FeaturesSet.advancedSOInvoices>() == false)
            {
                cache.RaiseExceptionHandling<postToField>(
                    row,
                    postTo,
                    new PXSetPropertyException(PXMessages.LocalizeFormat(
                        TX.Error.POST_TO_MISSING_FEATURES,
                        TX.Contract_PostTo.SALES_ORDER_INVOICE,
                        PXUIFieldAttribute.GetDisplayName<FeaturesSet.advancedSOInvoices>(featureSetCache)),
                    PXErrorLevel.Error));
            }
        }

        public static bool IsLotSerialRequired(PXCache cache, int? inventoryID)
        {
            PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(cache, inventoryID);
            var lsClass = (INLotSerClass)item;
            if (lsClass == null || lsClass.LotSerTrack == null || lsClass.LotSerTrack == INLotSerTrack.NotNumbered)
                return false;

            return true;
        }

        public static PXResult<InventoryItem, INLotSerClass> ReadInventoryItem(PXCache sender, int? inventoryID)
        {
            if (inventoryID == null)
                return null;
            var inventory = InventoryItem.PK.Find(sender.Graph, inventoryID);
            if (inventory == null)
                throw new PXException(ErrorMessages.ValueDoesntExistOrNoRights, IN.Messages.InventoryItem, inventoryID);
            INLotSerClass lotSerClass;
            if (inventory.StkItem == true)
            {
                lotSerClass = INLotSerClass.PK.Find(sender.Graph, inventory.LotSerClassID);
                if (lotSerClass == null)
                    throw new PXException(ErrorMessages.ValueDoesntExistOrNoRights, IN.Messages.LotSerClass, inventory.LotSerClassID);
            }
            else
            {
                lotSerClass = new INLotSerClass();
            }
            return new PXResult<InventoryItem, INLotSerClass>(inventory, lotSerClass);
        }
    }
}