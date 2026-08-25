namespace ECommerce.Infrastructure.Abstractions;

public static class Permissions
{
    public const string Prefix = "Permissions.";

    public static class Products
    {
        public const string View = Prefix + "Products.View";
        public const string Create = Prefix + "Products.Create";
        public const string Update = Prefix + "Products.Update";
        public const string Delete = Prefix + "Products.Delete";
    }

    public static class Categories
    {
        public const string Manage = Prefix + "Categories.Manage";
    }

    public static class PromoCodes
    {
        public const string View = Prefix + "PromoCodes.View";
        public const string Create = Prefix + "PromoCodes.Create";
        public const string Update = Prefix + "PromoCodes.Update";
        public const string Delete = Prefix + "PromoCodes.Delete";
    }

    public static class StoreAddresses
    {
        public const string Manage = Prefix + "StoreAddresses.Manage";
    }

    public static class Orders
    {
        public const string View = Prefix + "Orders.View";
        public const string Update = Prefix + "Orders.Update";
    }

    public static class Returns
    {
        public const string View = Prefix + "Returns.View";
        public const string Manage = Prefix + "Returns.Manage";
    }

    public static class Users
    {
        public const string View = Prefix + "Users.View";
        public const string Manage = Prefix + "Users.Manage";
    }

    public static class Deliveries
    {
        public const string Handle = Prefix + "Deliveries.Handle";
    }

    public static class Stores
    {
        public const string View = Prefix + "Stores.View";
        public const string Manage = Prefix + "Stores.Manage";
    }

    public static class Customers
    {
        public const string View = Prefix + "Customers.View";
        public const string Manage = Prefix + "Customers.Manage";
    }

    public static class Sellers
    {
        public const string View = Prefix + "Sellers.View";
        public const string Manage = Prefix + "Sellers.Manage";
    }

    public static class Drivers
    {
        public const string View = Prefix + "Drivers.View";
        public const string Manage = Prefix + "Drivers.Manage";
    }

    public static class Admins
    {
        public const string View = Prefix + "Admins.View";
        public const string Manage = Prefix + "Admins.Manage";
    }

    public static readonly string[] All =
    [
        .. typeof(Permissions).GetNestedTypes()
            .Where(t => t.IsSealed && t.IsAbstract)
            .SelectMany(t => t.GetFields())
            .Where(f => f.IsStatic && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
    ];
}
