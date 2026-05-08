# .NET Library Development Skill

Dùng khi build .NET class library, NuGet packaging, hoặc library-specific tasks.

## Project Structure

### Create Solution & Projects

```bash
# Create solution
dotnet new sln -n DainnUser

# Create class library projects
dotnet new classlib -n DainnUser.Core -o src/DainnUser.Core
dotnet new classlib -n DainnUser.Infrastructure -o src/DainnUser.Infrastructure
dotnet new classlib -n DainnUser.Application -o src/DainnUser.Application
dotnet new classlib -n DainnUser.Api -o src/DainnUser.Api
dotnet new classlib -n DainnUser.Web -o src/DainnUser.Web

# Create test projects
dotnet new xunit -n DainnUser.UnitTests -o tests/DainnUser.UnitTests
dotnet new xunit -n DainnUser.IntegrationTests -o tests/DainnUser.IntegrationTests
dotnet new xunit -n DainnUser.SecurityTests -o tests/DainnUser.SecurityTests

# Add projects to solution
dotnet sln add src/**/*.csproj
dotnet sln add tests/**/*.csproj

# Add project references
dotnet add src/DainnUser.Infrastructure/DainnUser.Infrastructure.csproj reference src/DainnUser.Core/DainnUser.Core.csproj
dotnet add src/DainnUser.Application/DainnUser.Application.csproj reference src/DainnUser.Core/DainnUser.Core.csproj
dotnet add src/DainnUser.Application/DainnUser.Application.csproj reference src/DainnUser.Infrastructure/DainnUser.Infrastructure.csproj
dotnet add src/DainnUser.Api/DainnUser.Api.csproj reference src/DainnUser.Application/DainnUser.Application.csproj
dotnet add src/DainnUser.Web/DainnUser.Web.csproj reference src/DainnUser.Application/DainnUser.Application.csproj
```

## .csproj Configuration

### Library Project Settings

```xml
<PropertyGroup>
  <TargetFramework>net8.0</TargetFramework>
  <Nullable>enable</Nullable>
  <ImplicitUsings>enable</ImplicitUsings>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <NoWarn>$(NoWarn);1591</NoWarn> <!-- Suppress XML doc warnings -->
  
  <!-- NuGet Package Metadata -->
  <PackageId>DainnUser.Core</PackageId>
  <Version>1.0.0</Version>
  <Authors>Your Name</Authors>
  <Company>Your Company</Company>
  <Description>Core domain models and interfaces for DainnUser library</Description>
  <PackageTags>authentication;authorization;user-management;dotnet</PackageTags>
  <PackageLicenseExpression>MIT</PackageLicenseExpression>
  <PackageProjectUrl>https://github.com/yourorg/dainnuser</PackageProjectUrl>
  <RepositoryUrl>https://github.com/yourorg/dainnuser</RepositoryUrl>
  <RepositoryType>git</RepositoryType>
  <PackageReadmeFile>README.md</PackageReadmeFile>
  <PackageIcon>icon.png</PackageIcon>
</PropertyGroup>

<ItemGroup>
  <None Include="..\..\README.md" Pack="true" PackagePath="\" />
  <None Include="..\..\icon.png" Pack="true" PackagePath="\" />
</ItemGroup>
```

## Build & Pack

```bash
# Build solution
dotnet build

# Build specific project
dotnet build src/DainnUser.Core/

# Build in Release mode
dotnet build -c Release

# Create NuGet package
dotnet pack -c Release -o nupkgs/

# Create package for specific project
dotnet pack src/DainnUser.Core/ -c Release -o nupkgs/

# Pack with version
dotnet pack -c Release -o nupkgs/ /p:Version=1.0.0
```

## Local Testing

```bash
# Add local NuGet source
dotnet nuget add source ./nupkgs -n LocalPackages

# Install from local source in test project
dotnet add package DainnUser.Core --source ./nupkgs

# Remove local source
dotnet nuget remove source LocalPackages
```

## Publish to NuGet

```bash
# Publish to NuGet.org
dotnet nuget push nupkgs/DainnUser.Core.1.0.0.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json

# Publish all packages
dotnet nuget push nupkgs/*.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json --skip-duplicate
```

## Library Design Best Practices

### Public API Surface

- **Minimize public surface:** Chỉ expose những gì cần thiết
- **Use interfaces:** Cho testability và extensibility
- **XML documentation:** Tất cả public members
- **Semantic versioning:** Major.Minor.Patch
- **Avoid breaking changes:** Deprecate trước khi remove

### Dependency Management

- **Minimize dependencies:** Mỗi dependency là burden cho consumers
- **Use framework libraries:** Prefer Microsoft.Extensions.* over third-party
- **Target lowest framework:** .NET Standard 2.0 nếu cần compatibility, .NET 8.0 nếu modern-only
- **Avoid version conflicts:** Use common versions

### Configuration

- **Options pattern:** IOptions<T> cho configuration
- **Validate on startup:** Fail fast nếu misconfigured
- **Sensible defaults:** Library hoạt động out-of-the-box
- **Override via appsettings:** Developers có thể customize

### Extension Methods

```csharp
// Service registration extension
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDainnUser(
        this IServiceCollection services,
        Action<DainnUserOptions>? configure = null)
    {
        // Register services
        services.AddScoped<IUserService, UserService>();
        
        // Configure options
        if (configure != null)
        {
            services.Configure(configure);
        }
        
        return services;
    }
}
```

### Async All The Way

```csharp
// Good
public async Task<User> GetUserAsync(int id, CancellationToken cancellationToken = default)
{
    return await _repository.GetByIdAsync(id, cancellationToken);
}

// Bad - blocking
public User GetUser(int id)
{
    return _repository.GetByIdAsync(id).Result; // NEVER DO THIS
}
```

## Sample Projects

Luôn tạo sample projects để demonstrate usage:

```bash
# Create sample web API
dotnet new webapi -n WebApiSample -o samples/WebApiSample
dotnet add samples/WebApiSample reference src/DainnUser.Api/

# Create sample MVC app
dotnet new mvc -n MvcSample -o samples/MvcSample
dotnet add samples/MvcSample reference src/DainnUser.Web/
```

## Documentation

Tạo docs cho library:
- `docs/getting-started.md` — Quick start guide
- `docs/configuration.md` — Configuration options
- `docs/api-reference.md` — API documentation
- `docs/migration-guide.md` — Version migration guides
- `docs/security.md` — Security best practices

## Checklist Before Release

- [ ] All tests passing
- [ ] XML documentation complete
- [ ] README.md updated
- [ ] CHANGELOG.md updated
- [ ] Sample projects working
- [ ] NuGet metadata correct
- [ ] License file included
- [ ] Security review done
- [ ] Breaking changes documented
- [ ] Version number bumped correctly
