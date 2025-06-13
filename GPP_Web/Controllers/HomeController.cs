using GPP_Web.DTOs.Project;
using GPP_Web.DTOs.RoleChangeRequest;
using GPP_Web.Models;
using GPP_Web.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Diagnostics;

namespace GPP_Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly GenericApiClient _apiClient;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="HomeController"/>.
        /// </summary>
        /// <param name="logger">El registrador utilizado para registrar informaci�n y errores en el controlador.</param>
        /// <param name="apiClient">El cliente API gen�rico utilizado para realizar solicitudes a servicios externos.</param>
        public HomeController(ILogger<HomeController> logger, GenericApiClient apiClient)
        {
            _apiClient = apiClient;
            _logger = logger;
        }

        /// <summary>
        /// Maneja la solicitud para la vista principal del controlador, recuperando una lista de proyectos activos desde la API.
        /// </summary>
        /// <returns>Una tarea que representa la operaci�n as�ncrona y devuelve un <see cref="IActionResult"/> que representa la vista con la lista de proyectos activos o un mensaje de error en caso de fallo.</returns>
        /// <remarks>Si la recuperaci�n de proyectos falla, se muestra un mensaje de error y se devuelve una lista vac�a para evitar que la vista se rompa.</remarks>
        public async Task<IActionResult> Index()
        {
            try
            {
                var response = await _apiClient.GetAsync<List<ProjectResponseDTO>>("api/Project/active");

                if (response.Success && response.Data != null)
                {
                    return View(response.Data);
                }
                else
                {
                    ViewBag.ErrorMessage = response.Message ?? "No se pudieron cargar los proyectos activos.";
                    return View(new List<ProjectResponseDTO>());
                }
            }
            catch (HttpRequestException ex)
            {
                ViewBag.ErrorMessage = $"Error de conexi�n con la API: {ex.Message}";
                return View(new List<ProjectResponseDTO>());
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Ocurri� un error inesperado: {ex.Message}";
                return View(new List<ProjectResponseDTO>());
            }
        }

        /// <summary>
        /// Devuelve la vista de error, proporcionando informaci�n sobre la solicitud que fall�.
        /// </summary>
        /// <returns>Un <see cref="IActionResult"/> que representa la vista de error con un modelo que contiene el identificador de la solicitud.</returns>
        /// <remarks>El identificador de la solicitud se utiliza para rastrear errores espec�ficos en los registros.</remarks>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // M�todo para mostrar la vista de "Convertirse en colaborador"
        public IActionResult ConvertirseEnColaborador()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ConvertirseEnColaborador(CreateRoleChangeRequestDTO request)
        {
            if (ModelState.IsValid)
            {
                // Especificamos expl�citamente los tipos gen�ricos: el tipo de solicitud (TRequest) y el tipo de respuesta (ApiResponse<object>).
                var response = await _apiClient.PostAsync<CreateRoleChangeRequestDTO, ApiResponse<object>>("api/RoleChangeRequest", request);

                if (response.Success)
                {
                    ViewBag.SuccessMessage = "Gracias por tu inter�s, nos pondremos en contacto pronto.";
                    return View();
                }
                else
                {
                    ViewBag.ErrorMessage = response.Message ?? "Ocurri� un error al registrar tu solicitud.";
                    return View();
                }
            }

            return View(request);
        }

    }
}
