using AggregatorService;
using AggregatorService.Filters;
using AggregatorService.Mappers;
using AggregatorService.Options;
using AggregatorService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.RateLimiting;
using static Pvs.Auth.Grpc.AuthService;
using static Pvs.Media.Grpc.MediaService;
using static Pvs.Content.Grpc.ContentService;
using static Pvs.Content.Grpc.CardService;
using static Pvs.Content.Grpc.AnalyticsService;
using static Pvs.Content.Grpc.StudyService;
using static Pvs.Content.Grpc.CommunityService;
using static Pvs.Content.Grpc.SubscriptionService;
using static Pvs.Content.Grpc.TextService;
using static Pvs.Content.Grpc.TermService;
using GrpcAgentServiceClient = Pvs.Agent.Grpc.AgentService.AgentServiceClient;

// Allow gRPC over plain HTTP (HTTP/2 without TLS) when talking to authorization-module and vocabulary-service
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpClient();

var adresses = builder.Configuration.GetSection("AggregatorService");
var corsOriginsStr = builder.Configuration["Cors:AllowedOrigins"];
var corsOrigins = string.IsNullOrWhiteSpace(corsOriginsStr)
    ? new[] { "http://localhost:3000" }
    : corsOriginsStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
ValidateAggregatorConfiguration(builder.Configuration, builder.Environment, corsOrigins);
// Отдельный SocketsHttpHandler без прокси — стабильный HTTP/2 (h2c) к 127.0.0.1 / сервисам в Docker
builder.Services.AddGrpcClient<AuthServiceClient>(x => x.Address = new Uri(adresses["AuthorizationModuleBaseUrl"]!))
    .ConfigurePrimaryHttpMessageHandler(GrpcClientConfiguration.CreateSocketsHandler);
builder.Services.AddGrpcClient<MediaServiceClient>(x => x.Address = new Uri(adresses["MediaServiceBaseUrl"]!))
    .ConfigurePrimaryHttpMessageHandler(GrpcClientConfiguration.CreateSocketsHandler)
    .ConfigureChannel(o => { o.MaxSendMessageSize = 1000 * 1024 * 1024; o.MaxReceiveMessageSize = 1000 * 1024 * 1024; });
builder.Services.AddGrpcClient<ContentServiceClient>(x => x.Address = new Uri(adresses["VocabularyServiceBaseUrl"]!))
    .ConfigurePrimaryHttpMessageHandler(GrpcClientConfiguration.CreateSocketsHandler);
builder.Services.AddGrpcClient<CardServiceClient>(x => x.Address = new Uri(adresses["VocabularyServiceBaseUrl"]!))
    .ConfigurePrimaryHttpMessageHandler(GrpcClientConfiguration.CreateSocketsHandler);
builder.Services.AddGrpcClient<AnalyticsServiceClient>(x => x.Address = new Uri(adresses["VocabularyServiceBaseUrl"]!))
    .ConfigurePrimaryHttpMessageHandler(GrpcClientConfiguration.CreateSocketsHandler);
builder.Services.AddGrpcClient<StudyServiceClient>(x => x.Address = new Uri(adresses["VocabularyServiceBaseUrl"]!))
    .ConfigurePrimaryHttpMessageHandler(GrpcClientConfiguration.CreateSocketsHandler);
builder.Services.AddGrpcClient<CommunityServiceClient>(x => x.Address = new Uri(adresses["VocabularyServiceBaseUrl"]!))
    .ConfigurePrimaryHttpMessageHandler(GrpcClientConfiguration.CreateSocketsHandler);
builder.Services.AddGrpcClient<SubscriptionServiceClient>(x => x.Address = new Uri(adresses["VocabularyServiceBaseUrl"]!))
    .ConfigurePrimaryHttpMessageHandler(GrpcClientConfiguration.CreateSocketsHandler);
builder.Services.AddGrpcClient<TextServiceClient>(x => x.Address = new Uri(adresses["VocabularyServiceBaseUrl"]!))
    .ConfigurePrimaryHttpMessageHandler(GrpcClientConfiguration.CreateSocketsHandler);
builder.Services.AddGrpcClient<TermServiceClient>(x => x.Address = new Uri(adresses["VocabularyServiceBaseUrl"]!))
    .ConfigurePrimaryHttpMessageHandler(GrpcClientConfiguration.CreateSocketsHandler);
builder.Services.AddGrpcClient<Pvs.Content.Grpc.LessonService.LessonServiceClient>(x => x.Address = new Uri(adresses["VocabularyServiceBaseUrl"]!))
    .ConfigurePrimaryHttpMessageHandler(GrpcClientConfiguration.CreateSocketsHandler);
builder.Services.AddGrpcClient<GrpcAgentServiceClient>(x => x.Address = new Uri(adresses["AgentServiceBaseUrl"]!))
    .ConfigurePrimaryHttpMessageHandler(GrpcClientConfiguration.CreateSocketsHandler);
