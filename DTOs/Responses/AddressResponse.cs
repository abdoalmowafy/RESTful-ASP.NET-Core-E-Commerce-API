﻿namespace ECommerceAPI.DTOs.Responses
{
    public class AddressResponse
    {
        public int? Id { get; set; }
        public string? Apartment { get; set; }
        public string? Floor { get; set; }
        public string? Building { get; set; }
        public string? Street { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        public string? PostalCode { get; set; }
    }
}
