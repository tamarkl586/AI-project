using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace CatalogService.Models
{
    public class Category
    {
        [BsonId]
        [BsonRepresentation(BsonType.Int32)]
        public int Id { get; set; }

        [Required(ErrorMessage = "שדה חובה")]
        [MaxLength(50, ErrorMessage = "שם הקטגוריה לא יכול להכיל יותר מ-50 תווים")]
        public string Name { get; set; } = string.Empty;
    }
}
