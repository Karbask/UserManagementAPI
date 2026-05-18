using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Swagger para documentar y probar la API durante el desarrollo
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Pipeline de la aplicación
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Se deja comentado para facilitar las pruebas locales con HTTP.
// app.UseHttpsRedirection();


// 1. Middleware global de gestión de errores
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (BadHttpRequestException ex)
    {
        app.Logger.LogWarning(ex, "La petición recibida no es válida.");

        if (!context.Response.HasStarted)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(new
            {
                error = "Bad request.",
                message = "La petición enviada no es válida. Revisa el formato de los datos."
            });
        }
    }
    catch (JsonException ex)
    {
        app.Logger.LogWarning(ex, "Error al leer el JSON recibido.");

        if (!context.Response.HasStarted)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(new
            {
                error = "Invalid JSON.",
                message = "El cuerpo de la petición no tiene un formato JSON válido."
            });
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Se produjo un error no controlado.");

        if (!context.Response.HasStarted)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(new
            {
                error = "Internal server error.",
                message = "Se produjo un error inesperado al procesar la solicitud."
            });
        }
    }
});


// 2. Middleware de autenticación basada en token
app.Use(async (context, next) =>
{
    bool isApiRequest = context.Request.Path.StartsWithSegments("/api");

    if (!isApiRequest)
    {
        await next();
        return;
    }

    string? authorizationHeader = context.Request.Headers.Authorization;

    if (string.IsNullOrWhiteSpace(authorizationHeader))
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";

        app.Logger.LogWarning(
            "Unauthorized request: {Method} {Path} - Missing token",
            context.Request.Method,
            context.Request.Path
        );

        await context.Response.WriteAsJsonAsync(new
        {
            error = "Unauthorized.",
            message = "No se ha enviado un token de autorización."
        });

        return;
    }

    const string bearerPrefix = "Bearer ";
    const string validToken = "techhive-token-123";

    if (!authorizationHeader.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";

        app.Logger.LogWarning(
            "Unauthorized request: {Method} {Path} - Invalid authorization format",
            context.Request.Method,
            context.Request.Path
        );

        await context.Response.WriteAsJsonAsync(new
        {
            error = "Unauthorized.",
            message = "El formato del token no es válido."
        });

        return;
    }

    string token = authorizationHeader[bearerPrefix.Length..].Trim();

    if (token != validToken)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";

        app.Logger.LogWarning(
            "Unauthorized request: {Method} {Path} - Invalid token",
            context.Request.Method,
            context.Request.Path
        );

        await context.Response.WriteAsJsonAsync(new
        {
            error = "Unauthorized.",
            message = "El token enviado no es válido."
        });

        return;
    }

    await next();
});


// 3. Middleware de registro de solicitudes y respuestas
app.Use(async (context, next) =>
{
    var stopwatch = Stopwatch.StartNew();

    string method = context.Request.Method;
    string path = context.Request.Path;

    app.Logger.LogInformation(
        "Incoming request: {Method} {Path}",
        method,
        path
    );

    await next();

    stopwatch.Stop();

    app.Logger.LogInformation(
        "Outgoing response: {Method} {Path} - Status Code: {StatusCode} - Time: {ElapsedMilliseconds} ms",
        method,
        path,
        context.Response.StatusCode,
        stopwatch.ElapsedMilliseconds
    );
});


// Datos en memoria.
// Para esta actividad no se usa base de datos, por lo que los datos se pierden al reiniciar la API.
Dictionary<int, User> users = new()
{
    {
        1,
        new User
        {
            Id = 1,
            Name = "Ana García",
            Email = "ana.garcia@techhive.com",
            Department = "RRHH",
            Role = "HR Manager"
        }
    },
    {
        2,
        new User
        {
            Id = 2,
            Name = "Carlos López",
            Email = "carlos.lopez@techhive.com",
            Department = "IT",
            Role = "System Administrator"
        }
    }
};

int nextId = 3;


