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

        // Acción para mostrar el formulario de transferencia de fondos entre partidas
        public async Task<IActionResult> TransferAmount(int budgetPartId)
        {
            try
            {
                // Obtener la partida de origen por su ID
                var responseSource = await _apiClient.GetAsync<BudgetPartResponseDTO>($"api/BudgetPart/{budgetPartId}");

                if (responseSource.Success && responseSource.Data != null)
                {
                    // Obtener el projectId de la partida origen
                    int projectId = responseSource.Data.ProjectId;

                    // Obtener todas las partidas de presupuesto
                    var responseAllParts = await _apiClient.GetAsync<List<BudgetPartResponseDTO>>("api/BudgetPart");

                    if (responseAllParts.Success && responseAllParts.Data != null)
                    {
                        // Filtrar las partidas de destino, excluyendo la partida de origen y solo mostrando las partidas del mismo proyecto
                        var filteredParts = responseAllParts.Data
                            .Where(part => part.BudgetPartId != budgetPartId && part.ProjectId == projectId)
                            .ToList();

                        // Asegúrate de que las variables no sean null antes de pasarlas a la vista
                        ViewBag.SourceBudgetPart = responseSource.Data;  // Partida de origen
                        ViewBag.AllBudgetParts = filteredParts;  // Partidas de destino disponibles

                        return View();
                    }
                    else
                    {
                        ViewBag.ErrorMessage = "No se pudieron cargar las partidas de destino.";
                        return View();
                    }
                }
                else
                {
                    ViewBag.ErrorMessage = "No se pudo cargar la partida origen.";
                    return View();
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Ocurrió un error inesperado: {ex.Message}";
                return View();
            }
        }

        // Acción para procesar la transferencia de fondos entre partidas
        [HttpPost]
        public async Task<IActionResult> TransferAmount(TransferFundsDTO transferFundsDto)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Realizamos la solicitud a la API para transferir los fondos entre las partidas
                    var response = await _apiClient.PostAsync<TransferFundsDTO, ApiResponse<object>>("api/BudgetPart/TransferFunds", transferFundsDto);

                    if (response.Success)
                    {
                        TempData["SuccessMessage"] = "Los fondos se han transferido correctamente.";
                        return RedirectToAction("Index"); // Redirige a la vista con las partidas
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
