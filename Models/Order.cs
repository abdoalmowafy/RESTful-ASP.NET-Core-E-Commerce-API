﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ECommerceAPI.Models
{
    public class Order
    {
        [Key] public int Id { get; set; }
        [Required] public required string UserId { get; set; }
        [ForeignKey("UserId")] public required User User { get; set; }
        public string? TransporterId { get; set; }
        [ForeignKey("TransporterId")] public User? Transporter { get; set; }
        [Required] public ICollection<OrderProduct> OrderProducts { get; set; } = [];
        public PromoCode? PromoCode { get; set; }
        [Required][Range(0, long.MaxValue)] public long TotalCents { get; set; }
        [Required] public string Currency { get; set; } = "EGP";
        [Required] public PaymentMethod PaymentMethod { get; set; } 
        [Required] public bool DeliveryNeeded { get; set; } = false;
        [Required] public OrderStatus Status { get; set; }
        public int? PaymobOrderId { get; set; }
        [Required] public required Address Address { get; set; }
        [Required][DataType(DataType.DateTime)] public DateTime CreatedDateTime { get; set; } = DateTime.Now;
        [DataType(DataType.DateTime)] public DateTime? DeliveryDateTime { get; set; }
        [DataType(DataType.DateTime)] public DateTime? DeletedDateTime { get; set; }
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PaymentMethod
    {
        COD,
        CreditCard,
        MobileWallet
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum OrderStatus
    {
        Paying,
        Processing,
        OnTheWay,
        Delivered,
        Deleted
    }
}
