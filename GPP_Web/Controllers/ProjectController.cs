using GPP_Web.DTOs.BudgetPart;
using GPP_Web.DTOs.Project;
using GPP_Web.Models;
using GPP_Web.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace GPP_Web.Controllers
{
    public class ProjectController : Controller
    {
        private readonly GenericApiClient _apiClient;

        public ProjectController(GenericApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        // Acción para mostrar la vista general de un proyecto específico
        public async Task<IActionResult> GeneralView(int projectId)
        {
            try
            {
                var response = await _apiClient.GetAsync<ProjectResponseDTO>($"api/Project/{projectId}");

                if (response.Success && response.Data != null)
                {
                    return View(response.Data); // Pasa los datos del proyecto a la vista
                }
                else
                {
                    ViewBag.ErrorMessage = response.Message ?? "No se pudo cargar la información del proyecto.";
                    return View();
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Ocurrió un error inesperado: {ex.Message}";
                return View();
            }
        }

        // Acción para obtener los proyectos del usuario autenticado (Manager)
        public async Task<IActionResult> ManagerProjects()
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
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Ocurrió un error inesperado: {ex.Message}";
                return View(new List<ProjectResponseDTO>());
            }
        }

        // Acción para mostrar el formulario de creación de un nuevo proyecto
        public IActionResult CreateProject()
        {
            // Inicializa el modelo con una lista vacía de partidas presupuestarias
            var projectDto = new CreateProjectDTO
            {
                BudgetParts = new List<CreateBudgetPartDTO>() // Inicializa la lista de BudgetParts
            };
            return View(projectDto);
        }

        // Acción para procesar el formulario de creación de un nuevo proyecto
        // Acción para procesar el formulario de creación de un nuevo proyecto
        [HttpPost]
        public async Task<IActionResult> CreateProject(CreateProjectDTO projectDto)
        {
            // Verifica que al menos haya una partida presupuestaria
            if (projectDto.BudgetParts == null || projectDto.BudgetParts.Count == 0)
            {
                ModelState.AddModelError("BudgetParts", "Debes agregar al menos una partida presupuestaria.");
                return View(projectDto); // Si no hay partidas, devuelve el formulario con el error
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Usamos PostAsync para enviar la solicitud de creación de un nuevo proyecto
                    var response = await _apiClient.PostAsync<CreateProjectDTO, ProjectResponseDTO>("api/Project", projectDto);

                    if (response.Success)
                    {
                        TempData["SuccessMessage"] = "El proyecto se ha creado correctamente.";
                        return RedirectToAction("ManagerProjects"); // Redirige a los proyectos del manager
                    }
                    else
                    {
                        ViewBag.ErrorMessage = response.Message ?? "No se pudo crear el proyecto.";
                        return View(projectDto);
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.ErrorMessage = $"Ocurrió un error inesperado: {ex.Message}";
                    return View(projectDto);
                }
            }

            return View(projectDto);
        }
    }
}
