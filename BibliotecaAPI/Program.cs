using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using AutoMapper;
using BibliotecaAPI.Datos;
using BibliotecaAPI.Entidades;
using BibliotecaAPI.Servicios;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Area de Servicios

var OrigenesPermitidos = builder.Configuration.GetSection("OrigenesPermitidos").Get<string[]>()!;

builder.Services.AddCors(opciones =>
{
    opciones.AddDefaultPolicy(opcionesCORS => 
    {
        opcionesCORS.WithOrigins(OrigenesPermitidos).AllowAnyMethod().AllowAnyHeader();
    });
});

builder.Services.AddAutoMapper(typeof(Program));

//builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

builder.Services.AddControllers().AddNewtonsoftJson();

builder.Services.AddDbContext<ApplicationDbContext>(opciones => opciones.UseSqlServer("name=DefaultConnection"));

//Authenticacion de usuarios
builder.Services.AddIdentityCore<Usuario>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<UserManager<Usuario>>();
builder.Services.AddScoped<SignInManager<Usuario>>();
builder.Services.AddTransient<IServicioUsuarios, ServicioUsuarios>();

builder.Services.AddTransient<IAlmacenadorArchivos, AlmacenadorArchivosLocal>();

builder.Services.AddHttpContextAccessor();

builder.Services.AddAuthentication().AddJwtBearer(opciones =>
{
    opciones.MapInboundClaims = false;

    opciones.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["llavejwt"]!)),
        ClockSkew = TimeSpan.Zero
    };

});

// Fin autenticacion de usuarios

builder.Services.AddAuthorization(opciones =>
{
    opciones.AddPolicy("esadmin", politica => politica.RequireClaim("esadmin"));
});


builder.Services.AddSwaggerGen();

var app = builder.Build();

// Area de middlewares

app.UseSwagger();
app.UseSwaggerUI();

app.UseStaticFiles();

app.UseCors();

app.MapControllers();


app.Run();
