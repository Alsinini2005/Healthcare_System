using System.ComponentModel.DataAnnotations;

namespace Healthcare_System_AdavanceProgrammingProject.Models.ViewModels
{
    public class ProfileViewModel
    {
        public string Name { get; set; }

        public string Email { get; set; }

        public string Role { get; set; }

        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; }

        [DataType(DataType.Password)]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Compare("NewPassword")]
        public string ConfirmPassword { get; set; }
    }
}