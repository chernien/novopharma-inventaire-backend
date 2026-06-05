using System.ComponentModel.DataAnnotations;

namespace tunisair_back.Models
{
    public class UserDepot
    {
        [Key]
        public int Id { get; set; }

        public Guid UserId { get; set; }
        public User User { get; set; }

        public int DeNo { get; set; }
        public FDepot Depot { get; set; }
        public string DeIntitule { get; set; } // 👈 nom du dépôt à stocker

    }
}
