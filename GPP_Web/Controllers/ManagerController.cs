using GPP_Web.DTOs.Project;
using GPP_Web.DTOs.RoleChangeRequest;
using GPP_Web.Models;
using GPP_Web.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using TimeZoneConverter;

namespace GPP_Web.Controllers
{
    public class ManagerController : Controller
    {
        private readonly ILogger<ManagerController> _logger;
        private readonly GenericApiClient _apiClient;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="ManagerController"/>.
        /// </summary>
        /// <param name="logger">El registrador utilizado para registrar información y errores en el controlador.</param>
        /// <param name="apiClient">El cliente API genérico utilizado para realizar solicitudes a servicios externos.</param>
        public ManagerController(ILogger<ManagerController> logger, GenericApiClient apiClient)
        {
            _apiClient = apiClient;
            _logger = logger;
        }

        /// <summary>
        /// Muestra el formulario para que un usuario con rol "Manager" solicite un cambio de rol.
        /// Pre-llena el email del usuario autenticado y presenta las opciones de rol limitadas a Contador y Gerente.
        /// </summary>
        /// <returns>La vista del formulario de solicitud de cambio de rol.</returns>
        [HttpGet]
        public IActionResult RequestRoleChange()
        {
            var model = new CreateRoleChangeRequestDTO();

            model.EmailAddress = User.Identity.Name; // Asumiendo que el nombre de usuario es el email
            model.FullName = null; // Se deja vacío para que el usuario lo introduzca

            // Roles disponibles para la solicitud para un Manager
            // Los valores son en inglés, los nombres a mostrar se mapean en la vista.
            ViewBag.RequestedRoles = new List<string> { "Accountant", "Manager" }; // Solo Contador y Gerente

            return View(model);
        }

