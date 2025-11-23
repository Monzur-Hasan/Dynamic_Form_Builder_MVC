namespace DynamicFormBuilder.Models.Pagination
{
    public class DataTableRequest
    {
        public int Skip { get; set; }
        public int PageSize { get; set; }
        public string? Search { get; set; }
        public string? SortColumn { get; set; }
        public string? SortDirection { get; set; }
    }

}
