using GPP_Web.DTOs.BudgetPart;
using GPP_Web.DTOs.Project;
using GPP_Web.DTOs.User;
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

        // Método para listar todos los proyectos activos con paginación
        [HttpGet]
        public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 10) // Añadimos parámetros de paginación
        {
            List<ProjectResponseDTO> allProjects = new List<ProjectResponseDTO>();
            ViewBag.ErrorMessage = null;
            ViewBag.InfoMessage = null;

            try
            {
                // CAMBIO: Llamada al endpoint para obtener SOLO los proyectos activos
                var response = await _apiClient.GetAsync<List<ProjectResponseDTO>>("api/Project/active");

                if (response.Success && response.Data != null)
                {
                    allProjects = response.Data;

                    if (!allProjects.Any()) // Si la lista está vacía
                    {
                        ViewBag.InfoMessage = "No se encontraron proyectos activos registrados en el sistema.";
                        ViewBag.TotalPages = 1;
                        ViewBag.CurrentPage = 1;
                        ViewBag.HasPreviousPage = false;
                        ViewBag.HasNextPage = false;
                        return View(new List<ProjectResponseDTO>()); // Devolver una lista vacía
                    }

                    // --- Lógica de Paginación en el Controlador ---
                    var totalCount = allProjects.Count;
                    int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                    // VALIDACIÓN Y AJUSTE DEL NÚMERO DE PÁGINA (CLAMPEO)
                    // Asegurarse de que pageNumber no sea menor que 1
                    if (pageNumber < 1) pageNumber = 1;
                    // Asegurarse de que pageNumber no sea mayor que TotalPages (y que haya al menos 1 página)
                    if (pageNumber > totalPages && totalPages > 0) pageNumber = totalPages;


                    ViewBag.TotalPages = totalPages;
                    ViewBag.CurrentPage = pageNumber; // Ahora ViewBag.CurrentPage siempre es válido y ajustado
                    ViewBag.PageSize = pageSize;

                    ViewBag.HasPreviousPage = (pageNumber > 1);
                    ViewBag.HasNextPage = (pageNumber < totalPages); // Usa 'totalPages' (ya clamped)


                    // Obtener los proyectos para la página actual
                    var projectsForPage = allProjects
                                            .Skip((pageNumber - 1) * pageSize)
                                            .Take(pageSize)
                                            .ToList();

                    return View(projectsForPage); // Pasar solo los proyectos de la página actual
                }
                else
                {
                    ViewBag.ErrorMessage = response.Message ?? "No se pudieron cargar los proyectos activos del sistema.";
                    ViewBag.TotalPages = 1;
                    ViewBag.CurrentPage = 1;
                    ViewBag.HasPreviousPage = false;
                    ViewBag.HasNextPage = false;
                    return View(new List<ProjectResponseDTO>());
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Ocurrió un error inesperado al cargar los proyectos: {ex.Message}";
                ViewBag.TotalPages = 1;
                ViewBag.CurrentPage = 1;
                ViewBag.HasPreviousPage = false;
                ViewBag.HasNextPage = false;
                return View(new List<ProjectResponseDTO>()); // Retornar lista vacía en caso de error
            }
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

        /// <summary>
        /// Acción GET: Muestra el formulario para crear un nuevo proyecto.
        /// Esta acción es invocada cuando el usuario navega a la URL para crear un proyecto.
        /// </summary>
        /// <returns>La vista "CreateProject" con un modelo inicial vacío.</returns>
        public IActionResult CreateProject()
        {
            var projectDto = new CreateProjectDTO
            {
                BudgetParts = new List<CreateBudgetPartDTO>()
            };
            return View(projectDto);
        }

        /// <summary>
        /// Acción POST: Procesa el envío del formulario para crear un nuevo proyecto.
        /// Esta acción es invocada cuando el usuario envía el formulario de creación.
        /// </summary>
        /// <param name="projectDto">DTO que contiene los datos del formulario (código, nombre, presupuesto, etc.,
        /// y la lista de partidas presupuestarias).</param>
        /// <returns>
        /// Redirección al "ManagerProjects" en caso de éxito, o la misma vista con mensajes de error
        /// si la validación falla o la API devuelve un error.
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken] // Atributo de seguridad para proteger contra ataques CSRF.
        public async Task<IActionResult> CreateProject(CreateProjectDTO projectDto)
        {
            // --- VALIDACIONES DE NEGOCIO PERSONALIZADAS ---

            // 1. Validación de que al menos una partida presupuestaria ha sido agregada.
            if (projectDto.BudgetParts == null || !projectDto.BudgetParts.Any())
            {
                ModelState.AddModelError("BudgetParts", "Debes agregar al menos una partida presupuestaria.");
            }

            // 2. Validación de montos del presupuesto del proyecto.
            // La validación DataAnnotation [Range] en el DTO ya cubre que sea > 0,
            // esta es una validación adicional a nivel de controlador.
            if (projectDto.Budget <= 0)
            {
                ModelState.AddModelError("Budget", "El presupuesto del proyecto debe ser un valor positivo.");
            }

            // 3. Validación de montos de partidas presupuestarias (individuales y suma total)
            decimal totalAllocatedAmount = 0;
            if (projectDto.BudgetParts != null)
            {
                for (int i = 0; i < projectDto.BudgetParts.Count; i++)
                {
                    var part = projectDto.BudgetParts[i];
                    if (part.AllocatedAmount <= 0)
                    {
                        ModelState.AddModelError($"BudgetParts[{i}].AllocatedAmount", "El monto asignado para esta partida debe ser un valor positivo.");
                    }
                    totalAllocatedAmount += part.AllocatedAmount;
                }

                if (totalAllocatedAmount > projectDto.Budget)
                {
                    ModelState.AddModelError("BudgetParts", "La suma de los montos asignados en las partidas presupuestarias no puede exceder el presupuesto total del proyecto.");
                }
            }

            // 4. Validación de que el gestor especificado existe en la lista de todos los usuarios.
            if (string.IsNullOrEmpty(projectDto.ManagerEmail))
            {
                ModelState.AddModelError("ManagerEmail", "El correo electrónico del gestor es obligatorio.");
            }
            else
            {
                try
                {
                    var apiResponseForUsers = await _apiClient.GetAsync<List<UserResponseDTO>>("api/User");

                    if (apiResponseForUsers == null || !apiResponseForUsers.Success || apiResponseForUsers.Data == null ||
                        !apiResponseForUsers.Data.Any(u => u.Email.Equals(projectDto.ManagerEmail, StringComparison.OrdinalIgnoreCase)))
                    {
                        ModelState.AddModelError("ManagerEmail", "El correo electrónico del gestor especificado no existe en el sistema.");
                    }
                }
                catch (HttpRequestException httpEx)
                {
                    Console.WriteLine($"ERROR: HttpRequestException al obtener usuarios de la API: {httpEx.StatusCode?.ToString() ?? "N/A"}. Mensaje: {httpEx.Message}");
                    ModelState.AddModelError("", "No se pudo verificar la existencia del gestor debido a un problema de comunicación con el servicio de usuarios.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR: Error inesperado al obtener usuarios para verificar gestor: {ex.Message}");
                    ModelState.AddModelError("", "Ocurrió un error inesperado al verificar la existencia del gestor.");
                }
            }

            // 5. NUEVA VALIDACIÓN: Validar si existe otro proyecto con el mismo código.
            if (string.IsNullOrEmpty(projectDto.ProjectCode)) // Usamos ProjectCode, no 'Code'
            {
                ModelState.AddModelError("ProjectCode", "El código del proyecto es obligatorio.");
            }
            else
            {
                try
                {
                    // ¡RUTA CORREGIDA! Ahora usa "api/Project/active"
                    var existingProjectsResponse = await _apiClient.GetAsync<List<ProjectResponseDTO>>("api/Project/active");

                    if (existingProjectsResponse != null && existingProjectsResponse.Success && existingProjectsResponse.Data != null)
                    {
                        // Se verifica si algún proyecto existente tiene el mismo código (comparación sin distinción de mayúsculas/minúsculas).
                        // Asumimos que ProjectResponseDTO tiene una propiedad ProjectCode o Code para comparar.
                        // Usaremos projectDto.ProjectCode para la comparación
                        if (existingProjectsResponse.Data.Any(p => p.ProjectCode.Equals(projectDto.ProjectCode, StringComparison.OrdinalIgnoreCase)))
                        {
                            ModelState.AddModelError("ProjectCode", "Ya existe otro proyecto activo con el mismo código. Por favor, use un código diferente.");
                        }
                    }
                    else
                    {
                        // Si no se pudieron recuperar los proyectos o la API reportó un error.
                        ModelState.AddModelError("", existingProjectsResponse?.Message ?? "No se pudieron recuperar los proyectos existentes para validar el código. Intente de nuevo.");
                    }
                }
                catch (HttpRequestException httpEx)
                {
                    Console.WriteLine($"ERROR: HttpRequestException al obtener proyectos existentes desde 'api/Project/active': {httpEx.StatusCode?.ToString() ?? "N/A"}. Mensaje: {httpEx.Message}");
                    ModelState.AddModelError("", "No se pudo verificar el código del proyecto debido a un problema de comunicación con el servicio de proyectos.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR: Error inesperado al obtener proyectos para verificar código: {ex.Message}");
                    ModelState.AddModelError("", "Ocurrió un error inesperado al verificar el código del proyecto.");
                }
            }

            // --- FIN VALIDACIONES DE NEGOCIO PERSONALIZADAS ---

            if (!ModelState.IsValid)
            {
                return View(projectDto);
            }

            try
            {
                var response = await _apiClient.PostAsync<CreateProjectDTO, ProjectResponseDTO>("api/Project", projectDto);

                if (response.Success)
                {
                    TempData["SuccessMessage"] = "El proyecto se ha creado correctamente.";
                    TempData["RedirectUrl"] = Url.Action("Dashboard", "Accountant");
                    return RedirectToAction("Dashboard", "Accountant");
                }
                else
                {
                    ModelState.AddModelError("", response.Message ?? "No se pudo crear el proyecto debido a un error de la API.");
                    return View(projectDto);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Ocurrió un error inesperado al comunicarse con el servidor: {ex.Message}");
                return View(projectDto);
            }
        }

        // Acción para obtener los proyectos activos
        public async Task<IActionResult> DeleteProjects()
        {
            try
            {
                // Llamada a la API para obtener los proyectos activos
                var response = await _apiClient.GetAsync<List<ProjectResponseDTO>>("api/Project/active");

                if (response.Success && response.Data != null)
                {
                    return View(response.Data); // Retorna los proyectos activos a la vista
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

        // Acción para desactivar un proyecto
        [HttpPost]
        public async Task<IActionResult> DeactivateProject(int projectId)
        {
            try
            {
                // Realizamos una solicitud PUT para desactivar el proyecto
                var response = await _apiClient.PutAsync<ApiResponse<object>>($"api/Project/{projectId}/Deactivate", null);

                if (response.Success)
                {
                    TempData["SuccessMessage"] = "El proyecto ha sido desactivado correctamente.";
                    TempData["RedirectUrl"] = Url.Action("Dashboard", "Accountant");
                    return RedirectToAction("Dashboard", "Accountant");
                }
                else
                {
                    ViewBag.ErrorMessage = response.Message ?? "No se pudo desactivar el proyecto.";
                }

                return RedirectToAction("ManagerProjects");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Ocurrió un error inesperado: {ex.Message}";
                return RedirectToAction("ManagerProjects");
            }
        }

    }
}
