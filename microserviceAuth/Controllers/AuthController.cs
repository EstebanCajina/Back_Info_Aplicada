namespace microserviceAuth.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.IdentityModel.Tokens;
    using microserviceAuth.Models;
    using System;
    using System.Text;
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Configuration;

    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _configuration;

        public AuthController(UserManager<User> userManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            // Convertir la fecha de nacimiento de string a DateTime
            if (!DateTime.TryParseExact(dto.DateOfBirth, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var dateOfBirth))
            {
                return BadRequest("Invalid date format. Please use dd/MM/yyyy.");
            }

            var user = new User
            {
                UserName = dto.Username,
                Email = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                DateOfBirth = dateOfBirth
            };

            var result = await _userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            return Ok("User registered successfully");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _userManager.FindByNameAsync(dto.Username);
            if (user == null || !(await _userManager.CheckPasswordAsync(user, dto.Password)))
            {
                return Unauthorized("Invalid login attempt");
            }

            // Genera un sessionToken único para validar la sesión
            var newSessionToken = Guid.NewGuid().ToString();
            user.SessionToken = newSessionToken;
            await _userManager.UpdateAsync(user);

            // Genera el accessToken
            var accessToken = GenerateJwtToken(user.Id, user.UserName!, user.SessionToken, "access");


            // Genera el refreshToken
            var refreshToken = GenerateJwtToken(user.Id, user.UserName!, user.SessionToken, "refresh");

            // Almacena el refreshToken en una cookie segura
            Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(7)
            });

            return Ok(new { accessToken });
        }

        private string GenerateJwtToken(string userId, string username, string sessionToken, string tokenType)
        {
            // Obtén la clave secreta desde la configuración de forma segura
            var secretKey = _configuration["JwtSettings:SecretKey"];
            if (string.IsNullOrEmpty(secretKey))
            {
                throw new InvalidOperationException("Secret key for JWT is not configured.");
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(JwtRegisteredClaimNames.UniqueName, username),
                new Claim("sessionToken", sessionToken),
                new Claim("type", tokenType)
            };

            var token = new JwtSecurityToken(
                claims: claims,
                expires: tokenType == "access" ? DateTime.UtcNow.AddHours(1) : DateTime.UtcNow.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
