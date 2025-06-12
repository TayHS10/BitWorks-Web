using GPP_Web.DTOs.User;
using GPP_Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json; 

namespace GPP_Web.Controllers
{
    public class AuthController : Controller
    {
        private readonly GenericApiClient _apiClient;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="AuthController"/>.
        /// </summary>
        /// <param name="apiClient">El cliente API genérico utilizado para realizar solicitudes de autenticación a servicios externos.</param>
        public AuthController(GenericApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        /// <summary>
        /// Devuelve la vista de inicio de sesión.
        /// </summary>
        /// <returns>Un <see cref="IActionResult"/> que representa la vista de login.</returns>
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        /// <summary>
        /// Maneja la solicitud de inicio de sesión, autenticando al usuario con las credenciales proporcionadas.
        /// </summary>
        /// <param name="loginRequest">El objeto que contiene las credenciales del usuario para el inicio de sesión.</param>
        /// <returns>Una tarea que representa la operación asíncrona y devuelve un <see cref="IActionResult"/> que representa la vista de inicio de sesión con errores de validación o redirige a la página principal si la autenticación es exitosa.</returns>
        /// <remarks>Si la autenticación falla, se muestran mensajes de error basados en la respuesta de la API o en excepciones de conexión.</remarks>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginUserDTO loginRequest)
        {
            if (!ModelState.IsValid)
            {
                return View(loginRequest);
            }

            try
            {
                var authApiResponse = await _apiClient.AuthenticateAsync("Auth/authenticate", loginRequest);

                if (authApiResponse != null && authApiResponse.Result && !string.IsNullOrEmpty(authApiResponse.Token))
                {
                    var handler = new JwtSecurityTokenHandler();
                    var jwtToken = handler.ReadJwtToken(authApiResponse.Token);

                    var identity = new ClaimsIdentity(jwtToken.Claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, authApiResponse?.Msj ?? "Error al intentar iniciar sesión. Credenciales incorrectas.");
                }
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, $"Error de comunicación con el servidor: {ex.Message}. Por favor, inténtalo más tarde.");
            }
            catch (JsonException ex)
            {
                ModelState.AddModelError(string.Empty, $"Error al procesar la respuesta del servidor: {ex.Message}.");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Ocurrió un error inesperado: {ex.Message}. Por favor, inténtalo de nuevo.");
            }

            return View(loginRequest);
        }

        /// <summary>
        /// Maneja la solicitud de cierre de sesión, desconectando al usuario de la aplicación.
        /// </summary>
        /// <returns>Una tarea que representa la operación asíncrona y devuelve un <see cref="IActionResult"/> que redirige a la vista de inicio de sesión.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Auth");
        }
    }
}