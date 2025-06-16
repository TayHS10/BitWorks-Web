namespace GPP_Web.DTOs.User
{
    /// <summary>
    /// Represents the data transfer object used for creating a new user.
    /// This DTO contains the essential information provided by the client
    /// to register a new user in the system.
    /// </summary>
    public class CreateUserDTO
    {
        /// <summary>
        /// Gets or sets the full name of the user. This field is required.
        /// </summary>
        public string? FullName { get; set; } // Hacemos nullable para manejo de Required en MVC

        /// <summary>
        /// Gets or sets the email address of the user. This field is required and must be unique.
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// Gets or sets the password for the new user. This field is required.
        /// The password will be hashed by the server before storage.
        /// </summary>
        public string? Password { get; set; }

        /// <summary>
        /// Gets or sets the role to be assigned to the new user. This field is required.
        /// Server-side validation should ensure this is a valid and authorized role.
        /// </summary>
        public string? Role { get; set; }
    }
}