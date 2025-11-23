using DynamicFormBuilder.Data;
using DynamicFormBuilder.Models.Pagination;
using Microsoft.AspNetCore.Mvc;

namespace DynamicFormBuilder.Controllers
{
    public class OptionSetController : Controller
    {
        private readonly IOptionRepository _optionRepository;
        public OptionSetController(IOptionRepository optionRepository)
        {
            _optionRepository = optionRepository;
        }

        // List of option sets
        public async Task<IActionResult> Index()
        {
            var sets = await _optionRepository.GetOptionSetsAsync();
            return View(sets);
        }

        [HttpPost]
        public async Task<IActionResult> LoadData()
        {
            var req = new DataTableRequest
            {
                Skip = Convert.ToInt32(Request.Form["start"]),
                PageSize = Convert.ToInt32(Request.Form["length"]),
                Search = Request.Form["search[value]"],
                SortColumn = Request.Form["columns[" + Request.Form["order[0][column]"] + "][data]"],
                SortDirection = Request.Form["order[0][dir]"]
            };

            var result = await _optionRepository.GetPagedOptionSetsAsync(req);

            return Json(new
            {
                draw = Request.Form["draw"],
                recordsTotal = result.TotalCount,
                recordsFiltered = result.FilteredCount,
                data = result.Data
            });
        }

        // Create page
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest("Option Set name is required.");

            try
            {
                await _optionRepository.CreateOptionSetAsync(name);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var set = await _optionRepository.GetOptionSetAsync(id);
            if (set == null) return NotFound();
            return View(set);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int optionId, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest("Option Set name is required.");

            try
            {
                bool isUpdated = await _optionRepository.UpdateOptionSetAsync(optionId, name);
                if (isUpdated)
                    return RedirectToAction("Index");

                return BadRequest("Update failed.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            bool isDeleted = await _optionRepository.DeleteOptionSetAsync(id);
            return isDeleted ? RedirectToAction("Index") : Ok();
        }

        // Option VALUES
        [HttpGet]
        public async Task<IActionResult> Values(int id)
        {
            var set = await _optionRepository.GetOptionSetAsync(id);
            if (set == null) return NotFound();

            ViewBag.SetName = set.Name;
            ViewBag.SetId = set.OptionId;

            var values = await _optionRepository.GetOptionValuesAsync(id);
            return View(values);
        }

        [HttpPost]
        public async Task<IActionResult> AddValue(int setId, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return BadRequest("Value is required.");

            try
            {
                bool isAdd = await _optionRepository.AddOptionValueAsync(setId, value);
                if (isAdd)
                    return RedirectToAction("Values", new { id = setId });

                return BadRequest("Save failed.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> EditValue(int id, int setId, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return BadRequest("Value is required.");

            try
            {
                bool updated = await _optionRepository.UpdateOptionValueAsync(id, value, setId);
                if (updated)
                    return RedirectToAction("Values", new { id = setId });

                return BadRequest("Update failed.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteValue(int id, int setId)
        {
            await _optionRepository.DeleteOptionValueAsync(id);
            return RedirectToAction("Values", new { id = setId });
        }
    }
}
