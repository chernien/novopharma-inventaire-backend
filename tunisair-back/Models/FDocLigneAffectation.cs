//namespace tunisair_back.Models
//{
//    public class FDocLigneAffectation
//    {
//        public int Id { get; set; }

//        public short? DO_Domaine { get; set; }
//        public short? DO_Type { get; set; }
//        public int DE_No { get; set; }
//        public short? DL_MvtStock { get; set; }
//        public string? DO_Piece { get; set; }
//        public string? AR_REF { get; set; }
//        public string? DL_Design { get; set; }
//        public DateTime? DO_Date { get; set; }
//        public decimal DL_QTE { get; set; }

//        public decimal QuantiteRecu { get; set; } = 0;
//        public string Statut { get; set; } = "En attente";
//        public ICollection<FDocLigneAffectationUser> AffectationUsers { get; set; }

//        public Guid UserId { get; set; }
//        public User User { get; set; }
//        public string? Justification { get; set; }
//    }
//}
