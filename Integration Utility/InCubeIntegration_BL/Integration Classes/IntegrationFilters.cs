using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InCubeLibrary;

namespace InCubeIntegration_BL
{
    public class IntegrationFilters
    {
        private int _warehouseID;
        public int WarehouseID
        {
            get { return _warehouseID; }
        }
        private int _employeeID;
        public int EmployeeID
        {
            get { return _employeeID; }
        }
        private int _organizationID;
        public int OrganizationID
        {
            get { return _organizationID; }
        }
        private DateTime _stockDate;
        public DateTime StockDate
        {
            get { return _stockDate; }
        }
        private DateTime _fromDate;
        public DateTime FromDate
        {
            get { return _fromDate; }
        }
        private DateTime _toDate;
        public DateTime ToDate
        {
            get { return _toDate; }
        }
        private bool _openInvoicesOnly;
        public bool OpenInvoicesOnly
        {
            get { return _openInvoicesOnly; }
        }
        private string _customerCode;
        public string CustomerCode
        {
            get { return _customerCode; }
        }
        private string _specialFunctionFilter;
        public string SpecialFunctionFilter
        {
            get { return _specialFunctionFilter; }
        }
        private string _extraSendFilter;
        public string ExtraSendFilter
        {
            get { return _extraSendFilter; }
        }
        private string _textSearch;
        public string TextSearch
        {
            get { return _textSearch; }
        }

        public IntegrationFilters(ActionType actionType)
        {
            _warehouseID = -1;
            _employeeID = -1;
            _organizationID = -1;
            _stockDate = DateTime.Today;
            if (actionType == ActionType.Send)
            {
                _fromDate = DateTime.Today;
                _toDate = DateTime.Today;
            }
            else
            {
                _fromDate = DateTime.MinValue;
                _toDate = DateTime.MaxValue;
            }
            _openInvoicesOnly = true;
            _customerCode = "";
            _specialFunctionFilter = "";
            _extraSendFilter = "";
            _textSearch = "";
        }

        public void SetValue(BuiltInFilters Filter, object FilterValue)
        {
            try
            {
                switch (Filter)
                {
                    case BuiltInFilters.Organization:
                        _organizationID = Convert.ToInt32(FilterValue);
                        break;
                    case BuiltInFilters.Employee:
                        _employeeID = Convert.ToInt32(FilterValue);
                        break;
                    case BuiltInFilters.Warehouse:
                        _warehouseID = Convert.ToInt32(FilterValue);
                        break;
                    case BuiltInFilters.FromDate:
                        _fromDate = ParseDateFilter(FilterValue.ToString());
                        break;
                    case BuiltInFilters.ToDate:
                        _toDate = ParseDateFilter(FilterValue.ToString());
                        break;
                    case BuiltInFilters.StockDate:
                        _stockDate = ParseDateFilter(FilterValue.ToString());
                        break;
                    case BuiltInFilters.DataTransferCheckList:
                    case BuiltInFilters.DatabaseBackupJob:
                    case BuiltInFilters.FilesManagementJobs:
                        _specialFunctionFilter = FilterValue.ToString();
                        break;
                    case BuiltInFilters.ExtraSendFilter:
                        _extraSendFilter = FilterValue.ToString();
                        break;
                    case BuiltInFilters.CustomerCode:
                        _customerCode = FilterValue.ToString();
                        break;
                    case BuiltInFilters.OpenInvoicesOnly:
                        _openInvoicesOnly = Convert.ToBoolean(FilterValue);
                        break;
                    case BuiltInFilters.TextSearch:
                        _textSearch = FilterValue.ToString();
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        private DateTime ParseDateFilter(string date)
        {
            DateTime dateValue = DateTime.Today;
            try
            {
                if (!DateTime.TryParse(date, out dateValue))
                {
                    dateValue = DateTime.Today;
                    string[] dateParts = date.Split(new char[] { '-', '+' });
                    if (dateParts.Length == 2)
                    {
                        string offset = date.Substring(date.Length - dateParts[1].Length - 1);
                        dateValue = DateTime.Today.AddDays(int.Parse(offset));
                    }
                    else if (date.Length == 10)
                    {
                        //dd-MM-yyyy
                        dateValue = new DateTime(int.Parse(date.Substring(6, 4)), int.Parse(date.Substring(3, 2)), int.Parse(date.Substring(0, 2)));
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return dateValue;
        }
    }
}
