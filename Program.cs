var ForgeClientID = Environment.GetEnvironmentVariable("FORGE_CLIENT_ID");
var ForgeClientSecret = Environment.GetEnvironmentVariable("FORGE_CLIENT_SECRET");
var ForgeBucket = Environment.GetEnvironmentVariable("FORGE_BUCKET"); // Optional
if (string.IsNullOrEmpty(ForgeClientID) || string.IsNullOrEmpty(ForgeClientSecret))
{
    Console.Error.WriteLine("Missing required environment variables FORGE_CLIENT_ID or FORGE_CLIENT_SECRET.");
    return;
}

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ForgeService>(new ForgeService(ForgeClientID, ForgeClientSecret, ForgeBucket));

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapGet("/api/auth/token", (HttpContext context, ForgeService forgeService) => forgeService.GetPublicToken());
app.MapGet("/api/models", (HttpContext context, ForgeService forgeService) => forgeService.GetObjects());
app.MapPost("/api/models", async (HttpContext context, ForgeService forgeService) =>
{
    var file = context.Request.Form.Files["model-file"];
    var entrypoint = context.Request.Form["model-zip-entrypoint"];
    var tmpFilePath = Path.GetTempFileName();
    using (var stream = new FileStream(tmpFilePath, FileMode.OpenOrCreate))
    {
        await file.CopyToAsync(stream);
    }
    using (var stream = File.OpenRead(tmpFilePath))
    {
        dynamic obj = await forgeService.UploadModel(file.FileName, stream, file.Length);
        await forgeService.TranslateModel(obj.objectId, entrypoint);
    }
    File.Delete(tmpFilePath);
});

app.Run();
