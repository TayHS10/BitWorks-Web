using GPP_Web.DTOs.User;
using GPP_Web.Models;
using Newtonsoft.Json;
using System.Text.Json;
using JsonException = System.Text.Json.JsonException;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace GPP_Web.Services
{
    public class GenericApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonSerializerOptions;

        public GenericApiClient(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("MyApiClient");
            _jsonSerializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true }; // Para manejar camelCase/PascalCase
        }

      
        public async Task<ApiResponse<T>> GetAsync<T>(string endpoint)
        {
            var response = await _httpClient.GetAsync(endpoint);

            response.EnsureSuccessStatusCode(); // Lanza una excepción para códigos de estado 4xx o 5xx

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<T>>();

            // Puedes añadir lógica adicional aquí para verificar apiResponse.Success
            // if (apiResponse == null || !apiResponse.Success)
            // {
            //     // Manejar casos donde la respuesta no es lo esperado o Success es false
            //     throw new InvalidOperationException($"La API devolvió un error para el endpoint {endpoint}: {apiResponse?.Message}");
            // }

            return apiResponse;
        }

        
        public async Task<ApiResponse<TResponseData>> PostAsync<TRequest, TResponseData>(string endpoint, TRequest data)
        {
            var response = await _httpClient.PostAsJsonAsync(endpoint, data);

            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<TResponseData>>();

            return apiResponse;
        }


        //public async Task<AuthResponse> AuthenticateAsync(string endpoint, LoginUserDTO loginRequest)
        //{
        //    try
        //    {
        //        var response = await _httpClient.PostAsJsonAsync(endpoint, loginRequest, _jsonSerializerOptions);

        //        var responseBody = await response.Content.ReadAsStringAsync();

        //        // Aquí deserializamos directamente al AuthResponseDTO, que NO es un ApiResponse<T> genérico
        //        var authApiResponse = JsonSerializer.Deserialize<AuthResponse>(responseBody, _jsonSerializerOptions);

        //        // Puedes añadir aquí una verificación de código de estado HTTP si lo deseas,
        //        // antes de devolver el authApiResponse
        //        if (!response.IsSuccessStatusCode && authApiResponse == null)
        //        {
        //            throw new HttpRequestException($"La autenticación a {endpoint} falló con el código de estado {response.StatusCode} y no se pudo deserializar la respuesta de error.");
        //        }
        //        if (!response.IsSuccessStatusCode)
        //        {
        //            var errorResponseBody = await response.Content.ReadAsStringAsync();
        //            Console.WriteLine($"Error HTTP StatusCode: {response.StatusCode}");
        //            Console.WriteLine($"Error Raw Response Content: {errorResponseBody}");
        //            // ...
        //        }
        //        else
        //        {
        //            var responseBody = await response.Content.ReadAsStringAsync();
        //            Console.WriteLine($"Raw Response Content (Success): {responseBody}");
        //        }

        //        return authApiResponse ?? new AuthResponse { Result = false, Msj = "Respuesta de autenticación vacía o nula." };
        //    }
        //    catch (HttpRequestException ex)
        //    {
        //        throw new HttpRequestException($"Error de conexión o HTTP al intentar autenticar: {ex.Message}", ex);
        //    }
        //    catch (JsonException ex)
        //    {
        //        throw new JsonException($"Error al procesar la respuesta de autenticación: {ex.Message}", ex);
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception($"Error inesperado durante la autenticación: {ex.Message}", ex);
        //    }
        //}

        public async Task<AuthResponse> AuthenticateAsync(string endpoint, LoginUserDTO loginRequest)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(endpoint, loginRequest, _jsonSerializerOptions);

                // Leemos el cuerpo de la respuesta UNA SOLA VEZ aquí.
                var responseBody = await response.Content.ReadAsStringAsync();

                // --- PUNTOS CRÍTICOS DE DEPURACIÓN ---
                // Estas líneas te ayudarán a ver qué está pasando en la consola.
                Console.WriteLine($"HTTP StatusCode: {response.StatusCode}");
                Console.WriteLine($"Raw Response Content: {responseBody}");
                // --- FIN PUNTOS CRÍTICOS ---

                if (!response.IsSuccessStatusCode)
                {
                    // Si el StatusCode no es exitoso, manejamos el error.
                    // Intentamos deserializar el cuerpo como AuthResponse incluso si es un error,
                    // por si la API envía un JSON de error en ese formato.
                    try
                    {
                        var errorAuthResponse = JsonSerializer.Deserialize<AuthResponse>(responseBody, _jsonSerializerOptions);
                        if (errorAuthResponse != null && !string.IsNullOrEmpty(errorAuthResponse.Msj))
                        {
                            return errorAuthResponse; // Devolvemos el mensaje de error de la API si lo hay.
                        }
                    }
                    catch (JsonException)
                    {
                        // Si no se puede deserializar como AuthResponse, significa que el cuerpo
                        // de la respuesta de error no es JSON o tiene un formato diferente.
                    }

                    // Devolvemos un AuthResponse genérico para errores HTTP no JSON.
                    return new AuthResponse
                    {
                        Result = false,
                        Msj = $"La autenticación falló con el código de estado HTTP: {response.StatusCode}. Contenido de la respuesta: {responseBody.Substring(0, Math.Min(responseBody.Length, 200))}..." // Muestra una parte del contenido
                    };
                }

                // Si llegamos aquí, el StatusCode fue exitoso (2xx).
                // Ahora deserializamos el cuerpo de la respuesta esperada.
                var authApiResponse = JsonSerializer.Deserialize<AuthResponse>(responseBody, _jsonSerializerOptions);

                return authApiResponse ?? new AuthResponse { Result = false, Msj = "Respuesta de autenticación vacía o nula." };
            }
            catch (HttpRequestException ex)
            {
                throw new HttpRequestException($"Error de conexión o HTTP al intentar autenticar: {ex.Message}", ex);
            }
            catch (JsonException ex)
            {
                // Esta excepción se lanzará si 'responseBody' no era JSON válido,
                // incluso después de pasar las verificaciones de StatusCode.
                // Esto es lo que queremos depurar con los Console.WriteLine.
                throw new JsonException($"Error al procesar la respuesta de autenticación: El contenido no es JSON válido. Detalles: {ex.Message}. Contenido recibido: [Ver salida de consola]", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error inesperado durante la autenticación: {ex.Message}", ex);
            }
        }
    }
}
