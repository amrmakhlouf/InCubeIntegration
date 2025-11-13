using InCubeIntegration_DAL.Security;

namespace InCubeIntegration_DAL
{
    public class InCubeSecurityClass
    {
        public InCubeDatabase Database;
        public InCubeDatabase WarehouseDatabase;
        public InCubeTable ApplicationTable = new InCubeTable();
        public InCubeTable OperatorTable = new InCubeTable();
        public InCubeTable OperatorLanguageTable = new InCubeTable();
        public InCubeTable OperatorPrivilegeTable = new InCubeTable();
        public InCubeTable OperatorSecurityGroupTable = new InCubeTable();
        public InCubeTable SecurityGroupTable = new InCubeTable();
        public InCubeTable SecurityGroupPrivilegeTable = new InCubeTable();
        public InCubeTable ErrorMessageTable = new InCubeTable();
        public InCubeTable ConfigurationTable = new InCubeTable();
        public InCubeTable ConfigurationSecurityGroupTable = new InCubeTable();
        public InCubeTable ConfigurationOrganizationTable = new InCubeTable();
        private string _encryptionKey = "!@#$%^&**&^%$#@!";
        private EncryptionManager _ecryptionManager;
        private string _dataSourceFilePath;

        public InCubeSecurityClass()
        {
            _ecryptionManager = new EncryptionManager(_encryptionKey);
            _dataSourceFilePath = string.Empty;
        }
        public string EncryptData(string data)
        {
            return _ecryptionManager.EncryptData(data);
        }
        public string DecryptData(string data)
        {
            return _ecryptionManager.DecryptData(data);
        }
        public enum ApplicationType
        {
            InVan = 0,
            WMS = 1,
            AMS = 2,
        }
    }
}