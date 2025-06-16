using GPP_Web.DTOs.BudgetPart;
using GPP_Web.DTOs.Expense;
using GPP_Web.Models;
using GPP_Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;

namespace GPP_Web.Controllers
{
    public class ExpenseController : Controller
    {
        private readonly GenericApiClient _apiClient;

        public ExpenseController(GenericApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        // Acción GET para mostrar el formulario
        [HttpGet]
        public async Task<IActionResult> Create(int budgetPartId) // Recibe el ID de la partida presupuestaria
        {
            // Asegúrate de que budgetPartId se reciba correctamente (ej: desde la URL)
            ViewBag.BudgetPartId = budgetPartId;

            decimal remainingAmount = 0;
            string budgetPartName = $"ID de Partida: {budgetPartId}"; // Valor por defecto

            try
            {
                // Obtener los detalles de la partida para mostrar el nombre y el monto restante
                var responseBudgetPart = await _apiClient.GetAsync<BudgetPartResponseDTO>($"api/BudgetPart/{budgetPartId}");

                if (responseBudgetPart.Success && responseBudgetPart.Data != null)
                {
                    budgetPartName = responseBudgetPart.Data.PartName;
                    remainingAmount = responseBudgetPart.Data.RemainingAmount; // Obtener el monto restante
                }
                else
                {
                    // Manejo de error si no se puede obtener la partida
                    ViewBag.ErrorMessage = responseBudgetPart.Message ?? "No se pudo cargar la información de la partida presupuestaria.";
                }
            }
            catch (Exception ex)
            {
                // Loggear el error (en un entorno de producción)
                // _logger.LogError(ex, "Error al cargar la información de la partida presupuestaria.");
                ViewBag.ErrorMessage = $"Ocurrió un error inesperado al cargar la partida: {ex.Message}";
            }

            ViewBag.BudgetPartName = budgetPartName;
            ViewBag.RemainingAmount = remainingAmount; // Pasa el monto restante a la vista

            return View(new CreateExpenseDTO { BudgetPartId = budgetPartId, ExpenseDate = DateOnly.FromDateTime(DateTime.Today) });
        }

        [HttpPost]
        // [ValidateAntiForgeryToken] // Considera añadir esto para protección CSRF
        public async Task<IActionResult> Create(CreateExpenseDTO expenseDto, IFormFile? DocumentFile)
        {
            // ... (Tu código para cargar BudgetPartName, RemainingAmount y manejar la subida de archivos) ...
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

            if (DocumentFile != null)
            {
                // ... (Tu lógica de validación y subida de archivos) ...
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
                    var response = await _apiClient.PostAsync<CreateExpenseDTO, ApiResponse<object>>("api/Expense", expenseDto);

                    if (response.Success)
                    {
                        // **CAMBIO CLAVE AQUÍ:**
                        // Establece el mensaje de éxito y la URL de redirección en TempData.
                        TempData["SuccessMessage"] = "El gasto se ha registrado correctamente.";
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

            return View(expenseDto);
        }

        // Acción para obtener los gastos de una partida presupuestaria
        public async Task<IActionResult> GetExpensesByBudgetPart(int budgetPartId)
        {
            try
            {
                // Llamada al endpoint para obtener los gastos de la partida
                var response = await _apiClient.GetAsync<List<ExpenseResponseDTO>>($"api/Expense/budgetpart/{budgetPartId}/expenses");

                if (response.Success && response.Data != null)
                {
                    return View(response.Data); // Pasa los datos a la vista
                }
                else
                {
                    ViewBag.ErrorMessage = response.Message ?? "No se pudo cargar los gastos.";
                    return View(new List<ExpenseResponseDTO>());
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Ocurrió un error inesperado: {ex.Message}";
                return View(new List<ExpenseResponseDTO>());
            }
        }
    }
}
