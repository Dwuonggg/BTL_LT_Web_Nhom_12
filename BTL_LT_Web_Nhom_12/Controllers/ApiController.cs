using Microsoft.AspNetCore.Mvc;

namespace BTL_LT_Web_Nhom_12.Controllers
{
    public class ApiController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}