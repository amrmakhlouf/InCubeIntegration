using System.Collections.Generic;
using InCubeLibrary;

namespace InCubeIntegration_BL
{
    public class Procedure
    {
        public string ProcedureName;
        public ProcType ProcedureType;
        public Dictionary<string, Parameter> Parameters;
        public string ExecutionTableName;
        public bool ReadExecutionDetails;
        public string ExecDetailsReadQry;
        public int MailTemplateID;
        public int DataTransferGroupID;
        public int ConnectionID;

        public Procedure()
        {
            ProcedureName = "";
            ProcedureType = ProcType.SQLProcedure;
            Parameters = new Dictionary<string, Parameter>();
            ExecutionTableName = "Int_ExecutionDetails";
            ExecDetailsReadQry = @"SELECT ID, Message +
CASE ISNULL(Inserted,0) WHEN 0 THEN '' ELSE ', ' + CAST(Inserted AS nvarchar(10)) + ' Inserted' END +
CASE ISNULL(Updated,0) WHEN 0 THEN '' ELSE ', ' + CAST(Updated AS nvarchar(10)) + ' Updated' END +
CASE ISNULL(Skipped,0) WHEN 0 THEN '' ELSE ', ' + CAST(Skipped AS nvarchar(10)) + ' Skipped' END
AS Message
FROM @ExecutionTable
WHERE TriggerID = @TriggerID AND ID > @ProcessID
AND ResultID IS NOT NULL";
            ReadExecutionDetails = false;
            MailTemplateID = 0;
            DataTransferGroupID = 0;
            ConnectionID = 0;
        }
        public Procedure(string ProcName) 
            : this()
        {
            ProcedureName = ProcName;
        }
        public void AddParameter(string Name, ParamType Type, object Value)
        {
            Parameter Par = new Parameter();
            Par.ParameterType = Type;
            Par.ParameterName = Name;
            Par.ParameterValue = Value;
            Par.Direction = ParamDirection.Input;
            Parameters.Add(Par.ParameterName, Par);
        }
        public void AddParameter(string Name, ParamType Type, string Value, ParamDirection direction)
        {
            Parameter Par = new Parameter();
            Par.ParameterType = Type;
            Par.ParameterName = Name;
            Par.ParameterValue = Value;
            Par.Direction = direction;
            Parameters.Add(Par.ParameterName, Par);
        }
    }
}
