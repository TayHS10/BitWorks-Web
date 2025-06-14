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

        // Acción para mostrar el formulario de registro de gasto
        public IActionResult Create(int budgetPartId)
        {
            ViewBag.BudgetPartId = budgetPartId;
            return View();
        }

        // Acción para registrar un gasto
        [HttpPost]
        public async Task<IActionResult> Create(CreateExpenseDTO expenseDto, IFormFile? DocumentFile)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Verificamos que se haya subido un archivo
                    if (DocumentFile != null)
                    {
                        // Validar que el archivo sea una imagen
                        if (DocumentFile.ContentType.StartsWith("image/"))
                        {
                            // Generar nombre único para el archivo y guardarlo
                            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(DocumentFile.FileName);
                            var fileExtension = Path.GetExtension(DocumentFile.FileName);
                            var newFileName = $"{fileNameWithoutExtension}_{Guid.NewGuid()}{fileExtension}";
                            var filePath = Path.Combine("wwwroot/images/documents", newFileName);

                            // Guardamos el archivo en el servidor
                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await DocumentFile.CopyToAsync(stream);
                            }

                            expenseDto.DocumentReference = newFileName;  // Guardamos solo el nombre o la ruta del archivo
                        }
                        else
                        {
                            ModelState.AddModelError("DocumentFile", "Solo se permiten archivos de imagen.");
                            return View(expenseDto); // Si el archivo no es válido, mostramos el formulario de nuevo con el error.
                        }
                    }

                    // Realizamos la solicitud a la API para registrar el gasto
                    var response = await _apiClient.PostAsync<CreateExpenseDTO, ApiResponse<object>>("api/Expense", expenseDto);

                    if (response.Success)
                    {
                        TempData["SuccessMessage"] = "El gasto se ha registrado correctamente.";
                        return RedirectToAction("Dashboard", "Manager"); // Redirige a la lista de partidas presupuestarias
                    }
                    else
                    {
                        // Si la API no responde correctamente, mostramos un mensaje de error
                        ModelState.AddModelError(string.Empty, response.Message ?? "No se pudo registrar el gasto.");
                    }
                }
                catch (Exception ex)
                {
                    // Manejo de errores generales
                    ModelState.AddModelError(string.Empty, $"Ocurrió un error al registrar el gasto: {ex.Message}");
                }
            }

            // Si el modelo no es válido o hay errores, mostramos el formulario nuevamente con los errores
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
