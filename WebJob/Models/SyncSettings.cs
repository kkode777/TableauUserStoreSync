namespace TableauSyncWebJob.Models
{
    public class SyncSettings
    {
        public string ConnectionString { get; set; }
        public string TableauSyncThreshold { get; set; }
        public bool SyncEnabled { get; set; }

        public int CommandTimeout { get; set; }
    }
}
