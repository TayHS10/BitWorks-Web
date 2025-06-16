using GPP_Web.DTOs.BudgetPart;
using GPP_Web.DTOs.Expense;
using GPP_Web.Models;
using GPP_Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace GPP_Web.Controllers
{
    public class ExpenseController : Controller
    {
        private readonly GenericApiClient _apiClient;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ExpenseController(GenericApiClient apiClient, IWebHostEnvironment webHostEnvironment)
        {
            _apiClient = apiClient;
            _webHostEnvironment = webHostEnvironment;
        }

        // Método para listar todos los gastos con paginación
        [HttpGet]
        public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 10) // Añadimos parámetros de paginación
        {
            List<ExpenseResponseDTO> allExpenses = new List<ExpenseResponseDTO>();
            ViewBag.ErrorMessage = null;
            ViewBag.InfoMessage = null;

            try
            {
                // Llamada al endpoint para obtener todos los gastos
                var response = await _apiClient.GetAsync<List<ExpenseResponseDTO>>("api/Expense");

                if (response.Success && response.Data != null)
                {
                    allExpenses = response.Data;

                    if (!allExpenses.Any()) // Si la lista está vacía
                    {
                        ViewBag.InfoMessage = "No se encontraron gastos registrados en el sistema.";
                        ViewBag.TotalPages = 1; // Aunque no haya gastos, al menos 1 página
                        ViewBag.CurrentPage = 1;
                        ViewBag.HasPreviousPage = false;
                        ViewBag.HasNextPage = false;
                        return View(new List<ExpenseResponseDTO>()); // Devolver una lista vacía
                    }

                    // --- Lógica de Paginación en el Controlador ---
                    var totalCount = allExpenses.Count;
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


                    // Obtener los gastos para la página actual
                    var expensesForPage = allExpenses
                                            .Skip((pageNumber - 1) * pageSize)
                                            .Take(pageSize)
                                            .ToList();

                    return View(expensesForPage); // Pasar solo los gastos de la página actual
                }
                else
                {
                    ViewBag.ErrorMessage = response.Message ?? "No se pudieron cargar los gastos del sistema.";
                    ViewBag.TotalPages = 1;
                    ViewBag.CurrentPage = 1;
                    ViewBag.HasPreviousPage = false;
                    ViewBag.HasNextPage = false;
                    return View(new List<ExpenseResponseDTO>());
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Ocurrió un error inesperado al cargar todos los gastos: {ex.Message}";
                ViewBag.TotalPages = 1;
                ViewBag.CurrentPage = 1;
                ViewBag.HasPreviousPage = false;
                ViewBag.HasNextPage = false;
                return View(new List<ExpenseResponseDTO>()); // Retornar lista vacía en caso de error
            }
        }

        // Acción GET para mostrar el formulario
        [HttpGet]
        public async Task<IActionResult> Create(int budgetPartId) // Recibe el ID de la partida presupuestaria
        {
            ViewBag.BudgetPartId = budgetPartId;

            decimal remainingAmount = 0;
            string budgetPartName = $"ID de Partida: {budgetPartId}"; // Valor por defecto

            try
            {
                var responseBudgetPart = await _apiClient.GetAsync<BudgetPartResponseDTO>($"api/BudgetPart/{budgetPartId}");

                if (responseBudgetPart.Success && responseBudgetPart.Data != null)
                {
                    budgetPartName = responseBudgetPart.Data.PartName;
                    remainingAmount = responseBudgetPart.Data.RemainingAmount;
                }
                else
                {
                    ViewBag.ErrorMessage = responseBudgetPart.Message ?? "No se pudo cargar la información de la partida presupuestaria.";
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Ocurrió un error inesperado al cargar la partida: {ex.Message}";
            }

            ViewBag.BudgetPartName = budgetPartName;
            ViewBag.RemainingAmount = remainingAmount;

            return View(new CreateExpenseDTO { BudgetPartId = budgetPartId, ExpenseDate = DateOnly.FromDateTime(DateTime.Today) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // Buena práctica para protección CSRF
        public async Task<IActionResult> Create(CreateExpenseDTO expenseDto, IFormFile? DocumentFile)
        {
            // Re-obtener los datos de la partida para repoblar ViewBags en caso de error de validación
            ViewBag.BudgetPartId = expenseDto.BudgetPartId;
            string budgetPartName = $"ID de Partida: {expenseDto.BudgetPartId}";
            decimal remainingAmount = 0;

            try
            {
                var responseBudgetPart = await _apiClient.GetAsync<BudgetPartResponseDTO>($"api/BudgetPart/{expenseDto.BudgetPartId}");
                if (responseBudgetPart.Success && responseBudgetPart.Data != null)
                {
                    budgetPartName = responseBudgetPart.Data.PartName;
                    remainingAmount = responseBudgetPart.Data.RemainingAmount;
                }
                else
                {
                    ModelState.AddModelError(string.Empty, responseBudgetPart.Message ?? "No se pudo cargar la partida presupuestaria para validación.");
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Error al obtener datos de la partida: {ex.Message}");
            }

            ViewBag.BudgetPartName = budgetPartName;
            ViewBag.RemainingAmount = remainingAmount;

            // Lógica para manejar la subida del archivo
            if (DocumentFile != null && DocumentFile.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
                var fileExtension = Path.GetExtension(DocumentFile.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("DocumentFile", "El formato del archivo no es válido. Solo se permiten JPG, JPEG, PNG y PDF.");
                }
                else if (DocumentFile.Length > 5 * 1024 * 1024) // 5 MB de límite
                {
                    ModelState.AddModelError("DocumentFile", "El tamaño del archivo no debe exceder los 5 MB.");
                }
                else
                {
                    try
                    {
                        // Crea un nombre de archivo único para evitar colisiones
                        var uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
                        var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "documents");

                        // Asegúrate de que el directorio exista
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        // Guarda el archivo en el servidor
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await DocumentFile.CopyToAsync(fileStream);
                        }

                        // Asigna el nombre único del archivo al DTO
                        expenseDto.DocumentReference = uniqueFileName;
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("DocumentFile", $"Error al guardar el archivo adjunto: {ex.Message}");
                        // Log the error for debugging purposes in a real application
                        // _logger.LogError(ex, "Error saving document file.");
                    }
                }
            }
            else
            {
                // Si no se adjunta archivo, asegúrate de que DocumentReference sea nulo o vacío
                expenseDto.DocumentReference = null;
            }


            // VALIDACIÓN: Gasto vs Monto Restante
            if (ModelState.IsValid && expenseDto.ExpenseAmount > remainingAmount)
            {
                ModelState.AddModelError("ExpenseAmount", $"El monto del gasto (₡{expenseDto.ExpenseAmount:N0}) sobrepasa el monto restante disponible en la partida (₡{remainingAmount:N0}).");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // La API solo recibirá el nombre del archivo, no el IFormFile
                    var response = await _apiClient.PostAsync<CreateExpenseDTO, ApiResponse<object>>("api/Expense", expenseDto);

                    if (response.Success)
                    {
                        TempData["SuccessMessage"] = "El gasto se ha registrado correctamente.";
                        // Redirige al listado de gastos de la partida para ver el nuevo gasto
                        // Asumiendo que tu Index de gastos recibe budgetPartId
                        TempData["RedirectUrl"] = Url.Action("Create", "Expense", new { budgetPartId = expenseDto.BudgetPartId });
                        return RedirectToAction("Create", "Expense", new { budgetPartId = expenseDto.BudgetPartId });
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, response.Message ?? "No se pudo registrar el gasto en el sistema externo.");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, $"Ocurrió un error inesperado al registrar el gasto. Por favor, inténtalo de nuevo. ({ex.Message})");
                }
            }

            // Si llegamos aquí, algo falló, se debe mostrar la vista con los errores de validación
            return View(expenseDto);
        }

        // Acción para obtener los gastos de una partida presupuestaria
        public async Task<IActionResult> GetExpensesByBudgetPart(int budgetPartId)
        {
            List<ExpenseResponseDTO> expenses = new List<ExpenseResponseDTO>();
            ViewBag.ErrorMessage = null;
            ViewBag.InfoMessage = null; // Inicializar ViewBag.InfoMessage

            try
            {
                // Llamada al endpoint para obtener los gastos de la partida
                var response = await _apiClient.GetAsync<List<ExpenseResponseDTO>>($"api/Expense/budgetpart/{budgetPartId}/expenses");

                if (response.Success && response.Data != null)
                {
                    expenses = response.Data;
                    if (!expenses.Any()) // Si la API devuelve una lista vacía
                    {
                        ViewBag.InfoMessage = "No hay gastos registrados para esta partida presupuestaria.";
                    }
                }
                else
                {
                    // Decodifica cualquier entidad HTML en el mensaje de error de la API
                    ViewBag.ErrorMessage = WebUtility.HtmlDecode(response.Message ?? "No se pudo cargar los gastos.");
                }
            }
            catch (Exception ex)
            {
                // Decodifica cualquier entidad HTML en el mensaje de la excepción
                ViewBag.ErrorMessage = WebUtility.HtmlDecode($"Ocurrió un error inesperado: {ex.Message}");
            }

            // Pasa la lista de gastos (que podría estar vacía) a la vista.
            // La vista se encargará de mostrar la tabla o el mensaje de "no hay datos".
            return View(expenses);
        }
    }
}
