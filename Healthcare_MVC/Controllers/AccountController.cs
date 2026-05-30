using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Healthcare_System_AdavanceProgrammingProject.Models.ViewModels;
using System.IdentityModel.Tokens.Jwt;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using HealthcareClinic.API.Data;
using HealthcareClinic.API.Models.Entities;
using HealthcareClinic.API.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;



namespace Healthcare_System_AdavanceProgrammingProject.MVC.Controllers
{
    public class AccountController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ClinicDbContext _context;

        public AccountController(IHttpClientFactory httpClientFactory,
            IConfiguration configuration, ClinicDbContext context)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _context = context;
        }

        // ── LOGIN ──────────────────────────────────────────────────────────
        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Dashboard");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                var client = _httpClientFactory.CreateClient();
                string apiBaseUrl = _configuration["ClinicApiSettings:BaseUrl"] ?? "https://localhost:7085/";
                client.BaseAddress = new Uri(apiBaseUrl);

                var response = await client.PostAsJsonAsync("api/auth/login",
                    new { email = model.Email, password = model.Password });

                if (!response.IsSuccessStatusCode)
                {
                    string errorDetail = "Invalid login credentials. Please try again.";
                    try
                    {
                        var errorObj = await response.Content
                            .ReadFromJsonAsync<Dictionary<string, string>>();
                        if (errorObj != null && errorObj.ContainsKey("message"))
                            errorDetail = errorObj["message"];
                    }
                    catch { }

                    ModelState.AddModelError(string.Empty, errorDetail);
                    return View(model);
                }

                var loginResult = await response.Content
                    .ReadFromJsonAsync<Dictionary<string, string>>();

                if (loginResult == null || !loginResult.ContainsKey("token"))
                {
                    ModelState.AddModelError(string.Empty, "Error parsing authentication response.");
                    return View(model);
                }

                string tokenStr = loginResult["token"];
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadToken(tokenStr) as JwtSecurityToken;

                if (jsonToken == null)
                {
                    ModelState.AddModelError(string.Empty, "Malformed security token.");
                    return View(model);
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, model.Email),
                    new Claim("JwtToken", tokenStr)
                };

                foreach (var claim in jsonToken.Claims)
                {
                    claims.Add(claim);

                    if (claim.Type == "role" ||
                        claim.Type == "roles" ||
                        claim.Type == ClaimTypes.Role ||
                        claim.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                    {
                        claims.Add(new Claim(ClaimTypes.Role, claim.Value));
                    }
                }

                if (loginResult.TryGetValue("patientReferenceNumber", out var refNum)
                    && !string.IsNullOrEmpty(refNum))
                {
                    claims.Add(new Claim("PatientReferenceNumber", refNum));
                }

                var claimsIdentity = new ClaimsIdentity(
                    claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(3)
                };

                foreach (var c in claims)
                {
                    Console.WriteLine($"{c.Type} : {c.Value}");
                }

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity), authProperties);

                return RedirectToAction("Index", "Dashboard");
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "Unable to connect to the authentication service.");
                return View(model);
            }
        }

        // ── REGISTER (Patients only) ───────────────────────────────────────
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Dashboard");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // CPR validation — server-side guard
            if (!Regex.IsMatch(model.CPRNumber ?? "", @"^\d{9}$"))
            {
                ModelState.AddModelError(nameof(model.CPRNumber), "CPR must be exactly 9 digits (numbers only).");
                return View(model);
            }

            bool emailExists = await _context.Users
                .AnyAsync(u => u.Email.ToLower() == model.Email.ToLower());
            if (emailExists)
            {
                ModelState.AddModelError(string.Empty, "An account with this email already exists.");
                return View(model);
            }

            // 1. Create User
            var user = new User
            {
                Name = model.Name,
                Email = model.Email,
                Role = "Patient",
                PasswordHash = PasswordHasher.Hash(model.Password)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // 2. Create Patient profile and generate reference number
            var refToken = Guid.NewGuid().ToString().Substring(0, 5).ToUpper();
            var refNumber = $"REF-2026-{refToken}";

            var patient = new Patient
            {
                UserId = user.Id,
                Name = model.Name,
                CPRNumber = model.CPRNumber,
                PatientReferenceNumber = refNumber
            };

            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            // Show success message on the Register page itself
            ViewBag.SuccessMessage = $"Account created successfully! Your reference number is: {refNumber} — please save it, you will need it to track your appointments.";

            return View(new RegisterViewModel()); // clear the form
        }

        // ── LOGOUT ─────────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }
    }
}
