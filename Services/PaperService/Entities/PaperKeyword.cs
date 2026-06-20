using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PaperService.Entities
{
    [Table("paper_keywords")]
    public class PaperKeyword
    {
        [Column("paper_id")]
        public Guid PaperId { get; set; }
        public Paper? Paper { get; set; }

        [Column("keyword_id")]
        public Guid KeywordId { get; set; }
        public Keyword? Keyword { get; set; }

        [Column("relevance_score")]
        public double? RelevanceScore { get; set; }
    }
}