builder.Services.AddGrpcClient<Pvs.Billing.Grpc.BillingService.BillingServiceClient>(x => x.Address = new Uri(adresses["BillingServiceBaseUrl"]!))
    .ConfigurePrimaryHttpMessageHandler(GrpcClientConfiguration.CreateSocketsHandler);

builder.Services.AddSingleton<IBillingServiceClient, BillingServiceClient>();

// Настройка Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Polyraspad Aggregator API", 
        Version = "v1",
        Description = "API для агрегации запросов к микросервисам Polyraspad",
        Contact = new OpenApiContact
        {
            Name = "Polyraspad Team"
        }
    });
    
    // Настройка JWT аутентификации в Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.\n\nExample: \"Bearer 12345abcdef\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });
    
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            Array.Empty<string>()
        }
    });
    
    // Используем фильтры для автоматического добавления требований безопасности к методам
    options.OperationFilter<SecurityRequirementsOperationFilter>();
});

builder.Services.Configure<AggregatorServiceOptions>(
    builder.Configuration.GetSection("AggregatorService"));

builder.Services.Configure<FeaturesOptions>(builder.Configuration.GetSection("Features"));

builder.Services.Configure<BillingOptions>(builder.Configuration.GetSection("Billing"));

builder.Services.Configure<IntegrationOptions>(builder.Configuration.GetSection("Integration"));

builder.Services.Configure<AiCompletionOptions>(builder.Configuration.GetSection("Ai"));
builder.Services.AddHttpClient<OpenAiChatCompletionClient>((sp, client) =>
{
    var o = sp.GetRequiredService<IOptions<AiCompletionOptions>>().Value;
    var baseUrl = (o.BaseUrl ?? "https://api.openai.com/v1").TrimEnd('/');
    client.BaseAddress = new Uri(baseUrl + "/");
    client.Timeout = TimeSpan.FromSeconds(Math.Clamp(o.TimeoutSeconds, 5, 600));
    client.DefaultRequestHeaders.Remove("Authorization");
    if (!string.IsNullOrWhiteSpace(o.ApiKey))
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", o.ApiKey.Trim());
});
builder.Services.AddScoped<IStudyCopilotFeedbackService, OpenAiCompatibleStudyCopilotFeedbackService>();
builder.Services.AddScoped<AiProxyApiKeyFilter>();
builder.Services.AddHttpClient<OpenAiSpeechClient>((sp, client) =>
{
    var o = sp.GetRequiredService<IOptions<AiCompletionOptions>>().Value;
    var baseUrl = (o.BaseUrl ?? "https://api.openai.com/v1").TrimEnd('/');
    client.BaseAddress = new Uri(baseUrl + "/");
    client.Timeout = TimeSpan.FromSeconds(Math.Clamp(o.TimeoutSeconds, 5, 600));
    client.DefaultRequestHeaders.Remove("Authorization");
    if (!string.IsNullOrWhiteSpace(o.ApiKey))
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", o.ApiKey.Trim());
});
builder.Services.AddScoped<ITtsAudioService, TtsAudioService>();

// Регистрация gRPC клиента для VocabularyService
builder.Services.AddSingleton<IVocabularyServiceClient, VocabularyServiceClient>();
builder.Services.AddSingleton<IAgentServiceClient, AgentServiceClient>();
builder.Services.AddSingleton<IAutomationJobOrchestrator, InMemoryAutomationJobOrchestrator>();

// Регистрация gRPC клиента для authorization-module
builder.Services.AddSingleton<IAuthorizationServiceClient, AuthorizationServiceClient>();

// Регистрация gRPC клиента для media-service
builder.Services.AddSingleton<IMediaServiceClient, MediaServiceClientImpl>();

// Регистрация gRPC клиента для OCR сервиса (EasyOCR on CPU can take minutes on multi-page PDFs)
builder.Services.AddGrpcClient<Ocr.OcrService.OcrServiceClient>(options =>
{
    options.Address = new Uri(builder.Configuration["Ocr:GrpcAddress"] ?? "http://localhost:50052");
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    var handler = GrpcClientConfiguration.CreateSocketsHandler();
    handler.PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan;
    handler.KeepAlivePingDelay = TimeSpan.FromSeconds(60);
    handler.KeepAlivePingTimeout = TimeSpan.FromSeconds(30);
    return handler;
});
builder.Services.AddSingleton<IOcrService, OcrGrpcService>();

builder.Services.AddSingleton<IDocumentTextExtractor, DocumentTextExtractor>();

// Регистрация AutoMapper
builder.Services.AddAutoMapper(cfg => { }, typeof(AutoMappingProfile));

