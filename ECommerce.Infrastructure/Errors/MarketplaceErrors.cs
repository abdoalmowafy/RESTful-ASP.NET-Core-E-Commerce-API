namespace ECommerce.Infrastructure.Errors;

public static class MarketplaceErrors
{
    public static class Store
    {
        public static readonly Error NotFound = Error.NotFound("Store.NotFound", "Store not found");
        public static readonly Error NotOwned = Error.Forbidden("Store.NotOwned", "You do not own this store");
        public static readonly Error NameDuplicated = Error.Conflict("Store.NameDuplicated", "A store with a similar name already exists");
        public static readonly Error NotActive = Error.Forbidden("Store.NotActive", "Your store is not active. It must be approved before selling");
        public static readonly Error AlreadyExists = Error.Conflict("Store.AlreadyExists", "You already own a store");
    }

    public static class DriverProfile
    {
        public static readonly Error NotFound = Error.NotFound("Driver.NotFound", "Driver profile not found");
        public static readonly Error AlreadyApplied = Error.Conflict("Driver.AlreadyApplied", "A driver application already exists");
        public static readonly Error NotEditable = Error.BadRequest("Driver.NotEditable", "Only rejected profiles can be resubmitted");
    }

    public static class Profiles
    {
        public static readonly Error CustomerNotFound = Error.NotFound("Customer.NotFound", "Customer profile not found");
        public static readonly Error AdminNotFound = Error.NotFound("Admin.NotFound", "Admin profile not found");
    }
}
