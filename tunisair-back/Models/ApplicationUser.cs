using Microsoft.AspNetCore.Identity;

namespace tunisair_back.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string Name { get; set; }
        public string Type { get; set; } // Client, Livreur, Restaurant, Admin
        public string Status { get; set; } = "Active";
    }
}
