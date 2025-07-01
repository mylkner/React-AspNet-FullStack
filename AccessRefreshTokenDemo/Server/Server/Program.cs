using Server.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddServices();
builder.Services.AddCustomServices();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUi(opt =>
    {
        opt.DocumentPath = "openapi/v1.json";
    });
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();
