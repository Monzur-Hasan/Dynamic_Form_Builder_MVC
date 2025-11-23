using DynamicFormBuilder.Models;
using DynamicFormBuilder.Models.Pagination;

public interface IFormRepository
{
    Task<bool> IsTitleExistsAsync(string title);
    Task<int> SaveFormAsync(string title, List<FieldDto> fields);
    Task<(List<FormDto> Data, int RecordsTotal, int RecordsFiltered)> GetFormsPagedAsync(DataTableRequest req);
    Task<FormDto?> GetFormWithFieldsAsync(int formId);
    Task<bool> UpdateFormAsync(FormDto form);
    Task<bool> DeleteFormAsync(int formId);
}

