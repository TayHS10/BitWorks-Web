using GPP_Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Net.Http.Headers;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient("MyApiClient", client =>
{
    client.BaseAddress = new Uri("https://localhost:7197/");
    // Puedes a�adir configuraciones adicionales aqu�
});

// 2. Configuraci�n de la autenticaci�n por Cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login"; // Redirige aqu� si el usuario no est� autenticado
        options.LogoutPath = "/Auth/Logout"; // Aseg�rate de tener una acci�n Logout en AuthController
        options.AccessDeniedPath = "/Auth/AccessDenied"; // Opcional: Ruta para acceso denegado si usas [Authorize(Roles="...")]
        options.ExpireTimeSpan = TimeSpan.FromHours(1); // Duraci�n de la sesi�n
        options.SlidingExpiration = true; // Renueva la cookie si el usuario est� activo

        // --- �NUEVAS L�NEAS PARA MAPEAR CLAIMS! ---
        // Este evento se ejecuta cada vez que se valida un ClaimsPrincipal a partir de una cookie
        options.Events.OnValidatePrincipal = async context =>
        {
            var identity = context.Principal?.Identity as ClaimsIdentity;
            // Si la identidad no es nula y no tiene un claim de tipo Name (User.Identity.Name)
            if (identity != null && !identity.HasClaim(c => c.Type == ClaimTypes.Name))
            {
                // Busca el claim "unique_name" de tu JWT
                var uniqueNameClaim = identity.FindFirst("unique_name");
                if (uniqueNameClaim != null)
                {
                    // A�ade un nuevo claim de tipo Name con el valor de "unique_name"
                    identity.AddClaim(new Claim(ClaimTypes.Name, uniqueNameClaim.Value));
                }

                // Si la identidad no tiene un claim de tipo Role
                if (!identity.HasClaim(c => c.Type == ClaimTypes.Role))
                {
                    // Busca el claim "role" de tu JWT (el nombre tal cual est� en el JWT)
                    var roleClaim = identity.FindFirst("role");
                    if (roleClaim != null)
                    {
                        // A�ade un nuevo claim de tipo Role con el valor del claim "role"
                        identity.AddClaim(new Claim(ClaimTypes.Role, roleClaim.Value));
                    }
                }
            }
            // Los claims de rol (ClaimTypes.Role) generalmente se mapean autom�ticamente si est�n presentes
            // en el JWT y el IdentityHandler de JWT los entiende, pero puedes a�adir una verificaci�n similar
            // si notas problemas con los roles.
            await Task.CompletedTask; // Opcional: Asegura que el Task se complete si no haces nada as�ncrono
        };
        // --- FIN DE LAS NUEVAS L�NEAS ---
    });

// Registrar tu ApiClient general
// AddSingleton es una buena opci�n si GenericApiClient no mantiene estado espec�fico por solicitud.
builder.Services.AddScoped<GenericApiClient>(); // Cambiado a AddScoped

// Agregar servicios para la aplicaci�n (MVC, Razor Pages, etc.)
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configuraci�n del pipeline de la aplicaci�n
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // El valor por defecto de HSTS es de 30 d�as. Podr�as cambiarlo en escenarios de producci�n.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 3. Habilitar la autenticaci�n y autorizaci�n
app.UseAuthentication();
app.UseAuthorization();

// Mapea las rutas de los controladores
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
