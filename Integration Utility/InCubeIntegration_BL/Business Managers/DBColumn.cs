using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InCubeLibrary;

namespace InCubeIntegration_BL
{
    public class DBColumn
    {
        public string Name;
        public ColumnType Type;
        public object Value;

        public DBColumn()
        {
            Name = "";
            Type = ColumnType.String;
            Value = null;
        }
    }
}
