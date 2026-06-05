using System.Text.Json.Serialization;

namespace tunisair_back.Models
{
    public class ProduitInventaire
    {
        public int Id { get; set; }

        public string Ref { get; set; }
        public string Designation { get; set; }
        public string? Famille { get; set; }
        public string? Gamme1 { get; set; }
        public string? Gamme2 { get; set; }
        public string? Nserie { get; set; }
        public int Quantite { get; set; }
        public decimal? PrixUnitaire { get; set; }
        public decimal? MontantTotal { get; set; }
        public string? SuiviStock { get; set; }
        public string? Depot { get; set; }
        public decimal? QuantiteTheorique { get; set; }
        public decimal? QuantiteComptage1 { get; set; }
        public decimal? QuantiteComptage2 { get; set; }
        public decimal? EcartTheorique { get; set; }
        public decimal? EcartPhysique { get; set; }
        public DateTime? DatePeremption { get; set; }
        public string? Statut { get; set; }
        public decimal? QuantiteFinale { get; set; }
        public string? Justification { get; set; }
        public string? Superviseur1 { get; set; }
        public string? Superviseur2 { get; set; }
        public int InventaireId { get; set; } 
        [JsonIgnore]
        public virtual Inventaire Inventaire { get; set; }
        public DateTime DateAjout { get; set; } = DateTime.Now;
        public string? Commentaire { get; set; }

    }

}
