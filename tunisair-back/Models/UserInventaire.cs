namespace tunisair_back.Models
{
    public class UserInventaire
    {
        public Guid UserId { get; set; }
        public User User { get; set; }

        public int InventaireId { get; set; }
        public Inventaire Inventaire { get; set; }
    }
}