// Настройка CORS: для credentialed запросов (serve-image с Bearer) нужен конкретный origin, не *
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(corsOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Настройка JWT аутентификации (интеграция с authorization-module)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsync(
            "{\"error\":\"Too many requests. Please try again later.\"}",
            cancellationToken);
    };
    options.AddPolicy("auth-public", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: GetRateLimitPartitionKey(httpContext),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration.GetValue<string>("Jwt:Issuer"),
        ValidAudience = builder.Configuration.GetValue<string>("Jwt:Audience"),
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration.GetValue<string>("Jwt:Secret")!))
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Настройка Swagger UI
    app.UseSwagger(c =>
    {
        c.RouteTemplate = "swagger/{documentname}/swagger.json";
    });
    
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Polyraspad Aggregator API v1");
        c.RoutePrefix = "swagger";
        c.DisplayRequestDuration();
        c.EnableDeepLinking();
        c.EnableFilter();
        c.ShowExtensions();
    });
}

app.UseHttpsRedirection();

app.UseCors();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));
app.MapControllers();

app.Run();

void ValidateAggregatorConfiguration(
    IConfiguration configuration,
    IWebHostEnvironment environment,
    string[] allowedOrigins)
{
    if (environment.IsDevelopment())
    {
        return;
    }

    var errors = new List<string>();

    ValidateJwtConfiguration(configuration, errors);
    ValidateCorsConfiguration(allowedOrigins, errors);
    ValidateServiceUrl(configuration["AggregatorService:VocabularyServiceBaseUrl"], "AggregatorService:VocabularyServiceBaseUrl", errors);
    ValidateServiceUrl(configuration["AggregatorService:AuthorizationModuleBaseUrl"], "AggregatorService:AuthorizationModuleBaseUrl", errors);
    ValidateServiceUrl(configuration["AggregatorService:MediaServiceBaseUrl"], "AggregatorService:MediaServiceBaseUrl", errors);

    var aiProxyApiKey = configuration["Ai:ProxyApiKey"];
    if (!string.IsNullOrWhiteSpace(aiProxyApiKey) &&
        aiProxyApiKey.Equals("dev-ai-proxy-shared-secret", StringComparison.OrdinalIgnoreCase))
    {
        errors.Add("Ai:ProxyApiKey cannot use the shared development default in non-development environments.");
    }

    if (errors.Count > 0)
    {
        throw new InvalidOperationException(
            "AggregatorService is missing required production configuration:" +
            $"{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", errors)}");
    }
}

void ValidateJwtConfiguration(IConfiguration configuration, List<string> errors)
{
    var jwtSecret = configuration["Jwt:Secret"];
    if (string.IsNullOrWhiteSpace(jwtSecret) ||
        jwtSecret.Length < 32 ||
        LooksLikePlaceholder(jwtSecret))
    {
        errors.Add("Jwt:Secret must be set to a non-placeholder value with at least 32 characters.");
    }

    if (string.IsNullOrWhiteSpace(configuration["Jwt:Issuer"]) || LooksLikePlaceholder(configuration["Jwt:Issuer"]!))
    {
        errors.Add("Jwt:Issuer must be configured.");
    }

    if (string.IsNullOrWhiteSpace(configuration["Jwt:Audience"]) || LooksLikePlaceholder(configuration["Jwt:Audience"]!))
    {
        errors.Add("Jwt:Audience must be configured.");
    }
}

void ValidateCorsConfiguration(string[] allowedOrigins, List<string> errors)
{
    if (allowedOrigins.Length == 0)
    {
        errors.Add("Cors:AllowedOrigins must contain at least one origin.");
        return;
    }

    if (allowedOrigins.Any(origin => origin == "*"))
    {
        errors.Add("Cors:AllowedOrigins cannot contain '*'.");
    }
}

void ValidateServiceUrl(string? url, string key, List<string> errors)
{
    if (string.IsNullOrWhiteSpace(url) ||
        !Uri.TryCreate(url, UriKind.Absolute, out var parsedUri) ||
        (parsedUri.Scheme != Uri.UriSchemeHttp && parsedUri.Scheme != Uri.UriSchemeHttps))
    {
        errors.Add($"{key} must be a valid absolute HTTP or HTTPS URL.");
    }
}

string GetRateLimitPartitionKey(HttpContext httpContext)
{
    var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].ToString();
    if (!string.IsNullOrWhiteSpace(forwardedFor))
    {
        return forwardedFor.Split(',')[0].Trim();
    }

    return httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}

bool LooksLikePlaceholder(string value) =>
    value.Contains("change-me", StringComparison.OrdinalIgnoreCase) ||
    value.Contains("example", StringComparison.OrdinalIgnoreCase) ||
    value.Contains("yourdomain", StringComparison.OrdinalIgnoreCase) ||
    value.Contains("yoursecretkeyhere", StringComparison.OrdinalIgnoreCase);

// Expose for WebApplicationFactory<Program> in tests
public partial class Program { }
