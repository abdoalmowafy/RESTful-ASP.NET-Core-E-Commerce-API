namespace Ordering.Customer.Contracts;

public record CreateReturnRequest(int Quantity, string Reason, int AddressId);
