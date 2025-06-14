using GPP_Web.DTOs.Project;
using GPP_Web.Models;
using GPP_Web.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Diagnostics;

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
        /// Maneja la solicitud para la vista principal del controlador, recuperando los proyectos asignados al manager desde la API.
        /// </summary>
        /// <returns>Una tarea que representa la operación asíncrona y devuelve un <see cref="IActionResult"/> con la vista que muestra los proyectos asignados o un mensaje de error en caso de fallo.</returns>
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var managerEmail = User.Identity.Name; // Obtener el email del manager desde el usuario autenticado
                var response = await _apiClient.GetAsync<List<ProjectResponseDTO>>($"api/Project/manager/{managerEmail}");

                if (response.Success && response.Data != null)
                {
                    return View(response.Data); // Pasa los proyectos a la vista
                }
                else
                {
                    ViewBag.ErrorMessage = response.Message ?? "No se pudieron cargar los proyectos asignados.";
                    return View(new List<ProjectResponseDTO>());
                }
            }
            catch (HttpRequestException ex)
            {
                ViewBag.ErrorMessage = $"Error de conexión con la API: {ex.Message}";
                return View(new List<ProjectResponseDTO>());
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Ocurrió un error inesperado: {ex.Message}";
                return View(new List<ProjectResponseDTO>());
            }
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
