# API Reference

This section contains the auto-generated API reference documentation for DainnUser. The documentation is generated from XML comments in the source code and provides detailed information about all public types, methods, properties, and events.

## How to Read API Documentation

The API reference is organized by namespace. Each namespace contains related types (classes, interfaces, enums, etc.) that work together to provide specific functionality.

### Understanding the Structure

- **Namespaces**: Logical groupings of related types
- **Classes**: Implementation types that provide functionality
- **Interfaces**: Contracts that define behavior
- **Enums**: Named constants for type-safe values
- **Methods**: Operations that can be performed
- **Properties**: Data members that can be read or written

### Code Examples

Most API members include code examples showing common usage patterns. These examples are designed to help you understand how to use the API in real-world scenarios.

## Main Namespaces

### DainnUser.Core

The core domain layer containing entities, interfaces, and domain logic. This namespace defines the fundamental building blocks of the library.

**Key Types:**
- Domain entities (User, Role, UserToken, etc.)
- Repository interfaces
- Domain exceptions
- Enums and constants

[Browse DainnUser.Core API →](DainnUser.Core.yml)

### DainnUser.Application

The application layer containing business logic, services, DTOs, and validators. This is where the main functionality is implemented.

**Key Types:**
- Service interfaces and implementations
- Data Transfer Objects (DTOs)
- FluentValidation validators
- Application-level exceptions

[Browse DainnUser.Application API →](DainnUser.Application.yml)

### DainnUser.Infrastructure

The infrastructure layer containing Entity Framework Core implementations, external service integrations, and data access logic.

**Key Types:**
- DbContext and entity configurations
- Repository implementations
- Email and storage providers
- JWT token services

[Browse DainnUser.Infrastructure API →](DainnUser.Infrastructure.yml)

### DainnUser.Api

The API layer containing controllers, middleware, filters, and extension methods for ASP.NET Core integration.

**Key Types:**
- API controllers
- Custom middleware
- Action filters
- Service registration extensions

[Browse DainnUser.Api API →](DainnUser.Api.yml)

### DainnUser.Web

The web layer containing Razor components, tag helpers, and view models for web UI integration.

**Key Types:**
- Razor/Blazor components
- Tag helpers
- View models
- UI utilities

[Browse DainnUser.Web API →](DainnUser.Web.yml)

## Additional Resources

- [Getting Started Guide](../articles/getting-started.md)
- [Configuration Reference](../articles/configuration.md)
- [Code Examples](../articles/code-examples.md)
- [Architecture Overview](../articles/architecture.md)
