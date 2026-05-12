using Microsoft.AspNetCore.Mvc;

namespace MvcSample.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
