using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PaperService.Entities
{
    [Table("paper_authors")]
    public class PaperAuthor
    {
        [Column("paper_id")]
        public Guid PaperId { get; set; }
        public Paper? Paper { get; set; }

        [Column("author_id")]
        public Guid AuthorId { get; set; }
        public Author? Author { get; set; }

        [Column("author_order")]
        public short AuthorOrder { get; set; } = 0;
    }
}
