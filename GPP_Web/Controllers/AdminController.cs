using GPP_Web.DTOs.Project; // Necesario para ProjectResponseDTO
using GPP_Web.DTOs.RoleChangeRequest; // Necesario para RoleChangeRequestDTO
using GPP_Web.DTOs.User;
using GPP_Web.Models;
using GPP_Web.Services;
using GPP_Web.ViewModels; // Necesario para AdminDashboardViewModel
using Microsoft.AspNetCore.Mvc;
using System.Runtime.InteropServices;

namespace GPP_Web.Controllers
{
    public class AdminController : Controller
    {
        private readonly GenericApiClient _apiClient;

        public AdminController(GenericApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        /// <summary>
        /// Muestra el dashboard del Administrador con métricas clave del sistema
        /// y una tabla paginada de usuarios.
        /// </summary>
        /// <param name="pageNumber">Número de la página actual para la tabla de usuarios. Por defecto es 1.</param>
        /// <param name="pageSize">Cantidad de usuarios por página en la tabla. Por defecto es 10.</param>
        /// <returns>Una vista que muestra las métricas del dashboard y la tabla de usuarios.</returns>
        [HttpGet]
        public async Task<IActionResult> Dashboard(int pageNumber = 1, int pageSize = 10)
        {
            var viewModel = new AdminDashboardViewModel();
            string concatenatedErrors = ""; // Para acumular mensajes de error de las llamadas a la API

            // Configura el mensaje de bienvenida y su icono basado en la hora local de Costa Rica
            SetWelcomeMessageAndIcon();

            // --- 1. Obtener todos los usuarios (para TotalUsers y para la paginación de la tabla) ---
            List<UserResponseDTO> allUsers = new List<UserResponseDTO>();
            try
            {
                var usersResponse = await _apiClient.GetAsync<List<UserResponseDTO>>("api/User"); // Llama a la API para obtener todos los usuarios

                if (usersResponse.Success && usersResponse.Data != null)
                {
                    allUsers = usersResponse.Data; // Guarda todos los usuarios para luego paginarlos
                    viewModel.TotalUsers = allUsers.Count; // Asigna el total de usuarios para la tarjeta
                }
                else
                {
                    concatenatedErrors += usersResponse.Message ?? "No se pudieron cargar los usuarios para el dashboard. ";
                    viewModel.TotalUsers = 0;
                }
            }
            catch (Exception ex)
            {
                concatenatedErrors += $"Error al cargar usuarios desde la API: {ex.Message}. ";
                viewModel.TotalUsers = 0;
            }

            // --- 2. Obtener solicitudes de cambio de rol activas (para tarjeta de resumen) ---
            try
            {
                var roleRequestsResponse = await _apiClient.GetAsync<List<RoleChangeRequestDTO>>("api/RoleChangeRequest/active");

                if (roleRequestsResponse.Success && roleRequestsResponse.Data != null)
                {
                    viewModel.TotalActiveRoleChangeRequests = roleRequestsResponse.Data.Count;
                }
                else
                {
                    concatenatedErrors += roleRequestsResponse.Message ?? "No se pudieron cargar las solicitudes de cambio de rol. ";
                    viewModel.TotalActiveRoleChangeRequests = 0;
                }
            }
            catch (Exception ex)
            {
                concatenatedErrors += $"Error al cargar solicitudes de rol: {ex.Message}. ";
                viewModel.TotalActiveRoleChangeRequests = 0;
            }

            // --- 3. Obtener proyectos activos (para tarjeta de resumen) ---
            try
            {
                var projectsResponse = await _apiClient.GetAsync<List<ProjectResponseDTO>>("api/Project/active");

                if (projectsResponse.Success && projectsResponse.Data != null)
                {
                    viewModel.TotalActiveProjects = projectsResponse.Data.Count;
                }
                else
                {
                    concatenatedErrors += projectsResponse.Message ?? "No se pudieron cargar los proyectos activos. ";
                    viewModel.TotalActiveProjects = 0;
                }
            }
            catch (Exception ex)
            {
                concatenatedErrors += $"Error al cargar proyectos activos: {ex.Message}. ";
                viewModel.TotalActiveProjects = 0;
            }

            // --- 4. Lógica de Paginación para la tabla de usuarios ---
            if (allUsers.Any())
            {
                int totalUserCount = allUsers.Count;
                int totalUserPages = (int)Math.Ceiling((double)totalUserCount / pageSize);

                // Asegura que pageNumber no sea menor que 1 y no exceda el total de páginas
                if (pageNumber < 1) pageNumber = 1;
                if (pageNumber > totalUserPages && totalUserPages > 0) pageNumber = totalUserPages;

                // Redirige si el pageNumber fue ajustado, para mantener la URL limpia y correcta
                if (Request.Query["pageNumber"].Count > 0 && int.TryParse(Request.Query["pageNumber"], out int requestedPage) && requestedPage != pageNumber)
                {
                    return RedirectToAction("Dashboard", new { pageNumber = pageNumber, pageSize = pageSize });
                }

                viewModel.CurrentPage = pageNumber;
                viewModel.TotalPages = totalUserPages;
                viewModel.PageSize = pageSize;
                viewModel.HasPreviousPage = (pageNumber > 1);
                viewModel.HasNextPage = (pageNumber < totalUserPages);

                // Aplica la paginación a la lista completa de usuarios
                viewModel.PaginatedUsers = allUsers
                                            .Skip((pageNumber - 1) * pageSize)
                                            .Take(pageSize)
                                            .ToList();
            }
            else
            {
                // Si no hay usuarios, establece los valores de paginación y un mensaje informativo
                viewModel.InfoMessage = "No hay usuarios registrados para mostrar en la tabla.";
                viewModel.CurrentPage = 1;
                viewModel.TotalPages = 1;
                viewModel.PageSize = pageSize;
                viewModel.HasPreviousPage = false;
                viewModel.HasNextPage = false;
                viewModel.PaginatedUsers = new List<UserResponseDTO>(); // Asegura que la lista esté vacía
            }

            // --- Asignar mensajes para SweetAlert2 ---
            viewModel.ErrorMessage = concatenatedErrors.Trim(); // Errores acumulados de las llamadas a la API

            // Los mensajes de TempData (ej. desde DeactivateUser) se muestran con ViewBag para SweetAlert2.
            ViewBag.SuccessMessage = TempData["SuccessMessage"] as string;
            ViewBag.ErrorMessage = viewModel.ErrorMessage; // Errores de la carga de datos del dashboard
            ViewBag.InfoMessage = viewModel.InfoMessage;   // Mensaje informativo (ej. no hay usuarios)

            return View(viewModel); // Pasa el ViewModel completo a la vista
        }

        // --- MÉTODO ACTUALIZADO: Mostrar y Paginar Usuarios ---
        /// <summary>
        /// Muestra un listado paginado de todos los usuarios del sistema con opciones para gestionar,
        /// excluyendo al usuario autenticado actual.
        /// </summary>
        /// <param name="pageNumber">Número de página actual. Por defecto es 1.</param>
        /// <param name="pageSize">Cantidad de usuarios por página. Por defecto es 10.</param>
        /// <returns>Una vista con la tabla de usuarios y controles de paginación.</returns>
        [HttpGet]
        public async Task<IActionResult> ManageUsers(int pageNumber = 1, int pageSize = 10)
        {
            var viewModel = new UserManagementViewModel();
            List<UserResponseDTO> allUsers = new List<UserResponseDTO>();

            try
            {
                var response = await _apiClient.GetAsync<List<UserResponseDTO>>("api/User");

                if (response.Success && response.Data != null)
                {
                    allUsers = response.Data;

                    // --- FILTRAR AL USUARIO AUTENTICADO ---
                    string authenticatedUserEmail = User.Identity.Name; // Obtener el email del usuario autenticado
                    if (!string.IsNullOrEmpty(authenticatedUserEmail))
                    {
                        // Excluir al usuario cuya dirección de correo electrónico coincide con la del usuario autenticado
                        allUsers = allUsers.Where(u => !u.Email.Equals(authenticatedUserEmail, StringComparison.OrdinalIgnoreCase)).ToList();
                    }
                    // --- FIN FILTRADO ---

                    if (!allUsers.Any())
                    {
                        viewModel.InfoMessage = "No se encontraron usuarios registrados en el sistema, o solo se encontró el usuario autenticado.";
                        viewModel.CurrentPage = 1;
                        viewModel.TotalPages = 1;
                        viewModel.HasPreviousPage = false;
                        viewModel.HasNextPage = false;
                        viewModel.Users = new List<UserResponseDTO>();
                    }
                    else
                    {
                        int totalUserCount = allUsers.Count;
                        int totalUserPages = (int)Math.Ceiling((double)totalUserCount / pageSize);

                        if (pageNumber < 1) pageNumber = 1;
                        if (pageNumber > totalUserPages && totalUserPages > 0) pageNumber = totalUserPages;

                        // Redirige si el pageNumber fue ajustado para mantener la URL limpia y correcta
                        if (Request.Query["pageNumber"].Count > 0 && int.TryParse(Request.Query["pageNumber"], out int requestedPage) && requestedPage != pageNumber)
                        {
                            return RedirectToAction("ManageUsers", new { pageNumber = pageNumber, pageSize = pageSize });
                        }

                        viewModel.CurrentPage = pageNumber;
                        viewModel.TotalPages = totalUserPages;
                        viewModel.PageSize = pageSize;
                        viewModel.HasPreviousPage = (pageNumber > 1);
                        viewModel.HasNextPage = (pageNumber < totalUserPages);

                        viewModel.Users = allUsers
                                            .Skip((pageNumber - 1) * pageSize)
                                            .Take(pageSize)
                                            .ToList();
                    }
                }
                else
                {
                    viewModel.ErrorMessage = response.Message ?? "No se pudieron cargar los usuarios del sistema.";
                    viewModel.CurrentPage = 1;
                    viewModel.TotalPages = 1;
                    viewModel.HasPreviousPage = false;
                    viewModel.HasNextPage = false;
                    viewModel.Users = new List<UserResponseDTO>();
                }
            }
            catch (HttpRequestException httpEx)
            {
                viewModel.ErrorMessage = $"Error HTTP al intentar cargar los usuarios: {httpEx.StatusCode} - {httpEx.Message}";
                viewModel.CurrentPage = 1;
                viewModel.TotalPages = 1;
                viewModel.HasPreviousPage = false;
                viewModel.HasNextPage = false;
                viewModel.Users = new List<UserResponseDTO>();
            }
            catch (Exception ex)
            {
                viewModel.ErrorMessage = $"Ocurrió un error inesperado al intentar cargar los usuarios: {ex.Message}";
                viewModel.CurrentPage = 1;
                viewModel.TotalPages = 1;
                viewModel.HasPreviousPage = false;
                viewModel.HasNextPage = false;
                viewModel.Users = new List<UserResponseDTO>();
            }

            // Estos ViewBag se usan para SweetAlert2 en la vista
            ViewBag.SuccessMessage = TempData["SuccessMessage"] as string;
            ViewBag.ErrorMessage = viewModel.ErrorMessage;
            ViewBag.InfoMessage = viewModel.InfoMessage;

            return View(viewModel);
        }


        // --- Método existente: Desactivar Usuario (MODIFICADO para redirigir a ManageUsers) ---
        /// <summary>
        /// Desactiva un usuario específico haciendo una llamada a la API.
        /// </summary>
        /// <param name="userId">El ID del usuario a desactivar.</param>
        /// <returns>Redirecciona de nuevo a la vista de gestión de usuarios después de la operación.</returns>
        [HttpPost]
        public async Task<IActionResult> DeactivateUser(int userId)
        {
            try
            {
                var response = await _apiClient.PutAsync<object>($"api/User/deactivate/{userId}", null);

                if (response.Success)
                {
                    TempData["SuccessMessage"] = "El usuario ha sido desactivado correctamente.";
                }
                else
                {
                    TempData["ErrorMessage"] = response.Message ?? "No se pudo desactivar el usuario.";
                }

                // MODIFICACIÓN CLAVE: Redirige a la vista ManageUsers
                return RedirectToAction("ManageUsers");
            }
            catch (HttpRequestException httpEx)
            {
                TempData["ErrorMessage"] = $"Error HTTP al desactivar el usuario: {httpEx.StatusCode} - {httpEx.Message}";
                return RedirectToAction("ManageUsers");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Ocurrió un error inesperado al desactivar el usuario: {ex.Message}";
                return RedirectToAction("ManageUsers");
            }
        }

        /// <summary>
        /// Método auxiliar para establecer el mensaje de bienvenida y el icono según la hora del día en Costa Rica.
        /// Estos valores se almacenan en ViewBag para ser usados en la vista.
        /// </summary>
        private void SetWelcomeMessageAndIcon()
        {
            DateTime now;

            // Determinar la zona horaria para Costa Rica
            TimeZoneInfo costaRicaZone;
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    costaRicaZone = TimeZoneInfo.FindSystemTimeZoneById("Central America Standard Time"); // Windows
                }
                else // Linux/macOS
                {
                    costaRicaZone = TimeZoneInfo.FindSystemTimeZoneById("America/Costa_Rica"); // IANA
                }
            }
            catch (TimeZoneNotFoundException)
            {
                // Fallback si la zona horaria específica no se encuentra, usar UTC como último recurso
                costaRicaZone = TimeZoneInfo.Utc;
            }

