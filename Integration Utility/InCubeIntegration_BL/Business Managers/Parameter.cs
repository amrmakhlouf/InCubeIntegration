using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InCubeLibrary;

namespace InCubeIntegration_BL
{
    public class Parameter
    {
        public ParamType ParameterType;
        public string ParameterName;
        public object ParameterValue;
        public ParamDirection Direction;

        public Parameter()
        {
            ParameterType = ParamType.Integer;
            ParameterName = "";
            ParameterValue = "";
            Direction = ParamDirection.Input;
        }
    }
}
