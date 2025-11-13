namespace InCubeIntegration_DAL
{
    /// <summary>
    /// Errors returned by function in InCubeLibrary
    /// </summary>
    public enum InCubeErrors
    {
        /// <summary>
        /// Success. No errors to report.
        /// </summary>
        Success=0,
        /// <summary>
        /// General error.
        /// </summary>
        Error,
        NotInitialized,

        // Database
        DBCannotReadConfigurationFile,
        DBIncorrectDatabaseName,
        DBCannotOpenDatabase,
        DBCannotCloseDatabase,
        DBDatabaseAlreadyOpened,
        DBDatabaseNotOpened,
        DBTableNotOpened,
        DBTableAlreadyOpened,
        DBCannotOpenTable,
        DBNoMoreTables,
        DBNoMoreRows,
        DBCannotGetRow,
        DBFindFirstNotCalled,
        DBCannotExecuteCommand,
        DBIncorrectCommand,
        DBInvalidCriteria,
        DBInvalidFieldName,
        DBInvalidFieldIndex,
        DBNoCurrentRow,
        DBIndexOutOfRange,
        DBInvalidFieldType,
        DBDatabaseError,
        DBDatabaseConnectionError,
        DBQueryAlreadyOpened,
        DBCannotExecuteQuery,
        DBQueryNotOpened,
        DBTimeoutExpired,
        DBCannotInsertDuplicateKey,
        SuccessWithZeroRowAffected,
        // Security
        SEAuthorized,
        SEUnauthorized,
        SEInvalidUserNameOrPassowrd,
        SEFindFirstNotCalled,
        SERootMenu,
        SENotLoggedIn
    }
}
