namespace DynamicFormBuilder.Models.Pagination
{

    public class PagedResult<T>
    {
        public int TotalCount { get; set; }
        public int FilteredCount { get; set; }
        public List<T> Data { get; set; } = new();
    }


}
