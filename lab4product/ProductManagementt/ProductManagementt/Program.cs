using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProductManagementt.Features.Products;
using ProductManagementt.Persistance;
using ProductManagementt.Validators;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddDbContext<ProductManagementContext>(options =>
    options.UseSqlite("Data Source=productmanagement.db"));
builder.Services.AddScoped<CreateProductHandler>();
builder.Services.AddScoped<GetAllProductsHandler>();
builder.Services.AddScoped<DeleteProductHandler>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateProductValidator>();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc
    (
        "v1",
        new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "Product Management API",
            Version = "v1",
            Description = "API for managing products",
            Contact = new Microsoft.OpenApi.Models.OpenApiContact
            {
                Name = "API support",
                Email = "support@example.com"
            }
        });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ProductManagementContext>();
    context.Database.EnsureCreated();
}


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Product Management API v1");
        c.RoutePrefix = string.Empty;
        c.DisplayRequestDuration();
    });
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("/products", async (CreateProductRequest req, CreateProductHandler handler) =>
    await handler.Handle(req));
app.MapGet("/products", async (GetAllProductsHandler handler) =>
    await handler.Handle(new GetAllProductsRequest()));
app.MapDelete("/products/{id:guid}", async (Guid id, DeleteProductHandler handler) =>
{
    await handler.Handle(new DeleteProductRequest(id));
});

app.Run();
