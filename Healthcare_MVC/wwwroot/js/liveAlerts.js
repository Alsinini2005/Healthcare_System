// Initialize SignalR connection
const signalrConnection = new signalR.HubConnectionBuilder()
    .withUrl("https://localhost:7123/appointmentHub")
    .withAutomaticReconnect()
    .build();

// Receive real-time updates
signalrConnection.on("ReceiveStatusUpdate", function (appointmentId, patientName, newStatus) {

    const alertBanner = document.getElementById("liveAlertBanner");
    const alertMsg = document.getElementById("liveAlertMessage");

    // Show alert banner
    if (alertBanner && alertMsg) {

        alertMsg.innerText =
            `Patient ${patientName} (Appointment #${appointmentId}) updated to ${newStatus}`;

        alertBanner.classList.remove("d-none");
    }

    // Update appointment badge
    const targetBadge = document.getElementById("badge-" + appointmentId);

    if (targetBadge) {

        targetBadge.innerText = newStatus;

        targetBadge.className = "badge";

        switch (newStatus) {

            case "Confirmed":
                targetBadge.classList.add("bg-primary");
                break;

            case "Requested":
                targetBadge.classList.add("bg-warning", "text-dark");
                break;

            case "CheckedIn":
                targetBadge.classList.add("bg-info", "text-white");
                break;

            case "InProgress":
                targetBadge.classList.add("bg-secondary", "text-white");
                break;

            case "Completed":
                targetBadge.classList.add("bg-success");
                break;

            case "Cancelled":
                targetBadge.classList.add("bg-danger");
                break;

            default:
                targetBadge.classList.add("bg-dark");
                break;
        }
    }
});

// Start SignalR connection
signalrConnection.start()
    .catch(err => console.error("SignalR connection failed:", err));


// Update appointment status
function triggerStatusUpdate(id, statusValue) {

    // Get JWT token from hidden input OR localStorage
    const token =
        document.getElementById("jwtToken")?.value ||
        localStorage.getItem("jwtToken");

    if (!token) {
        alert("JWT token not found.");
        return;
    }

    fetch(`https://localhost:7123/api/appointments/update-status/${id}?status=${statusValue}`, {

        method: 'PUT',

        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${token}`
        }

    })
        .then(async response => {

            if (!response.ok) {

                const errorText = await response.text();

                console.error("Status update failed:", errorText);

                alert("Failed to update appointment status.");

                return;
            }

            console.log("Appointment status updated successfully.");

        })
        .catch(error => {
            console.error('Error updating appointment:', error);
        });
}
