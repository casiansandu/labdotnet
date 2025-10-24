using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProductManagementt.Persistance;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddDbContext<ProductManagementContext>(options =>
    options.UseSqlite("Data Source=productmanagement.db"));
//builder.Services.AddScoped<CreateUserHandler>();
//builder.Services.AddScoped<GetAllUsersHandler>();
//builder.Services.AddScoped<DeleteUserHandler>();
//builder.Services.AddValidatorsFromAssemblyContaining<CreateUserValidator>();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();


app.Run();
