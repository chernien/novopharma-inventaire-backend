using System.ComponentModel.DataAnnotations;

namespace tunisair_back.DTO
{
    public class UserDto

    {
        public Guid Id { get; set; } = Guid.NewGuid();

   
        public string Username { get; set; }

  
        public string Name { get; set; }
    
        public string Email { get; set; }


        public string? Role { get; set; } = "User";

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
