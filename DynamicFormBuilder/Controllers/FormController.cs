using DynamicFormBuilder.Models;
using Microsoft.AspNetCore.Mvc;

public class FormController : Controller
{
    private readonly IFormRepository _formRepo;
    private readonly IOptionRepository _optionRepo;

    public FormController(IFormRepository formRepo, IOptionRepository optionRepo)
    {
        _formRepo = formRepo;
        _optionRepo = optionRepo;
    }

    public IActionResult Create() => View();

    [HttpPost]
    [Route("api/form/save")]
    public async Task<IActionResult> Save([FromBody] FormDto model)
    {
        if (string.IsNullOrWhiteSpace(model.Title))
            return BadRequest("Title is required.");

        if (await _formRepo.IsTitleExistsAsync(model.Title))
            return Conflict("A form with this title already exists.");

        foreach (var f in model.Fields)
            if (string.IsNullOrWhiteSpace(f.Label))
                return BadRequest("Each field must have a label.");

        var id = await _formRepo.SaveFormAsync(model.Title, model.Fields);
        return Ok(new { formId = id });
    }

    [HttpGet]
    public async Task<IActionResult> Preview(int id)
    {
        var form = await _formRepo.GetFormWithFieldsAsync(id);
        return form == null ? NotFound() : View(form);
    }

    [HttpGet("api/optionsets")]
    public async Task<IActionResult> GetOptionSets()
    {
        var optionSets = await _optionRepo.GetOptionSetsAsync();
        return Ok(optionSets);
    }

    [HttpGet("api/options/{optionId}")]
    public async Task<IActionResult> GetOptionValues(int optionId)
    {
        var optionValues = await _optionRepo.GetOptionValuesAsync(optionId);
        return Ok(optionValues);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var form = await _formRepo.GetFormWithFieldsAsync(id);
        return form == null ? NotFound() : View(form);
    }

    [HttpPost("api/form/update")]
    public async Task<IActionResult> Update([FromBody] FormDto model)
    {
        if (string.IsNullOrWhiteSpace(model.Title))
            return BadRequest("Title is required.");

        bool ok = await _formRepo.UpdateFormAsync(model);
        return ok ? Ok() : BadRequest("Update failed.");
    }

    [HttpDelete("api/form/delete/{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        bool ok = await _formRepo.DeleteFormAsync(id);
        return ok ? Ok() : BadRequest("Delete failed.");
    }
}
