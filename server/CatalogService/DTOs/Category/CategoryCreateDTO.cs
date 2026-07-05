using System.ComponentModel.DataAnnotations;

namespace CatalogService.DTOs.Category
{
    public class CategoryCreateDTO
    {
        [Required(ErrorMessage = "שם קטגוריה הוא שדה חובה")]
        [MinLength(2, ErrorMessage = "שם קטגוריה חייב להכיל לפחות 2 תווים")]
        public string Name { get; set; } = string.Empty;
    }
}
