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
COPY Ordering.Transporter/Ordering.Transporter.csproj Ordering.Transporter/
COPY Users.Management/Users.Management.csproj Users.Management/
COPY Store.API/Store.API.csproj Store.API/

RUN dotnet restore Store.API/Store.API.csproj

COPY . .
RUN dotnet publish Store.API/Store.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Store.API.dll"]
