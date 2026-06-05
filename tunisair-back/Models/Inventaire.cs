using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace tunisair_back.Models
{
    public enum StatutInventaire
    {
        Ouvert,
        EnCours,
        Ferme
    }

    public class Inventaire
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

    
        public string Depot { get; set; }


        [DataType(DataType.DateTime)]
        public DateTime DateCreation { get; set; } = DateTime.Now;
        public DateTime DateInventaire { get; set; }
        public bool? IsImport { get; set; } = false; // ✅ Clé étrangère

        public StatutInventaire Statut { get; set; } = StatutInventaire.Ouvert;
        public ICollection<UserInventaire> UserInventaires { get; set; }

    }
}
