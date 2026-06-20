using NpgsqlTypes;

namespace PaperService.Entities
{
    public enum SyncStatus
    {
        [PgName("running")]
        Running,
        [PgName("success")]
        Success,
        [PgName("failed")]
        Failed,
        [PgName("cancelled")]
        Cancelled
    }
}