        /// <summary>
        /// Procesa la solicitud de cambio de rol enviada por un usuario con rol "Manager".
        /// Valida los datos y envía la solicitud a la API, utilizando los datos de autenticación del servidor
        /// para el email y nombre completo por seguridad.
        /// </summary>
        /// <param name="requestDto">DTO con los detalles de la solicitud de cambio de rol.</param>
        /// <returns>Redirecciona al dashboard del Manager con un mensaje de éxito o error.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestRoleChange(CreateRoleChangeRequestDTO requestDto)
        {
            // Roles permitidos para la validación (se mantienen en inglés para la lógica interna)
            var allowedRoles = new List<string> { "Accountant", "Admin" };
            ViewBag.RequestedRoles = allowedRoles; // Vuelve a llenar para el dropdown si se regresa la vista

            // ¡IMPORTANTE POR SEGURIDAD! Siempre obtener EmailAddress y FullName del usuario autenticado en el servidor
            requestDto.EmailAddress = User.Identity.Name;
            requestDto.FullName = User.FindFirst(ClaimTypes.GivenName)?.Value + " " + User.FindFirst(ClaimTypes.Surname)?.Value;
            if (string.IsNullOrWhiteSpace(requestDto.FullName))
            {
                requestDto.FullName = User.Identity.Name;
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Por favor, corrige los errores en el formulario.";
                return View(requestDto);
            }

            // Validar que el rol solicitado sea uno de los permitidos para un Manager
            if (string.IsNullOrWhiteSpace(requestDto.RequestedRole) || !allowedRoles.Contains(requestDto.RequestedRole))
            {
                ModelState.AddModelError("RequestedRole", "El rol solicitado no es válido.");
                TempData["ErrorMessage"] = "El rol solicitado no es válido. Solo se permiten Contador o Administrador.";
                return View(requestDto);
            }

            try
            {
                var response = await _apiClient.PostAsync<CreateRoleChangeRequestDTO, object>("api/RoleChangeRequest", requestDto);

                if (response.Success)
                {
                    TempData["SuccessMessage"] = "Tu solicitud de cambio de rol ha sido enviada exitosamente. Se te notificará una vez que sea revisada.";
                    TempData["RedirectUrl"] = Url.Action("Dashboard", "Manager");
                    return RedirectToAction("Dashboard", "Manager");
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

        public async Task<IActionResult> Dashboard()
        {
            string greetingMessage;
            string greetingIconClass;
            TimeZoneInfo costaRicaTimeZone;
            DateOnly todayCostaRica; // Declarar como DateOnly

            try
            {
                try
                {
                    costaRicaTimeZone = TZConvert.GetTimeZoneInfo("America/Costa_Rica");
                }
                catch (TimeZoneNotFoundException)
                {
                    costaRicaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Central America Standard Time");
                }

                DateTime costaRicaTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, costaRicaTimeZone);
                int hour = costaRicaTime.Hour;

                if (hour >= 6 && hour < 12)
                {
                    greetingMessage = "Buenos Días";
                    greetingIconClass = "bi bi-sun-fill";
                }
                else if (hour >= 12 && hour < 18)
                {
                    greetingMessage = "Buenas Tardes";
                    greetingIconClass = "bi bi-cloud-sun-fill";
                }
                else
                {
                    greetingMessage = "Buenas Noches";
                    greetingIconClass = "bi bi-moon-stars-fill";
                }

                // **CORRECCIÓN AQUÍ:** Convertir DateTime a DateOnly
                todayCostaRica = DateOnly.FromDateTime(costaRicaTime);
                ViewBag.TodayCostaRica = todayCostaRica; // Ahora ViewBag.TodayCostaRica es DateOnly
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al determinar la hora de Costa Rica: {ex.Message}");
                greetingMessage = "Hola";
                greetingIconClass = "bi bi-person-fill";
                // Fallback: usar la fecha UTC actual como DateOnly
                todayCostaRica = DateOnly.FromDateTime(DateTime.UtcNow);
                ViewBag.TodayCostaRica = todayCostaRica;
            }

            ViewBag.GreetingMessage = greetingMessage;
            ViewBag.GreetingIconClass = greetingIconClass;

            List<ProjectResponseDTO> projects = new List<ProjectResponseDTO>();
            try
            {
                var managerEmail = User.Identity.Name;
                var response = await _apiClient.GetAsync<List<ProjectResponseDTO>>($"api/Project/manager/{managerEmail}");

                if (response.Success && response.Data != null)
                {
                    projects = response.Data;
                }
                else
                {
                    ViewBag.ErrorMessage = response.Message ?? "No se pudieron cargar los proyectos asignados.";
                }
            }
            catch (HttpRequestException ex)
            {
                ViewBag.ErrorMessage = $"Error de conexión con la API: {ex.Message}";
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Ocurrió un error inesperado: {ex.Message}";
            }

            int activeProjectsCount = projects.Count(p => p.Status == "Active");
            ViewBag.ActiveProjectsCount = activeProjectsCount;

            decimal totalExpensesToday = 0;
            foreach (var project in projects)
            {
                if (project.BudgetParts != null)
                {
                    foreach (var budgetPart in project.BudgetParts)
                    {
                        if (budgetPart.Expenses != null)
                        {
                            // **ESTA LÍNEA AHORA FUNCIONARÁ CORRECTAMENTE:**
                            // Ambos lados de la comparación son DateOnly
                            totalExpensesToday += budgetPart.Expenses
                                                    .Where(e => e.ExpenseDate == todayCostaRica) // Usar la variable local todayCostaRica
                                                    .Sum(e => e.ExpenseAmount);
                        }
                    }
                }
            }
            ViewBag.TotalExpensesToday = totalExpensesToday;

            int completedProjectsCount = projects.Count(p => p.RemainingBudget == 0);
            ViewBag.CompletedProjectsCount = completedProjectsCount;

            return View(projects);
        }

        /// <summary>
        /// Devuelve la vista de error, proporcionando información sobre la solicitud que falló.
        /// </summary>
        /// <returns>Un <see cref="IActionResult"/> que representa la vista de error con un modelo que contiene el identificador de la solicitud.</returns>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
