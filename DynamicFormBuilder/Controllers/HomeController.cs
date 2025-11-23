using DynamicFormBuilder.Data.Service;
using DynamicFormBuilder.Models.Pagination;
using Microsoft.AspNetCore.Mvc;
namespace DynamicFormBuilder.Controllers
{
    public class HomeController : Controller
    {
        private readonly IFormRepository _formRepository;
        public HomeController(IFormRepository formRepository)
        {
            _formRepository = formRepository;
        }

        public IActionResult Index()
        {
           return View();
        }

        [HttpPost]
        public async Task<IActionResult> List(int draw, int start = 0, int length = 10)
        {            
            var req = new DataTableRequest
            {
                Skip = start,
                PageSize = length,
                Search = Request.Form["search[value]"].FirstOrDefault(),
                SortColumn = Request.Form["columns[" + Request.Form["order[0][column]"] + "][data]"].FirstOrDefault(),
                SortDirection = Request.Form["order[0][dir]"].FirstOrDefault()
            };
          
            var (data, total, filtered) = await _formRepository.GetFormsPagedAsync(req);
            
            return Json(new
            {
                draw = draw,
                recordsTotal = total,
                recordsFiltered = filtered,
                data = data
            });
        }

    }
}
