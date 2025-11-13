using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InCubeLibrary
{
    public class FieldItem
    {
        public string Description;
        public int FieldID
        {
            get { return Field.GetHashCode(); }
        }
        public IntegrationField Field;
        public bool DefaultCheck;
        public ActionType Type;
        public FieldItem()
        {
            Description = "";
            DefaultCheck = false;
        }
    }
}
