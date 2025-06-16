using GPP_Web.DTOs.Project;
using GPP_Web.Models;
using GPP_Web.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using TimeZoneConverter;
using System.Linq;

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
