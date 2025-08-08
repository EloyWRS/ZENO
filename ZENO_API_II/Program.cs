using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Models;
using ZENO_API_II.Data;
using ZENO_API_II.Services;
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
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ZENO_API_II", Version = "v1" });
    
    // Add JWT Authentication
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// Authentication Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtService, JwtService>();

// Existing Services
builder.Services.AddScoped<ICreditService, CreditService>();
builder.Services.AddScoped<ITokenEstimatorService, TokenEstimatorService>();
builder.Services.AddScoped<IOpenAITextToSpeechService, OpenAITextToSpeechService>();
builder.Services.AddScoped<IAssistantMessageService, AssistantMessageService>();
builder.Services.AddScoped<IAudioTranscriptionService, AudioTranscriptionService>();
builder.Services.AddScoped<IAssistantService, AssistantService>();

// User Setup Service
builder.Services.AddScoped<IUserSetupService, UserSetupService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder
                .AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

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
app.UseCors("AllowAll");

app.MapControllers();

app.Run();
