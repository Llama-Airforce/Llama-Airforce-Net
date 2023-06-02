using Llama.Airforce.API.Builder;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
        configurePolicy: builder =>
        {
            builder
                .WithOrigins(
                    "http://localhost:8080",
                    "https://www.llama.airforce",
                    "https://next.llama.airforce",
                    "https://llama.airforce",
                    "https://fixedforex.live", // dev: https://twitter.com/zashtoneth
                    "https://aura.defilytica.com") // dev: aura grantee
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});
builder.Services.AddControllers();
builder.Services.AddContexts();
builder.Services.AddMemoryCache();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.Run();
