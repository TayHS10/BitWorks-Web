using Microsoft.AspNetCore.Mvc;

namespace GPP_Web.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
