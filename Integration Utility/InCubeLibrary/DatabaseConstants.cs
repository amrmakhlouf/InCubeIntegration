using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InCubeLibrary
{
    public class DatabaseConstants
    {

        public class QueryColumnsNames
        {
#if SQL_SERVER
            public const string Description = "[Description]";

#elif ORACLE
            public const string Description = "Description";
#endif

        }
        public class TableNames
        {
#if SQL_SERVER
            public const string Transaction = "[Transaction]";
            public const string Route = "[Route]";
            public const string Comment = "Comment";
#elif ORACLE
            public const string Transaction = "Transaction";
            public const string Route = "Route";
            public const string Comment = "\"COMMENT\"";
#endif
        }

        public class CharactersAndDataTypes
        {

#if SQL_SERVER
            public const char ParameterIdentifier = '@';
            public const string Concatenation = "+";
            public const string Nvarchar = "nvarchar";
            public const string OrderBy = "{0}.";
            public const string LeftBracket = "[";
            public const string RightBracket = "]";
            public const string StringQuoteSpace = "' ";
            public const string SpaceStringQuote = " '";
            public const string ISNULLCheck = "ISNULL";
            public const string StringQuote = "'";

#elif ORACLE
            public const char ParameterIdentifier = ':';
            public const string Concatenation = "||";
            public const string Nvarchar = "nvarchar2";
            public const string OrderBy = "";
            public const string LeftBracket = "";
            public const string RightBracket = "";
            public const string StringQuoteSpace = "\" ";
            public const string SpaceStringQuote = " \"";
            public const string ISNULLCheck = "nvl";
            public const string StringQuote = "\"";


#endif
        }

        public class PredefinedQueries
        {
 #if SQL_SERVER
            public const string GetTablesForReports = "SELECT name  FROM sysobjects WHERE xtype = 'U' and name not like 'DB%' and name not like 'Report%' and name not in  ('Application','ApplicationLanguage','Menu','MenuLanguage','OperatorPrivilege','OperatorSecurityGroup','SynchronizedTables ','FilterType','FilterTypeLanguage','Languages','NewCustomerTable','SecurityGroup','SecurityGroupLanguage','SecurityGroupPrivilege','PARAMETERLANGUAGE','PARAMETER') order by name"; 
             public const string GetLanguageTablesForReports = "SELECT name  FROM sysobjects WHERE xtype = 'U' and name not like 'DB%' and name Like '%Language' and name not like 'Report%' and name not in  ('Application','ApplicationLanguage','Menu','MenuLanguage','OperatorPrivilege','OperatorSecurityGroup','SynchronizedTables ','FilterType','FilterTypeLanguage','Languages','NewCustomerTable','SecurityGroup','SecurityGroupLanguage','SecurityGroupPrivilege','PARAMETERLANGUAGE','PARAMETER') order by name"; 
#elif ORACLE
            public const string GetTablesForReports = "SELECT distinct table_name name FROM cols where table_name not like 'DB%' and table_name not like 'REPORT%' and table_name  not in   ('APPLICATION','APPLICATIONLANGUAGE','MENU','MENULANGUAGE','OPERATORPRIVILEGE','OPERATORSECURITYGROUP','SYNCHRONIZEDTABLES ','FILTERTYPE','FILTERTYPELANGUAGE','LANGUAGES','NEWCUSTOMERTABLE','SECURITYGROUP','SECURITYGROUPLANGUAGE','SECURITYGROUPPRIVILEGE','PARAMETERLANGUAGE','PARAMETER','FORM','FORMCONTROL','FORMCONTROLCOLUMN','FORMCONTROLCOLUMNLANGUAGE','FORMCONTROLLANGUAGE','FORMCONTROLPARAMETER','FORMLANGUAGE') order by table_name";
            public const string GetLanguageTablesForReports = "SELECT distinct table_name name FROM cols where table_name not like 'DB%' and table_name not like 'REPORT%' and table_name Like '%LANGUAGE'  and table_name  not in  ('APPLICATION','APPLICATIONLANGUAGE','MENU','MENULANGUAGE','OPERATORPRIVILEGE','OPERATORSECURITYGROUP','SYNCHRONIZEDTABLES ','FILTERTYPE','FILTERTYPELANGUAGE','LANGUAGES','NEWCUSTOMERTABLE','SECURITYGROUP','SECURITYGROUPLANGUAGE','SECURITYGROUPPRIVILEGE','PARAMETERLANGUAGE','PARAMETER','FORM','FORMCONTROL','FORMCONTROLCOLUMN','FORMCONTROLCOLUMNLANGUAGE','FORMCONTROLLANGUAGE','FORMCONTROLPARAMETER','FORMLANGUAGE') order by table_name"; 
#endif

            public static string GetTablesColumn(string TableName)
            {
                string GetColumnsString = string.Empty;
                try
                {
                     #if SQL_SERVER
                    GetColumnsString="select column_name,data_type,is_nullable,column_default from information_schema.columns  where table_name = '" + TableName + "'order by ordinal_position";
#elif ORACLE
                    GetColumnsString = "select column_name,data_type,nullable is_nullable,data_default column_default  from cols where table_name ='" + TableName + "'";
#endif
                }
                catch (Exception ex)
                {
                    General.Common.ErrorLogger.LogError(((System.Reflection.MemberInfo)(typeof(DatabaseConstants))).Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace);
                }
                return GetColumnsString;
            }
        }
    }
}
