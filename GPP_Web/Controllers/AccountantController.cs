using GPP_Web.DTOs.Project;
using GPP_Web.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
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
            try
            {
                // Obtener todos los proyectos activos
                var response = await _apiClient.GetAsync<List<ProjectResponseDTO>>("api/Project/active");

                if (response.Success && response.Data != null)
                {
                    // Pasamos los proyectos activos a la vista
                    return View(response.Data);
                }
                else
                {
                    ViewBag.ErrorMessage = response.Message ?? "No se pudieron cargar los proyectos activos.";
                    return View(new List<ProjectResponseDTO>());
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Ocurrió un error inesperado: {ex.Message}";
                return View(new List<ProjectResponseDTO>());
            }
        }
    }
}
