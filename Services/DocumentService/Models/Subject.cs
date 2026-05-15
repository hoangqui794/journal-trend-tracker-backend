using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DocumentService.Models
{
    [Table("subjects")]
    public class Subject
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Required]
        [Column("code")]
        [MaxLength(50)]
        public string Code { get; set; } = null!;

        [Required]
        [Column("name")]
        [MaxLength(255)]
        public string Name { get; set; } = null!;

        [Column("description")]
        public string? Description { get; set; }
    }
}
