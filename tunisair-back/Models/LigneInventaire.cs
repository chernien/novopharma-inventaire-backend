using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace tunisair_back.Models
{
    public class LigneInventaire
    {
        public int Id { get; set; }
        public int InventaireId { get; set; }
        public string ArRef { get; set; }
        public string? ArDesign { get; set; }
        public decimal? ArPunet { get; set; }
        public int Quantite { get; set; }
        public string Username { get; set; }
        public string Depot { get; set; }
        public string? NumSerie { get; set; }
        public string? Gamme1 { get; set; }
        public string? Gamme2 { get; set; }
        public string? TypeDocument { get; set; }
        public DateTime? DateInventaire { get; set; }
        public string? Domaine { get; set; }
        public DateTime DateSaisie { get; set; } = DateTime.Now;
    }
}
