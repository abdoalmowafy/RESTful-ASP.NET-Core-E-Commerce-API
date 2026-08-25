FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY ECommerce.Infrastructure/ECommerce.Infrastructure.csproj ECommerce.Infrastructure/
COPY ECommerce.Authentication/ECommerce.Authentication.csproj ECommerce.Authentication/
COPY Catalog.Public/Catalog.Public.csproj Catalog.Public/
COPY Catalog.Management/Catalog.Management.csproj Catalog.Management/
COPY Shopping.Customer/Shopping.Customer.csproj Shopping.Customer/
COPY Ordering.Customer/Ordering.Customer.csproj Ordering.Customer/
COPY Ordering.Management/Ordering.Management.csproj Ordering.Management/
COPY Admin.Management/Admin.Management.csproj Admin.Management/
COPY Admin.Profile/Admin.Profile.csproj Admin.Profile/
COPY Customer.Management/Customer.Management.csproj Customer.Management/
COPY Customer.Profile/Customer.Profile.csproj Customer.Profile/
COPY Seller.Management/Seller.Management.csproj Seller.Management/
COPY Seller.Profile/Seller.Profile.csproj Seller.Profile/
COPY Driver.Management/Driver.Management.csproj Driver.Management/
COPY Driver.Profile/Driver.Profile.csproj Driver.Profile/
COPY Roles.Management/Roles.Management.csproj Roles.Management/
COPY Notifications/Notifications.csproj Notifications/
COPY Store.API/Store.API.csproj Store.API/

RUN dotnet restore Store.API/Store.API.csproj

COPY . .
RUN dotnet publish Store.API/Store.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Store.API.dll"]
