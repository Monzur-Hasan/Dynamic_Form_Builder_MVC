using System;

namespace DynamicFormBuilder.Models
{
    public class FormDto
    {
        public int FormId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime? CreatedDate { get; set; }
        public List<FieldDto> Fields { get; set; } = new List<FieldDto>();
    }
}
