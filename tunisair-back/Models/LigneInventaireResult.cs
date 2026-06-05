namespace tunisair_back.Models
{
    public class LigneInventaireResult
    {
        public string ArRef { get; set; }
        public string ArDesign { get; set; }
        public string Depot { get; set; }
        public decimal QuantiteTheorique { get; set; }
        public decimal QuantiteComptage1 { get; set; }
        public decimal QuantiteComptage2 { get; set; }
        public decimal EcartTheorique { get; set; }
        public decimal EcartPhysique { get; set; }
        public string Statut { get; set; }
        public decimal QuantiteFinale { get; set; }
        public string Justification { get; set; }
        public string Superviseur1 { get; set; }
        public string Superviseur2 { get; set; }
    }
}
