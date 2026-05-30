using System.Net.Http.Headers;
using System.Net.Http.Json;
using HealthcareClinic.ReportingApp.Models;

namespace HealthcareClinic.ReportingApp.Services
{
    // This service is the ONLY way the reporting app accesses data — always through the API
    public class ReportingApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ReportingApiService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
        }

        // Attaches the stored JWT token to every request
        private void AttachToken()
        {
            var token = _httpContextAccessor.HttpContext?.Session.GetString("JwtToken");
            if (!string.IsNullOrEmpty(token))
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        // POST: api/auth/login — get JWT token
        public async Task<LoginResultModel?> LoginAsync(string email, string password)
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", new { email, password });
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<LoginResultModel>();
        }

        // GET: api/reports/dashboard-stats
        public async Task<DashboardStatsModel?> GetDashboardStatsAsync()
        {
            AttachToken();
            var response = await _httpClient.GetAsync("api/reports/dashboard-stats");
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<DashboardStatsModel>();
        }

        // GET: api/reports/operational
        public async Task<OperationalReportModel?> GetOperationalReportAsync()
        {
            AttachToken();
            var response = await _httpClient.GetAsync("api/reports/operational");
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<OperationalReportModel>();
        }
    }
}