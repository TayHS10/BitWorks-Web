using GPP_Web.DTOs.Project;
using GPP_Web.DTOs.RoleChangeRequest;
using GPP_Web.DTOs.User;
using GPP_Web.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Threading.Tasks;

namespace GPP_Web.Controllers
{
    public class AccountantController : Controller
    {
        private readonly GenericApiClient _apiClient;

        public AccountantController(GenericApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        // Acción para mostrar el dashboard del contador
        public async Task<IActionResult> Dashboard()
        {
            // Inicializar elementos de ViewBag para evitar excepciones de referencia nula en la vista
            SetWelcomeMessageAndIcon(); // Configura el mensaje de bienvenida y su icono
            ViewBag.TotalExpensesAllProjects = 0m; // Se inicializa para el card de gastos totales
            ViewBag.TotalProjectsCount = 0; // Se inicializa para el card de conteo de proyectos
            ViewBag.TotalManagersCount = 0; // Se inicializa para el card de conteo de managers
            ViewBag.ErrorMessage = null; // Reiniciar cualquier mensaje de error previo

            // 1. Obtener todos los proyectos activos para la tabla y para calcular los gastos totales
            List<ProjectResponseDTO> activeProjects = new List<ProjectResponseDTO>();
            try
            {
                var projectsResponse = await _apiClient.GetAsync<List<ProjectResponseDTO>>("api/Project/active");

                if (projectsResponse.Success && projectsResponse.Data != null)
                {
                    activeProjects = projectsResponse.Data;
                    // Actualizar el conteo total de proyectos basado en los proyectos obtenidos
                    ViewBag.TotalProjectsCount = activeProjects.Count;

                    // *** CÁLCULO DE GASTOS TOTALES DE TODOS LOS PROYECTOS ***
                    // Sumamos la diferencia entre el presupuesto inicial y el presupuesto restante de cada proyecto.
                    ViewBag.TotalExpensesAllProjects = activeProjects.Sum(p => p.Budget - p.RemainingBudget);
                }
                else
                {
                    ViewBag.ErrorMessage = projectsResponse.Message ?? "No se pudieron cargar los proyectos activos para la tabla y calcular los gastos.";
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Ocurrió un error inesperado al cargar los proyectos y/o calcular los gastos: {ex.Message}";
            }

            // 2. Obtener datos para la tarjeta de "Cantidad Total de Managers"
            // (Esta es una llamada API independiente para obtener todos los usuarios y luego filtrar)
            try
            {
                var allUsersResponse = await _apiClient.GetAsync<List<UserResponseDTO>>("api/User");

                if (allUsersResponse.Success && allUsersResponse.Data != null)
                {
                    // Filtrar los usuarios para obtener solo aquellos con el rol "Manager"
                    ViewBag.TotalManagersCount = allUsersResponse.Data.Where(u => u.Role == "Manager").Count();
                }
                else
                {
                    // Manejo de error no crítico para la tarjeta de managers
                    // Puedes mostrar un mensaje en la consola del servidor o loguearlo
                    Console.WriteLine($"Error al obtener la lista de usuarios para contar gerentes: {allUsersResponse.Message}");
                }
            }
            catch (Exception ex)
            {
                // Manejo de excepción para la tarjeta de managers (ej. problemas de conexión con la API de usuarios)
                Console.WriteLine($"Excepción al obtener la lista de usuarios para contar gerentes: {ex.Message}");
            }

            return View(activeProjects); // Pasamos los proyectos activos para la tabla
        }

        // Método auxiliar para establecer el mensaje de bienvenida y el icono según la hora del día en Costa Rica
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

        /// <summary>
        /// Obtiene y muestra una lista de usuarios con el rol "Manager" con paginación.
        /// Consumo: GET /api/User
        /// </summary>
        /// <param name="pageNumber">Número de página actual (por defecto 1).</param>
        /// <param name="pageSize">Número de elementos por página (por defecto 10).</param>
        /// <returns>Una vista que muestra los usuarios gerentes paginados.</returns>
        [HttpGet]
        public async Task<IActionResult> ManagersList(int pageNumber = 1, int pageSize = 10)
        {
            List<UserResponseDTO> managersForPage = new List<UserResponseDTO>();
            ViewBag.ErrorMessage = null;
            ViewBag.InfoMessage = null;

            try
            {
                // Realizar la solicitud GET a la API de usuarios utilizando GenericApiClient
                var apiResponse = await _apiClient.GetAsync<List<UserResponseDTO>>("api/User");

                // Verificar si la solicitud fue exitosa y se obtuvieron datos
                if (apiResponse.Success && apiResponse.Data != null)
                {
                    // Filtrar los usuarios para obtener solo aquellos con el rol "Manager"
                    var allManagers = apiResponse.Data.Where(u => u.Role == "Manager").ToList();

                    if (!allManagers.Any()) // Si la lista de managers está vacía
                    {
                        ViewBag.InfoMessage = "No se encontraron usuarios con el rol 'Manager' registrados en el sistema.";
                        ViewBag.TotalPages = 1;
                        ViewBag.CurrentPage = 1;
                        ViewBag.HasPreviousPage = false;
                        ViewBag.HasNextPage = false;
                        return View(new List<UserResponseDTO>()); // Devolver una lista vacía
                    }

                    // --- Lógica de Paginación en el Controlador ---
                    var totalCount = allManagers.Count;
                    int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                    // Guardar el pageNumber original para comparar
                    int requestedPageNumber = pageNumber;

                    // VALIDACIÓN Y AJUSTE DEL NÚMERO DE PÁGINA (CLAMPEO)
                    // Asegurarse de que pageNumber no sea menor que 1
                    if (pageNumber < 1) pageNumber = 1;
                    // Asegurarse de que pageNumber no sea mayor que TotalPages (y que haya al menos 1 página)
                    if (pageNumber > totalPages && totalPages > 0) pageNumber = totalPages;

                    // REDIRECCIÓN SI EL NÚMERO DE PÁGINA FUE AJUSTADO
                    if (requestedPageNumber != pageNumber)
                    {
                        // Redirige al número de página válido. Esto refresca la URL en el navegador.
                        return RedirectToAction("ManagersList", new { pageNumber = pageNumber, pageSize = pageSize });
                    }

                    ViewBag.TotalPages = totalPages;
                    ViewBag.CurrentPage = pageNumber; // Ahora ViewBag.CurrentPage siempre es válido y ajustado
                    ViewBag.PageSize = pageSize; // Asegurarse de pasar el pageSize a la vista

                    ViewBag.HasPreviousPage = (pageNumber > 1);
                    ViewBag.HasNextPage = (pageNumber < totalPages); // Usa 'totalPages' (ya clamped)

                    // Obtener los managers para la página actual aplicando Skip y Take
                    managersForPage = allManagers
                                    .Skip((pageNumber - 1) * pageSize)
                                    .Take(pageSize)
                                    .ToList();

                    return View(managersForPage); // Pasar solo los managers de la página actual
                }
                else
                {
                    // Manejar errores de la API o si la respuesta no es exitosa
                    ViewBag.ErrorMessage = apiResponse.Message ?? "Error desconocido al obtener usuarios de la API.";
                    ViewBag.TotalPages = 1; // Establecer valores predeterminados para la paginación en caso de error
                    ViewBag.CurrentPage = 1;
                    ViewBag.HasPreviousPage = false;
                    ViewBag.HasNextPage = false;
                    return View(new List<UserResponseDTO>()); // Devolver lista vacía en caso de error
                }
            }
            catch (Exception ex)
            {
                // Captura cualquier excepción inesperada (conexión, deserialización, etc.)
                ViewBag.ErrorMessage = $"Ocurrió un error inesperado al cargar la lista de gerentes: {ex.Message}";
                ViewBag.TotalPages = 1; // Establecer valores predeterminados para la paginación en caso de error
                ViewBag.CurrentPage = 1;
                ViewBag.HasPreviousPage = false;
                ViewBag.HasNextPage = false;
                return View(new List<UserResponseDTO>()); // Retornar lista vacía en caso de error
            }
        }

        /// <summary>
        /// Muestra el formulario para que un usuario contable solicite un cambio de rol.
        /// Pre-llena el email del usuario autenticado y presenta las opciones de rol limitadas a Manager y Accountant.
        /// </summary>
        /// <returns>La vista del formulario de solicitud de cambio de rol.</returns>
        [HttpGet]
        public IActionResult RequestRoleChange()
        {
            var model = new CreateRoleChangeRequestDTO();

            model.EmailAddress = User.Identity.Name;
            model.FullName = null; // Se deja vacío para que el usuario lo introduzca

            // Roles disponibles para la solicitud (valores en inglés que se enviarán a la API)
            ViewBag.RequestedRoles = new List<string> { "Manager", "Accountant" }; // Excluyendo "Admin" para esta solicitud

            return View(model);
        }

        /// <summary>
        /// Procesa la solicitud de cambio de rol enviada por el usuario contable.
        /// Valida los datos y envía la solicitud a la API, utilizando los datos de autenticación del servidor
        /// para el email y nombre completo por seguridad.
        /// </summary>
        /// <param name="requestDto">DTO con los detalles de la solicitud de cambio de rol.</param>
        /// <returns>Redirecciona al dashboard del contable con un mensaje de éxito o error.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestRoleChange(CreateRoleChangeRequestDTO requestDto)
        {
            // Vuelve a llenar los roles para el dropdown si la validación falla y se regresa la vista
            ViewBag.RequestedRoles = new List<string> { "Manager", "Admin" };

            // ¡IMPORTANTE POR SEGURIDAD! Siempre obtener EmailAddress y FullName del usuario autenticado en el servidor
            // Esto previene que un usuario intente enviar una solicitud en nombre de otro.
            requestDto.EmailAddress = User.Identity.Name;
            requestDto.FullName = User.FindFirst(ClaimTypes.GivenName)?.Value + " " + User.FindFirst(ClaimTypes.Surname)?.Value;
            if (string.IsNullOrWhiteSpace(requestDto.FullName))
            {
                // Fallback si no se encuentran Claims para GivenName/Surname, usar el nombre de usuario
                requestDto.FullName = User.Identity.Name;
            }

            // Validar el modelo (incluyendo Justification y RequestedRole, y FullName si se ha configurado así en el DTO)
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Por favor, corrige los errores en el formulario.";
                return View(requestDto); // Regresa la vista con los datos del formulario y los errores de validación
            }

            // Validar que el rol solicitado sea uno de los permitidos
            var allowedRoles = new List<string> { "Manager", "Admin" };
            if (string.IsNullOrWhiteSpace(requestDto.RequestedRole) || !allowedRoles.Contains(requestDto.RequestedRole))
            {
                ModelState.AddModelError("RequestedRole", "El rol solicitado no es válido.");
                TempData["ErrorMessage"] = "El rol solicitado no es válido. Solo se permiten Manager o Admin.";
                return View(requestDto);
            }

            try
            {
                var response = await _apiClient.PostAsync<CreateRoleChangeRequestDTO, object>("api/RoleChangeRequest", requestDto);

                if (response.Success)
                {
                    TempData["SuccessMessage"] = "Tu solicitud de cambio de rol ha sido enviada exitosamente. Se te notificará una vez que sea revisada.";
                    TempData["RedirectUrl"] = Url.Action("Dashboard", "Accountant");
                    return RedirectToAction("Dashboard", "Accountant");
                }
                else
                {
                    TempData["ErrorMessage"] = response.Message ?? "No se pudo enviar la solicitud de cambio de rol. Por favor, intenta de nuevo.";
                    if (!string.IsNullOrWhiteSpace(response.Message) && response.Message.Contains("ya existe", StringComparison.OrdinalIgnoreCase))
                    {
                        ModelState.AddModelError("", "Ya tienes una solicitud de cambio de rol pendiente o reciente.");
                    }
                    return View(requestDto);
                }
            }
            catch (HttpRequestException httpEx)
            {
                string apiErrorMessage = $"Error de conexión con el servicio: {httpEx.StatusCode} - {httpEx.Message}.";
                TempData["ErrorMessage"] = apiErrorMessage;
                ModelState.AddModelError("", apiErrorMessage);
                return View(requestDto);
            }
            catch (Exception ex)
            {
                string genericErrorMessage = $"Ocurrió un error inesperado al enviar la solicitud: {ex.Message}";
                TempData["ErrorMessage"] = genericErrorMessage;
                ModelState.AddModelError("", genericErrorMessage);
                return View(requestDto);
            }
        }
    }
}
