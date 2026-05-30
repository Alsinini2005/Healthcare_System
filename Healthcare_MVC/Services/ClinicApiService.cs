
using HealthcareClinic.API.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Healthcare_System_AdavanceProgrammingProject.MVC.Services
{
    public class ClinicApiService
    {
        private readonly HttpClient _httpClient;

        public ClinicApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

       
        /// Contacts the Web API backend anonymously via HttpClient to look up 
        /// active and upcoming appointments using a patient's CPR and reference number.
        
        public async Task<List<PublicAppointmentDto>> LookupAppointmentsAsync(string cpr, string referenceNumber)
        {
            try
            {
                // Appends the query strings to match the Web API PublicLookup routing path exactly
                var response = await _httpClient.GetAsync($"api/public-lookup/find?cpr={cpr}&refNum={referenceNumber}");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<PublicAppointmentDto>>()
                           ?? new List<PublicAppointmentDto>();
                }

                return null!;
            }
            catch (Exception)
            {
                // Returns an empty list or handles the network connection failure safely
                return null!;
            }
        }
    }
}