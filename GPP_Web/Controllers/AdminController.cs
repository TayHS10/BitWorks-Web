using GPP_Web.DTOs.User;
using GPP_Web.Models;
using GPP_Web.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace GPP_Web.Controllers
{
    public class AdminController : Controller
    {
        private readonly GenericApiClient _apiClient;

        public AdminController(GenericApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        // Acción para obtener la lista de usuarios
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var response = await _apiClient.GetAsync<List<UserResponseDTO>>("api/User");

                if (response.Success && response.Data != null)
                {
                    return View(response.Data); // Pasa los usuarios a la vista
                }
                else
                {
                    ViewBag.ErrorMessage = response.Message ?? "No se pudieron cargar los usuarios.";
                    return View(new List<UserResponseDTO>());
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Ocurrió un error inesperado: {ex.Message}";
                return View(new List<UserResponseDTO>());
            }
        }

        // Acción para desactivar un usuario
        [HttpPost]
        public async Task<IActionResult> DeactivateUser(int userId)
        {
            try
            {
                // Llamada a la API para desactivar al usuario
                var response = await _apiClient.PutAsync<ApiResponse<object>>($"api/User/deactivate/{userId}", null);

                if (response.Success)
                {
                    TempData["SuccessMessage"] = "El usuario ha sido desactivado correctamente.";
                }
                else
                {
                    ViewBag.ErrorMessage = response.Message ?? "No se pudo desactivar el usuario.";
                }

                // Redirige a la lista de usuarios después de la desactivación
                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Ocurrió un error inesperado: {ex.Message}";
                return RedirectToAction("Dashboard");
            }
        }
    }
}
