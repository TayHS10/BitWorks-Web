using GPP_Web.DTOs.User;

namespace GPP_Web.ViewModels
{
    public class AdminDashboardViewModel
    {
        // Propiedades para las tarjetas de resumen
        /// <summary>
        /// Obtiene o establece la cantidad total de usuarios registrados en el sistema.
        /// </summary>
        public int TotalUsers { get; set; }

        /// <summary>
        /// Obtiene o establece la cantidad de solicitudes de cambio de rol activas.
        /// </summary>
        public int TotalActiveRoleChangeRequests { get; set; }

        /// <summary>
        /// Obtiene o establece la cantidad total de proyectos activos.
        /// </summary>
        public int TotalActiveProjects { get; set; }

        // Propiedades para la tabla de usuarios paginada
        /// <summary>
        /// Obtiene o establece la lista de usuarios para la página actual de la tabla.
        /// </summary>
        public List<UserResponseDTO> PaginatedUsers { get; set; } = new List<UserResponseDTO>();

        /// <summary>
        /// Obtiene o establece el número de la página actual.
        /// </summary>
        public int CurrentPage { get; set; }

        /// <summary>
        /// Obtiene o establece el número total de páginas disponibles para los usuarios.
        /// </summary>
        public int TotalPages { get; set; }

        /// <summary>
        /// Obtiene o establece el tamaño de la página (cantidad de elementos por página).
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Indica si hay una página anterior disponible.
        /// </summary>
        public bool HasPreviousPage { get; set; }

        /// <summary>
        /// Indica si hay una página siguiente disponible.
        /// </summary>
        public bool HasNextPage { get; set; }

        // Propiedades para mensajes de la vista
        /// <summary>
        /// Obtiene o establece un mensaje de error para mostrar en la vista.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Obtiene o establece un mensaje de información para mostrar en la vista.
        /// </summary>
        public string InfoMessage { get; set; } = string.Empty;
    }
}
