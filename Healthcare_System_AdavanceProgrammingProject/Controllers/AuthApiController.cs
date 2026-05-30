using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using HealthcareClinic.API.Data;
using HealthcareClinic.API.Models.Entities;
using HealthcareClinic.API.Models.DTOs;
using HealthcareClinic.API.Services;

namespace HealthcareClinic.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthApiController : ControllerBase
    {
        private readonly ClinicDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthApiController(ClinicDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // POST: api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Email check
            bool emailExists = await _context.Users
                .AnyAsync(u => u.Email.ToLower() == dto.Email.ToLower());

            if (emailExists)
                return BadRequest(new { message = "Email already registered." });

            // ==============================
            // CPR VALIDATION (ONLY FOR PATIENTS)
            // ==============================
            
                if (dto.Role == "Patient")
                {
                    if (string.IsNullOrWhiteSpace(dto.CPRNumber))
                    {
                        return BadRequest(new { message = "CPR is required for patients." });
                    }

                    if (!dto.CPRNumber.All(char.IsDigit))
                    {
                        return BadRequest(new { message = "CPR must contain numbers only." });
                    }

                    if (dto.CPRNumber.Length != 9)
                    {
                        return BadRequest(new { message = "CPR must be exactly 9 digits." });
                    }
                }

            // Create user
            var user = new User
            {
                Name = dto.Name,
                Email = dto.Email,
                Role = dto.Role,
                PasswordHash = PasswordHasher.Hash(dto.Password)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            string? refNumber = null;

            // Auto-create Patient profile
            if (dto.Role == "Patient")
            {
                var refToken = Guid.NewGuid().ToString().Substring(0, 5).ToUpper();
                refNumber = $"REF-2026-{refToken}";

                _context.Patients.Add(new Patient
                {
                    UserId = user.Id,
                    Name = user.Name,
                    CPRNumber = dto.CPRNumber!,
                    PatientReferenceNumber = refNumber
                });

                await _context.SaveChangesAsync();
            }

            // Auto-create Doctor profile
            if (dto.Role == "Doctor")
            {
                _context.Doctors.Add(new Doctor
                {
                    UserId = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    WorkingHours = "08:00 - 16:00",
                    DaysOff = "Friday, Saturday"
                });

                await _context.SaveChangesAsync();
            }

            return Ok(new
            {
                message = "Registered successfully.",
                patientReferenceNumber = refNumber
            });
        }

        // POST: api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());

            if (user == null || !PasswordHasher.Verify(dto.Password, user.PasswordHash))
                return Unauthorized(new { message = "Invalid email or password." });

            string? refNumber = null;

            if (user.Role == "Patient")
            {
                var patient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.UserId == user.Id);

                refNumber = patient?.PatientReferenceNumber;
            }

            var token = GenerateJwtToken(user);

            return Ok(new
            {
                token,
                role = user.Role,
                username = user.Name,
                patientReferenceNumber = refNumber
            });
        }

        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var key = Encoding.UTF8.GetBytes(jwtSettings["Secret"]!);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var tokenDescriptor = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(3),
                signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
        }
    }
}