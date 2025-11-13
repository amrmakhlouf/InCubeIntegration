using System.Data;

namespace InCubeLibrary
{
    public class Queue
    {
        public DataTable Schedules;
        public DataTable Filters;
        public string Key;
        public bool IsRunning;
        public Queue(string key)
        {
            Schedules = new DataTable();
            Filters = new DataTable();
            Key = key;
            IsRunning = false;
        }
    }
}
