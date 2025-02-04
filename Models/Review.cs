﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerceAPI.Models
{
    public class Review
    {
        [Key] public int Id { get; set; }
        [Required] public required User Reviewer { get; set; }
        [Required] public int ProductId { get; set; }
        [ForeignKey("ProductId")] public required Product Product { get; set; }
        [Required][Range(1,5)] public byte Rating { get; set; }
        [DataType(DataType.MultilineText)] public required string Text { get; set; }
        [Required][DataType(DataType.DateTime)] public DateTime CreatedDateTime { get; set; } = DateTime.Now;
        [DataType(DataType.DateTime)] public DateTime? DeletedDateTime { get; set; }
        public ICollection<EditHistory> EditsHistory { get; set; } = [];
    }
}
