namespace Customer.Profile.Contracts;

public record CreateAddressRequest(
    string Apartment,
    string Floor,
    string Building,
    string Street,
    string City,
    string State,
    string Country,
    string PostalCode);

public record CustomerAddressResponse(
    int Id,
    string Apartment,
    string Floor,
    string Building,
    string Street,
    string City,
    string State,
    string Country,
    string PostalCode);
