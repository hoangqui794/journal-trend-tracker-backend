using NpgsqlTypes;

namespace PaperService.Entities
{
    public enum KeywordSource
    {
        [PgName("user")]
        User,
        [PgName("api")]
        Api
    }
}
