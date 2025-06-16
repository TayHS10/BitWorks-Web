using GPP_Web.DTOs.BudgetPart;
using GPP_Web.Models;
using GPP_Web.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GPP_Web.Controllers
{
    public class BudgetPartController : Controller
    {
        private readonly GenericApiClient _apiClient;

        public BudgetPartController(GenericApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        // Acción para la vista principal que lista las partidas presupuestarias de un proyecto específico
        public async Task<IActionResult> Index(int projectId)
        {
            try
            {
                // Realizar una solicitud GET a la API para obtener las partidas presupuestarias de un proyecto específico
                var response = await _apiClient.GetAsync<List<BudgetPartResponseDTO>>($"api/BudgetPart/project/{projectId}/budgetparts");

                if (response.Success && response.Data != null)
                {
                    return View(response.Data); // Pasa las partidas presupuestarias a la vista
                }
                else
                {
                    ViewBag.ErrorMessage = response.Message ?? "No se pudieron cargar las partidas presupuestarias.";
                    return View(new List<BudgetPartResponseDTO>());
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Ocurrió un error inesperado: {ex.Message}";
                return View(new List<BudgetPartResponseDTO>());
            }
        }

        // Acción para mostrar el formulario de transferencia de fondos entre partidas (SIN CAMBIOS AQUÍ)
        public async Task<IActionResult> TransferAmount(int budgetPartId)
        {
            var model = new TransferFundsDTO { SourceBudgetPartId = budgetPartId };

            try
            {
                var responseSource = await _apiClient.GetAsync<BudgetPartResponseDTO>($"api/BudgetPart/{budgetPartId}");

                if (responseSource.Success && responseSource.Data != null)
                {
                    model.SourceBudgetPartId = responseSource.Data.BudgetPartId;
                    int projectId = responseSource.Data.ProjectId;
                    ViewBag.SourceBudgetPart = responseSource.Data;

                    var responseAllParts = await _apiClient.GetAsync<List<BudgetPartResponseDTO>>("api/BudgetPart");

                    if (responseAllParts.Success && responseAllParts.Data != null)
                    {
                        var filteredParts = responseAllParts.Data
                            .Where(part => part.BudgetPartId != budgetPartId && part.ProjectId == projectId)
                            .ToList();
                        ViewBag.AllBudgetParts = filteredParts;
                        return View(model);
                    }
                    else
                    {
                        ViewBag.ErrorMessage = "No se pudieron cargar las partidas de destino.";
                        return View(model);
                    }
                }
                else
                {
                    ViewBag.ErrorMessage = "No se pudo cargar la partida origen.";
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Ocurrió un error inesperado: {ex.Message}";
                return View(model);
            }
        }

        // Acción para procesar la transferencia de fondos entre partidas
        [HttpPost]
        public async Task<IActionResult> TransferAmount(TransferFundsDTO transferFundsDto)
        {
            // ... (Tu código existente para obtener SourceBudgetPart y AllBudgetParts) ...
            var responseSource = await _apiClient.GetAsync<BudgetPartResponseDTO>($"api/BudgetPart/{transferFundsDto.SourceBudgetPartId}");
            BudgetPartResponseDTO sourceBudgetPart = null;

            if (responseSource.Success && responseSource.Data != null)
            {
                sourceBudgetPart = responseSource.Data;
                ViewBag.SourceBudgetPart = sourceBudgetPart;
            }
            else
            {
                ModelState.AddModelError(string.Empty, "No se pudo recuperar la partida de origen para validar el monto.");
            }

            var responseAllParts = await _apiClient.GetAsync<List<BudgetPartResponseDTO>>("api/BudgetPart");
            if (responseAllParts.Success && responseAllParts.Data != null && sourceBudgetPart != null)
            {
                var filteredParts = responseAllParts.Data
                    .Where(part => part.BudgetPartId != transferFundsDto.SourceBudgetPartId && part.ProjectId == sourceBudgetPart.ProjectId)
                    .ToList();
                ViewBag.AllBudgetParts = filteredParts;
            }

            if (ModelState.IsValid)
            {
                if (sourceBudgetPart != null && transferFundsDto.Amount > sourceBudgetPart.RemainingAmount)
                {
                    ModelState.AddModelError("Amount", $"El monto a transferir (₡{transferFundsDto.Amount:N0}) sobrepasa el monto restante disponible en la partida de origen (₡{sourceBudgetPart.RemainingAmount:N0}).");
                    return View(transferFundsDto);
                }

                try
                {
                    var response = await _apiClient.PostAsync<TransferFundsDTO, ApiResponse<object>>("api/BudgetPart/TransferFunds", transferFundsDto);

                    if (response.Success)
                    {
                        TempData["SuccessMessage"] = "Los fondos se han transferido correctamente.";
                        // **NUEVO:** Almacenar la URL de redirección en TempData
                        TempData["RedirectUrl"] = Url.Action("Dashboard", "Manager"); // Genera la URL para el Dashboard del Manager

                       
                        return RedirectToAction("Dashboard", "Manager");
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, response.Message ?? "Ocurrió un error al transferir los fondos.");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, $"Ocurrió un error al procesar la transferencia: {ex.Message}");
                }
            }

            return View(transferFundsDto);
        }
    }
}
