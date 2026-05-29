# ASP.NET Agent Instructions

## System Architecture
- Backend: ASP.NET Core Web API (.NET 8/10)
- Database: Entity Framework Core (SQL Server / PostgreSQL)

## Working Agreements & Tooling
- Always run `dotnet build` to verify syntax before declaring a task done.
- Run tests via `dotnet test` from the root directory.
- Use `dotnet add package <PackageName>` for dependency additions.

## Naming & Style Conventions
- C# Code: Follow standard Microsoft C# PascalCase guidelines.
- Dependency Injection: Always inject services via Primary Constructors or Controller Constructors; do not instantiate directly.
- Async Patterns: Always append `Async` to asynchronous methods (e.g., `GetByIdAsync`) and enforce `await`.
- Database: Keep Entity Configurations separate from Models using `IEntityTypeConfiguration`.

## Feature Implementation Guidance
- When adding controllers, services, or endpoints, check `Program.cs` for required dependency injection registrations and existing middleware configuration.
- Register services with the appropriate lifetime, such as `builder.Services.AddScoped<IExampleService, ExampleService>()`.
- For file upload endpoints, prefer secure `IFormFile` handling and avoid trusting client-supplied file names or content types without validation.
- For DTOs, request models, validators, mappings, and controllers, follow existing project structure and standard `ActionResult` patterns.
- Before starting a task, check existing models, services, controllers, views, and tests so duplicate work is not created.

## Entity Framework Workflow
- Create migrations with `dotnet ef migrations add <MigrationName>`.
- Apply local migrations with `dotnet ef database update`.
- Keep migrations focused on the feature being implemented.
- When a task requires EF Core migration commands or database updates, request/confirm the needed local execution permissions instead of stopping at code changes.

## Scaffolding Workflow
- For repetitive ASP.NET boilerplate, generate DTOs, request models, validators, mappings, controllers, Razor views, and tests in the existing project structure.
- Keep API/controller actions on standard ASP.NET `ActionResult` or `IActionResult` patterns.
- When multiple independent areas are requested together, split work by ownership area and avoid editing the same files in parallel.

## Codex Prompting Notes
- Anchor feature prompts with relevant files, for example: `@Program.cs @Controllers/UserController.cs`.
- For scaffolding requests, include the source model and desired outputs, for example: `@Models/Product.cs ProductDto CreateProductRequest ProductsController`.
- When changing dependency injection, include `@Program.cs` in the task context so service registrations and middleware remain aligned.
- In Visual Studio workflows, expect files to reload externally after Codex writes them; in VS Code workflows, review generated diffs before accepting broad changes.
