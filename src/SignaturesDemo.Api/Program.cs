using Microsoft.AspNetCore.Http.Features;
using QuestPDF.Infrastructure;
using SignaturesDemo.Api.Services;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 50_000_000;
});

builder.Services.AddSingleton<SignatureFieldInjector>();
builder.Services.AddSingleton<SamplePdfGenerator>();
builder.Services.AddSingleton<SignatureValidator>();

const string WebCorsPolicy = "WebCorsPolicy";
builder.Services.AddCors(o => o.AddPolicy(WebCorsPolicy, p => p
    .WithOrigins("https://localhost:7002", "http://localhost:5002")
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(WebCorsPolicy);
app.MapControllers();

app.Run();
