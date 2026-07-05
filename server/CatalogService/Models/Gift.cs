using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace CatalogService.Models
{
    public class Gift
    {
        [BsonId]
        [BsonRepresentation(BsonType.Int32)]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = null!;

        [Required]
        public string Description { get; set; } = null!;

        [Required]
        public string Picture { get; set; } = null!;

        [Required]
        public int Price { get; set; }

        // Foreign key references (int IDs — no navigation properties)
        public int DonorId { get; set; }
        public int? CategoryId { get; set; }

        /// <summary>Reference to the winning User ID in the Identity Service. Assigned by the Drawing Service.</summary>
        public int? WinnerId { get; set; }

        // Denormalized for read performance — kept in sync on write
        public string DonorName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
    }
}
