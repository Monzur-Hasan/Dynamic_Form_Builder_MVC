using DynamicFormBuilder.Models;
using DynamicFormBuilder.Models.Pagination;

public interface IOptionRepository
{
    Task<List<OptionSetDto>> GetOptionSetsAsync();
    Task<PagedResult<OptionSetDto>> GetPagedOptionSetsAsync(DataTableRequest req);

    Task<List<OptionDto>> GetOptionValuesAsync(int optionId);

    Task CreateOptionSetAsync(string name);

    Task<OptionSetDto?> GetOptionSetAsync(int id);

    Task<bool> UpdateOptionSetAsync(int id, string name);

    Task<bool> DeleteOptionSetAsync(int id);

    Task<bool> AddOptionValueAsync(int setId, string value);

    Task<bool> UpdateOptionValueAsync(int id, string value, int setId);

    Task<bool> DeleteOptionValueAsync(int id);
}
