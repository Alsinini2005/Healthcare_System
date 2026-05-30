using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace HealthcareClinic.API.Hubs
{
    public class AppointmentHub : Hub
    {
        // Broadcasts status transitions (e.g.1, Ali Mansoor changed status to "In Progress")
        public async Task UpdateAppointmentStatus(int appointmentId, string patientName, string newStatus)
        {
            await Clients.All.SendAsync("ReceiveStatusUpdate", appointmentId, patientName, newStatus);
        }
    }

}
