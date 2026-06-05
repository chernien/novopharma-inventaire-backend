namespace tunisair_back.DTO
{
    public class RapportInventaireDto
    {
        public int Id { get; set; }
        public string Ref { get; set; }
        public string Designation { get; set; }
        public string? nserie { get; set; }
        public string? gamme1 { get; set; }
        public string Depot { get; set; }
        public decimal? QuantiteTheorique { get; set; }
        public decimal? QuantiteComptage1 { get; set; }
        public decimal? QuantiteComptage2 { get; set; }
        public decimal? QuantiteFinale { get; set; }
        public string Superviseur1 { get; set; }
        public string Superviseur2 { get; set; }

    }

}
