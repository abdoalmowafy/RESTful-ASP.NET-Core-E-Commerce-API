namespace ECommerce.Infrastructure.Abstractions;

public static class DefaultUsers
{
    public const string SuperAdminEmail = "superadmin@store.com";
    public const string SuperAdminPassword = "SuperAdmin@123!";
    public const string AdminEmail = "admin@store.com";
    public const string AdminPassword = "Admin@123!";
    public const string SellerEmail = "seller@store.com";
    public const string SellerPassword = "Seller@123!";
    public const string DriverEmail = "driver@store.com";
    public const string DriverPassword = "Driver@123!";
    public const string CustomerEmail = "customer@store.com";
    public const string CustomerPassword = "Customer@123!";
}

public static class DefaultRoles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";
    public const string Customer = "Customer";
    public const string Seller = "Seller";
    public const string Driver = "Driver";
}

public static class RegexPatterns
{
    public const string Sku = @"^[A-Z0-9]{2,10}-[0-9]{4}$";
}