// GET: Obtener todos los usuarios
app.MapGet("/api/users", (int skip = 0, int take = 50) =>
{
    if (skip < 0)
    {
        return Results.BadRequest("El valor de skip no puede ser negativo.");
    }

    if (take <= 0 || take > 100)
    {
        return Results.BadRequest("El valor de take debe estar entre 1 y 100.");
    }

    var result = users.Values
        .OrderBy(user => user.Id)
        .Skip(skip)
        .Take(take)
        .ToList();

    return Results.Ok(result);
});


// GET: Obtener un usuario por ID
app.MapGet("/api/users/{id:int}", (int id) =>
{
    if (!users.TryGetValue(id, out var user))
    {
        return Results.NotFound($"No se encontró ningún usuario con el ID {id}.");
    }

    return Results.Ok(user);
});


// Endpoint solo para probar el middleware de errores
app.MapGet("/api/users/test-error", () =>
{
    throw new Exception("Error simulado para probar el middleware.");
});


// POST: Crear un nuevo usuario
app.MapPost("/api/users", (User? user) =>
{
    if (user == null)
    {
        return Results.BadRequest("Los datos del usuario son obligatorios.");
    }

    NormalizeUser(user);

    var validationErrors = ValidateUser(user);

    if (validationErrors.Any())
    {
        return Results.BadRequest(validationErrors);
    }

    var emailExists = users.Values.Any(existingUser =>
        existingUser.Email.Equals(user.Email, StringComparison.OrdinalIgnoreCase));

    if (emailExists)
    {
        return Results.Conflict("Ya existe un usuario con ese email.");
    }

    user.Id = nextId;
    nextId++;

    users.Add(user.Id, user);

    return Results.Created($"/api/users/{user.Id}", user);
});


// PUT: Actualizar un usuario existente
app.MapPut("/api/users/{id:int}", (int id, User? updatedUser) =>
{
    if (updatedUser == null)
    {
        return Results.BadRequest("Los datos del usuario son obligatorios.");
    }

    if (!users.TryGetValue(id, out var existingUser))
    {
        return Results.NotFound($"No se encontró ningún usuario con el ID {id}.");
    }

    NormalizeUser(updatedUser);

    var validationErrors = ValidateUser(updatedUser);

    if (validationErrors.Any())
    {
        return Results.BadRequest(validationErrors);
    }

    var emailExists = users.Values.Any(user =>
        user.Id != id &&
        user.Email.Equals(updatedUser.Email, StringComparison.OrdinalIgnoreCase));

    if (emailExists)
    {
        return Results.Conflict("Ya existe otro usuario con ese email.");
    }

    existingUser.Name = updatedUser.Name;
    existingUser.Email = updatedUser.Email;
    existingUser.Department = updatedUser.Department;
    existingUser.Role = updatedUser.Role;

    return Results.Ok(existingUser);
});


// DELETE: Eliminar un usuario por ID
app.MapDelete("/api/users/{id:int}", (int id) =>
{
    if (!users.TryGetValue(id, out var user))
    {
        return Results.NotFound($"No se encontró ningún usuario con el ID {id}.");
    }

    users.Remove(id);

    return Results.NoContent();
});


app.Run();


// Limpia espacios al principio y al final.
// También evita problemas si algún campo llega como null desde el JSON.
static void NormalizeUser(User user)
{
    user.Name = user.Name?.Trim() ?? string.Empty;
    user.Email = user.Email?.Trim() ?? string.Empty;
    user.Department = user.Department?.Trim() ?? string.Empty;
    user.Role = user.Role?.Trim() ?? string.Empty;
}


// Valida el usuario usando los atributos definidos en la clase User.
static List<string> ValidateUser(User user)
{
    var errors = new List<string>();

    var validationContext = new ValidationContext(user);
    var validationResults = new List<ValidationResult>();

    bool isValid = Validator.TryValidateObject(
        user,
        validationContext,
        validationResults,
        true
    );

    if (!isValid)
    {
        errors.AddRange(validationResults.Select(result =>
            result.ErrorMessage ?? "Error de validación."));
    }

    return errors;
}


public class User
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "El email es obligatorio.")]
    [EmailAddress(ErrorMessage = "El formato del email no es válido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "El departamento es obligatorio.")]
    public string Department { get; set; } = string.Empty;

    [Required(ErrorMessage = "El rol es obligatorio.")]
    public string Role { get; set; } = string.Empty;
}