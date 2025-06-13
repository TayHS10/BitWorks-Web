using Microsoft.AspNetCore.Mvc;

namespace GPP_Web.Controllers
{
    public class ManagerController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
