using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using tunisair_back.DTO;
using tunisair_back.Models;

namespace tunisair_back.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly DTHDLGContext _context;
        private readonly IConfiguration _config;

        public AuthController(DTHDLGContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetAllUsers()
        {
            return await _context.Users.ToListAsync();
        }
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return BadRequest("Email déjà utilisé.");

            var user = new User
            {
                Username = dto.Username,
                Email = dto.Email,
                Name=dto.Name,
                IsActive=true,
                Role = dto.Type,
                PasswordHash = PasswordHasher.HashPassword(dto.Password)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(user);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

                if (user == null || !PasswordHasher.VerifyPassword(dto.Password, user.PasswordHash))
                    return Unauthorized("Email ou mot de passe incorrect.");

                // Génère un JWT token
                var token = GenerateJwtToken(user);

                return Ok(new { token, user });
            }
            catch (Exception ex)
            {
                // 💥 Journalisation de l'erreur dans la console
                Console.WriteLine("❌ Exception dans Login : " + ex.Message);
                return StatusCode(500, "Erreur serveur : " + ex.Message);
            }
        }


        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role ?? "User")
        };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                _config["Jwt:Issuer"],
                _config["Jwt:Audience"],
                claims,
                expires: DateTime.Now.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        [HttpGet("{idOrEmail}")]
        public async Task<ActionResult<User>> GetUserByIdOrEmail(string idOrEmail)
        {
            User? user;

            if (Guid.TryParse(idOrEmail, out Guid userId))
            {
                user = await _context.Users.FindAsync(userId);
            }
            else
            {
                user = await _context.Users.FirstOrDefaultAsync(u => u.Email == idOrEmail);
            }

            if (user == null)
                return NotFound("Utilisateur non trouvé");

            return Ok(new
            {
                user.Id,
                user.Username,
                user.Email,
                user.Role,
                user.IsActive,
                user.CreatedAt
            });
        }

        // PUT: api/User/{id}/ChangePassword
        public class ChangePasswordDto
        {
            public string NewPassword { get; set; }
        }

        [HttpPut("{id}/ChangePassword")]
        public async Task<IActionResult> ChangePasswordByAdmin(Guid id, [FromBody] ChangePasswordDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound("Utilisateur non trouvé");

            if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 6)
                return BadRequest("Mot de passe trop court");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);

            try
            {
                await _context.SaveChangesAsync();
                return Ok("Mot de passe mis à jour");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erreur interne : " + ex.Message);
            }
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound("Utilisateur non trouvé");

            var userName = string.IsNullOrEmpty(user.Name) ? user.Email : user.Name; // Utiliser email si nom est vide
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"L'utilisateur {userName} a été supprimé avec succès." });
        }



        public static class PasswordHasher
        {
            public static string HashPassword(string password)
            {
                return BCrypt.Net.BCrypt.HashPassword(password);
            }

            public static bool VerifyPassword(string password, string hash)
            {
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
        }
    }
}
