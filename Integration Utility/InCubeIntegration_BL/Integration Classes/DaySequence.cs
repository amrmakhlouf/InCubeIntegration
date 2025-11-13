using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InCubeIntegration_DAL;

namespace InCubeIntegration_BL
{
    public class DaySequence
    {
        private string _weekDay = string.Empty;

        public string WeekDay
        {
            get { return _weekDay; }
            set { _weekDay = value; }
        }
        private string _visitSequence = string.Empty;

        public string VisitSequence
        {
            get { return _visitSequence; }
            set { _visitSequence = value; }
        }
        private string _routeID = string.Empty;

        public string RouteID
        {
            get { return _routeID; }
            set { _routeID = value; }
        }

    }
}
