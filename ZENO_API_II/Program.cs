using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Models;
using ZENO_API_II.Data;
using ZENO_API_II.Services.Implementations;
using ZENO_API_II.Services.Interfaces;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ZenoDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer(); // <- isto ativa os metadados da API
builder.Services.AddSwaggerGen();           // <- isto gera a doc do Swagger

builder.Services.AddScoped<ICreditService, CreditService>();
builder.Services.AddScoped<ITokenEstimatorService, TokenEstimatorService>();


var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.MapOpenApi();
//}
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/")
    {
        context.Response.Redirect("/swagger");
        return;
    }
    await next();
});
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
