using GPP_Web.DTOs.User;

namespace GPP_Web.ViewModels
{
    public class UserManagementViewModel
    {
        public List<UserResponseDTO> Users { get; set; } = new List<UserResponseDTO>();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
        public bool HasPreviousPage { get; set; }
        public bool HasNextPage { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string InfoMessage { get; set; } = string.Empty;
    }
}
