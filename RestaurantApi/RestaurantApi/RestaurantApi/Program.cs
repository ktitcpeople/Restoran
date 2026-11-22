using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RestaurantApi.Database;
using RestaurantApi.Hubs;
using RestaurantApi.Interface;
using RestaurantApi.Models;
using RestaurantApi.Service;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ContextDb>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("RestaurantApi")));

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ITableService, TableService>();
builder.Services.AddScoped<IMenuService, MenuService>();
builder.Services.AddScoped<IDeliveryService, DeliveryService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddHttpClient("YooKassa", client => client.BaseAddress = new Uri("https://api.yookassa.ru"));

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
    throw new InvalidOperationException("Jwt:Key должен быть задан и содержать не менее 32 символов.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.MapInboundClaims = false;
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            NameClaimType = "name",
            RoleClaimType = "role",
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        // SignalR при WebSocket/SSE передаёт JWT через query string.
        opt.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var token = context.Request.Query["access_token"].FirstOrDefault();
                if (!string.IsNullOrEmpty(token) && context.HttpContext.Request.Path.StartsWithSegments("/hubs/kitchen"))
                    context.Token = token;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();

builder.Services.AddCors(o =>
{
    o.AddPolicy("AllowBlazor", p =>
    {
        p.AllowAnyHeader().AllowAnyMethod().SetIsOriginAllowed(_ => true).AllowCredentials();
    });
});

builder.Services.AddSignalR();

builder.Services.AddControllers(o => o.Filters.Add<RestaurantApi.Attributes.ValidateInputFilter>()).ConfigureApiBehaviorOptions(o =>
{
    o.InvalidModelStateResponseFactory = context => new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(new
    {
        status = false,
        message = "Проверьте формат полей: числа, даты и обязательные значения.",
        errors = context.ModelState.Where(x => x.Value?.Errors.Count > 0).ToDictionary(x => x.Key, x => "Некорректное значение поля.")
    });
}).AddJsonOptions(o =>
{
    o.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Restaurant API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ContextDb>();
    db.Database.Migrate();

    // В исходном проекте таблица Roles создавалась миграцией, но начальные роли не добавлялись.
    var roles = new[]
    {
        new Role { RoleId = 1, Name = "Administrator" },
        new Role { RoleId = 2, Name = "Waiter" },
        new Role { RoleId = 3, Name = "Cook" },
        new Role { RoleId = 4, Name = "Guest" },
        new Role { RoleId = 5, Name = "Courier" },
        new Role { RoleId = 6, Name = "SupportOperator" },
        new Role { RoleId = 7, Name = "Supplier" }
    };

    foreach (var role in roles)
    {
        if (!db.Roles.Any(r => r.RoleId == role.RoleId))
            db.Roles.Add(role);
    }
    db.SaveChanges();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowBlazor");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<KitchenHub>("/hubs/kitchen");

app.Run();
