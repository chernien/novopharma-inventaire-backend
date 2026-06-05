#nullable disable
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace tunisair_back.Models
{
    [Table("F_ARTENUMREF")]
    public class FArtEnumRef
    {
        [Key]
        [Column("AE_CodeBarre")]
        [StringLength(19)]
        public string AE_CodeBarre { get; set; }

        [Required]
        [Column("AR_Ref")]
        [StringLength(19)]
        public string AR_Ref { get; set; }
    }
}
