namespace DynamicFormBuilder.Models
{
    public class FieldDto
    {
        public int FieldId { get; set; }
        public int FormId { get; set; }
        public string Label { get; set; } = string.Empty;
        public bool IsRequired { get; set; }

        // OptionSet chosen for this field
        public int OptionId { get; set; }

        // Selected OptionValueId
        public int? SelectedOptionValueId { get; set; }

        // Option value text (optional for preview if needed)
        public string? SelectedOption { get; set; }
    }
}
