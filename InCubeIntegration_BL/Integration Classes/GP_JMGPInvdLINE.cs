using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InCubeIntegration_DAL;

namespace InCubeIntegration_BL
{
    public class GP_JMGPInvdLINE
    {
        private string _transactionID = string.Empty;

        public string TransactionID
        {
            get { return _transactionID; }
            set { _transactionID = value; }
        }
        private string _itemNumber = string.Empty;

        public string ItemNumber
        {
            get { return _itemNumber; }
            set { _itemNumber = value; }
        }
        private string _gatePass = string.Empty;

        public string GatePass
        {
            get { return _gatePass; }
            set { _gatePass = value; }
        }
        private decimal _quantity = 0;

        public decimal Quantity
        {
            get { return _quantity; }
            set { _quantity = value; }
        }
    }
}
