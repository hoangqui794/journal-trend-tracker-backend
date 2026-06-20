using NpgsqlTypes;

namespace PaperService.Entities
{
    public enum PaperSource
    {
        [PgName("openalex")]
        OpenAlex,
        [PgName("semantic_scholar")]
        SemanticScholar,
        [PgName("crossref")]
        Crossref
    }
}
