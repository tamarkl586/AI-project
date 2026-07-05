using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace CatalogService.Models
{
    public class Donor
    {
        [BsonId]
        [BsonRepresentation(BsonType.Int32)]
        public int Id { get; set; }

        [Required]
        [StringLength(9)]
        [RegularExpression(@"^\d{9}$", ErrorMessage = "תעודת זהות חייבת להכיל 9 ספרות")]
        public string IdentityNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "שדה חובה")]
        [MaxLength(50, ErrorMessage = "שם לא יכול להכיל יותר מ-50 תווים")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "שדה חובה")]
        [Phone(ErrorMessage = "מספר טלפון לא תקין")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "שדה חובה")]
        [EmailAddress(ErrorMessage = "כתובת אימייל לא תקינה")]
        public string Email { get; set; } = string.Empty;
    }
}
