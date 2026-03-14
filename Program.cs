using System.Text;
using HRsystem.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowAll",
        builder =>
        {
            builder
                .AllowAnyOrigin() // Barcha manbalarga ruxsat
                .AllowAnyMethod() // Barcha metodlarga (GET, POST, va hokazo) ruxsat
                .AllowAnyHeader(); // Barcha header’larga ruxsat
        }
    );
});

// AutoMapper ni qo‘shish
builder.Services.AddAutoMapper(typeof(Program).Assembly);

//var port = Environment.GetEnvironmentVariable("PORT") ?? "5000"; // Heroku uchun kerak
//builder.WebHost.UseUrls($"http://*:{port}");

// Swagger/OpenAPI ni sozlash
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc(
        "v1",
        new OpenApiInfo
        {
            Title = "HRsystem API",
            Version = "v1",
            Description = "HRsystem loyihasi uchun API hujjatlari",
        }
    );

    c.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            In = ParameterLocation.Header,
            Description = "JWT token’ni Bearer sifatida kiriting (masalan: Bearer {token})",
            Name = "Authorization",
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer",
        }
    );

    c.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer",
                    },
                },
                new string[] { }
            },
        }
    );
});

// Autentifikatsiya va avtorizatsiya
builder.Services.AddAuthorization();
builder
    .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme) // "Bearer" scheme
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"], // "HRSystem"
            ValidAudience = builder.Configuration["Jwt:Audience"], // "HRSystemUsers"
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
            ), // "Bu_Secret_Key_32_Characters_Long!"
        };
    });

var app = builder.Build();

// Configure the HTTP request pipeline

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "HRsystem API v1");
        c.RoutePrefix = string.Empty;
    });

app.UseCors("AllowAll");
app.UseDefaultFiles();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseStaticFiles();
app.MapFallbackToFile("index.html");
app.MapControllers();
app.Run();
