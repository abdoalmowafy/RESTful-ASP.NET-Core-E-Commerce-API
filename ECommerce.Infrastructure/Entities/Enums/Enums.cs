using System.Text.Json.Serialization;

namespace ECommerce.Infrastructure.Entities.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Gender
{
    Male,
    Female
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
    Cancelled
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReturnStatus
{
    Processing,
    OnTheWay,
    Returned,
    Cancelled
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StoreStatus
{
    PendingVerification,
    Active,
    Suspended,
    Rejected
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RegistrationStatus
{
    PendingVerification,
    Active,
    Rejected
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DriverStatus
{
    PendingVerification,
    Active,
    Suspended,
    Rejected
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum VehicleType
{
    Motorcycle,
    Car,
    Van,
    Bicycle
}

public enum OtpPurpose
{
    EmailVerification,
    PhoneVerification,
    PasswordReset
}

public enum AppOwnerType
{
    Customer,
    Seller,
    Driver,
    Admin
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DevicePlatform
{
    Android,
    Ios,
    Web
}
