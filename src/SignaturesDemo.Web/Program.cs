using SignaturesDemo.Web.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddHubOptions(o =>
    {
        // JS interop payloads carry the base64 of the signed PDF (~150–500 KB
        // typical). The default SignalR limit of 32 KB kills the circuit and
        // throws away the signature result, so we raise it to 10 MB.
        o.MaximumReceiveMessageSize = 10 * 1024 * 1024;
    });

// Typed HttpClient pointing at the SignaturesDemo API. The base URL is read
// from configuration so we don't hardcode dev ports in code.
builder.Services.AddHttpClient("Api", (sp, client) =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var baseUrl = cfg["Api:BaseUrl"] ?? "https://localhost:7001";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
