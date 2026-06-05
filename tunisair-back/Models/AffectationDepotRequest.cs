namespace tunisair_back.Models
{

    public class AffectationDepotRequest
    {
        public Guid UserId { get; set; }
        public List<int> DepotIds { get; set; }
    }
}
