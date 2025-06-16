using GPP_Web.DTOs.User;
using GPP_Web.Models;
using GPP_Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
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
                    // Si la autenticación falla, verificamos si el mensaje es "Credenciales inválidas"
                    if (authApiResponse?.Msj.Contains("Credenciales inválidas") == true)
                    {
                        ModelState.AddModelError(string.Empty, "Credenciales inválidas. Por favor, verifique su usuario y contraseña.");
                        ViewBag.ErrorMessage = "Credenciales inválidas. Por favor, intente de nuevo.";
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, authApiResponse?.Msj ?? "Error al intentar iniciar sesión.");
                    }
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

        // Método para mostrar la vista de restablecimiento de contraseña
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // Método que maneja el formulario de restablecimiento de contraseña
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequestDTO forgotPasswordRequest)
        {
            if (!ModelState.IsValid)
            {
                return View(forgotPasswordRequest);
            }

            try
            {
                // Llamar a la API para procesar la solicitud de restablecimiento de contraseña
                var response = await _apiClient.PostAsync<ForgotPasswordRequestDTO, ApiResponse<string>>("Auth/forgot-password-request", forgotPasswordRequest);

                if (response.Success)
                {
                    ViewBag.SuccessMessage = "Se ha enviado un correo para restablecer tu contraseña.";
                }
                else
                {
                    ModelState.AddModelError(string.Empty, response.Message ?? "Error al intentar enviar el correo de restablecimiento.");
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Ocurrió un error inesperado: {ex.Message}. Por favor, intenta de nuevo.");
            }

            return View(forgotPasswordRequest);
        }

        [HttpGet]
        public IActionResult ResetPasswordConfirm(string email, string token)
        {
            // Verificamos que el email y el token no estén vacíos
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Login", "Auth");
            }

            // Pasamos el email y el token a la vista
            ViewData["Email"] = email;
            ViewData["Token"] = token;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPasswordConfirm(ResetPasswordConfirmDTO request)
        {
            if (!ModelState.IsValid)
            {
                return View(request);
            }

            try
            {
                // Verificamos si las contraseñas coinciden
                if (request.NewPassword != request.ConfirmNewPassword)
                {
                    ModelState.AddModelError(string.Empty, "Las contraseñas no coinciden.");
                    return View(request);
                }

                // Llamar a la API para confirmar el restablecimiento de la contraseña
                var response = await _apiClient.PostAsync<ResetPasswordConfirmDTO, ApiResponse<string>>("Auth/reset-password-confirm", request);

                if (response.Success)
                {
                    // Pasar mensaje de éxito a la vista
                    ViewBag.SuccessMessage = "Contraseña restablecida con éxito.";
                }
                else
                {
                    // Pasar mensaje de error a la vista
                    ModelState.AddModelError(string.Empty, response.Message ?? "Error al intentar restablecer la contraseña.");
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Ocurrió un error inesperado: {ex.Message}. Por favor, intenta de nuevo.");
            }

            return View(request);
        }


    }
}