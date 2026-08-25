using ECommerce.Infrastructure.Abstractions;

namespace ECommerce.UnitTests.Abstractions;

public class PermissionsCatalogTests
{
    [Fact]
    public void All_contains_the_core_permission_constants()
    {
        var all = Permissions.All;

        Assert.Contains(Permissions.Products.View, all);
        Assert.Contains(Permissions.Products.Create, all);
        Assert.Contains(Permissions.Orders.Update, all);
        Assert.Contains(Permissions.Returns.Manage, all);
        Assert.Contains(Permissions.Users.View, all);
        Assert.Contains(Permissions.Deliveries.Handle, all);
    }

    [Fact]
    public void All_permissions_use_the_standard_prefix()
    {
        Assert.All(Permissions.All, p => Assert.StartsWith(Permissions.Prefix, p));
    }

    [Fact]
    public void All_permissions_are_distinct()
    {
        var all = Permissions.All;
        Assert.Equal(all.Length, all.Distinct().Count());
    }

    [Fact]
    public void Catalog_exposes_a_meaningful_number_of_permissions()
    {
        Assert.True(Permissions.All.Length >= 15, $"expected at least 15 permissions, found {Permissions.All.Length}");
    }
}

public class ResultTests
{
    private static readonly Error TestError = new("Test.Code", "description", 400);

    [Fact]
    public void Succeed_result_has_no_error()
    {
        var result = Result.Succeed();

        Assert.True(result.IsSucceed);
        Assert.False(result.IsFailure);
        Assert.Equal(Error.None, result.Error);
    }

    [Fact]
    public void Failure_result_carries_the_error()
    {
        var result = Result.Failure(TestError);

        Assert.True(result.IsFailure);
        Assert.Equal(TestError, result.Error);
    }

    [Fact]
    public void Generic_success_exposes_its_value()
    {
        var result = Result.Succeed(42);

        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Generic_failure_throws_when_value_is_accessed()
    {
        var result = Result.Failure<int>(TestError);

        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Success_with_an_error_is_invalid()
    {
        Assert.Throws<InvalidOperationException>(() => new Result(true, TestError));
    }

    [Fact]
    public void Failure_without_an_error_is_invalid()
    {
        Assert.Throws<InvalidOperationException>(() => new Result(false, Error.None));
    }
}