            // Convertir la hora UTC actual a la hora de Costa Rica
            now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, costaRicaZone);
            int hour = now.Hour;

            if (hour >= 5 && hour < 12)
            {
                ViewBag.WelcomeMessage = "¡Buenos Días!";
                ViewBag.WelcomeIconClass = "bi bi-sun-fill"; // Icono de sol para la mañana
            }
            else if (hour >= 12 && hour < 18)
            {
                ViewBag.WelcomeMessage = "¡Buenas Tardes!";
                ViewBag.WelcomeIconClass = "bi bi-cloud-sun-fill"; // Icono de nube y sol para la tarde
            }
            else
            {
                ViewBag.WelcomeMessage = "¡Buenas Noches!";
                ViewBag.WelcomeIconClass = "bi bi-moon-fill"; // Icono de luna para la noche
            }
        }

        // --- NUEVO Método: Obtener Solicitudes de Rol Pendientes ---
        /// <summary>
        /// Muestra el listado de solicitudes de cambio de rol pendientes.
        /// </summary>
        /// <returns>Una vista con la tabla de solicitudes de rol.</returns>
        [HttpGet]
        public async Task<IActionResult> RoleRequests()
        {
            List<RoleChangeRequestDTO> activeRequests = new List<RoleChangeRequestDTO>();
            string errorMessage = string.Empty;
            string infoMessage = string.Empty;

            try
            {
                var response = await _apiClient.GetAsync<List<RoleChangeRequestDTO>>("api/RoleChangeRequest/active");

                if (response.Success && response.Data != null)
                {
                    activeRequests = response.Data;
                    if (!activeRequests.Any())
                    {
                        infoMessage = "No hay solicitudes de cambio de rol pendientes en este momento.";
                    }
                }
                else
                {
                    errorMessage = response.Message ?? "No se pudieron cargar las solicitudes de cambio de rol pendientes.";
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Ocurrió un error al intentar cargar las solicitudes: {ex.Message}";
            }

            // Pasa la lista de solicitudes y mensajes a la vista
            ViewBag.ActiveRoleRequests = activeRequests;
            ViewBag.ErrorMessage = errorMessage;
            ViewBag.InfoMessage = infoMessage;
            ViewBag.SuccessMessage = TempData["SuccessMessage"] as string; // Para mensajes después de aceptar/rechazar

            return View();
        }

        // --- NUEVO Método: Aprobar Solicitud de Rol ---
        /// <summary>
        /// Aprueba una solicitud de cambio de rol.
        /// </summary>
        /// <param name="id">El ID de la solicitud a aprobar.</param>
        /// <returns>Redirecciona de nuevo al listado de solicitudes de rol.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken] // Siempre buena práctica para POST/PUT
        public async Task<IActionResult> ApproveRoleRequest(int id)
        {
            try
            {
                var response = await _apiClient.PutAsync<ApiResponse<object>>($"api/RoleChangeRequest/{id}/approve", null);

                if (response.Success)
                {
                    TempData["SuccessMessage"] = $"Solicitud {id} aprobada correctamente.";
                }
                else
                {
                    TempData["ErrorMessage"] = response.Message ?? $"No se pudo aprobar la solicitud {id}.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Ocurrió un error inesperado al intentar aprobar la solicitud {id}: {ex.Message}";
            }
            return RedirectToAction("RoleRequests"); // Redirige al listado
        }

        // --- NUEVO Método: Rechazar Solicitud de Rol ---
        /// <summary>
        /// Rechaza una solicitud de cambio de rol.
        /// </summary>
        /// <param name="id">El ID de la solicitud a rechazar.</param>
        /// <returns>Redirecciona de nuevo al listado de solicitudes de rol.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken] // Siempre buena práctica para POST/PUT
        public async Task<IActionResult> RejectRoleRequest(int id)
        {
            try
            {
                var response = await _apiClient.PutAsync<ApiResponse<object>>($"api/RoleChangeRequest/{id}/cancel", null);

                if (response.Success)
                {
                    TempData["SuccessMessage"] = $"Solicitud {id} rechazada correctamente.";
                }
                else
                {
                    TempData["ErrorMessage"] = response.Message ?? $"No se pudo rechazar la solicitud {id}.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Ocurrió un error inesperado al intentar rechazar la solicitud {id}: {ex.Message}";
            }
            return RedirectToAction("RoleRequests"); // Redirige al listado
        }


        // --- Método: Mostrar formulario para agregar usuario (GET) ---
        [HttpGet]
        public IActionResult CreateUser()
        {
            ViewBag.Roles = new List<string> { "Manager", "Accountant", "Admin" };
            return View();
        }

        // --- Método: Procesar formulario para agregar usuario (POST) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(CreateUserDTO createUserDto)
        {
            var allowedRoles = new List<string> { "Manager", "Accountant", "Admin" };
            ViewBag.Roles = allowedRoles; // Re-poblar para el dropdown si se regresa la vista

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Por favor, corrige los errores en el formulario.";
                return View(createUserDto);
            }

            if (string.IsNullOrWhiteSpace(createUserDto.Role) || !allowedRoles.Contains(createUserDto.Role))
            {
                ModelState.AddModelError("Role", "El rol seleccionado no es válido.");
                TempData["ErrorMessage"] = "El rol seleccionado no es válido. Solo se permiten Manager, Accountant o Admin.";
                return View(createUserDto);
            }

            try
            {
                // CORRECCIÓN: PostAsync espera DOS argumentos de tipo genérico (TRequest, TResponseData)
                var response = await _apiClient.PostAsync<CreateUserDTO, object>("api/User", createUserDto);

                if (response.Success)
                {
                    TempData["SuccessMessage"] = "Usuario creado exitosamente.";
                    return RedirectToAction("Dashboard");
                }
                else
                {
                    // Esto se ejecuta si la API devuelve un 2xx pero con Success=false.
                    TempData["ErrorMessage"] = response.Message ?? "No se pudo crear el usuario. Por favor, intenta de nuevo.";
                    // Intenta pasar el error específico de la API al ModelState para que se muestre en el campo correcto
                    if (!string.IsNullOrWhiteSpace(response.Message) && response.Message.Contains("email", StringComparison.OrdinalIgnoreCase))
                    {
                        ModelState.AddModelError("Email", response.Message);
                    }
                    return View(createUserDto); // Regresa la vista con los datos del formulario y el error
                }
            }
            catch (HttpRequestException httpEx) // Captura si EnsureSuccessStatusCode lanza una excepción (ej. 409, 500)
            {
                // Personaliza el mensaje de error para el caso de conflicto (ej. email duplicado)
                string errorMessage = $"Error de conexión con el servicio: {httpEx.StatusCode} - {httpEx.Message}.";
                if (httpEx.StatusCode == System.Net.HttpStatusCode.Conflict) // 409 Conflict
                {
                    errorMessage = "El correo electrónico proporcionado ya está registrado. Por favor, use uno diferente.";
                    ModelState.AddModelError("Email", errorMessage); // Asocia al campo de email
                }
                else
                {
                    ModelState.AddModelError("", errorMessage); // Error general
                }
                TempData["ErrorMessage"] = errorMessage;
                return View(createUserDto); // Vuelve a mostrar el formulario con los errores
            }
            catch (Exception ex) // Otros errores inesperados
            {
                TempData["ErrorMessage"] = $"Ocurrió un error inesperado al crear el usuario: {ex.Message}";
                ModelState.AddModelError("", $"Error inesperado: {ex.Message}");
                return View(createUserDto); // Vuelve a mostrar el formulario con los errores
            }
        }
    }
}