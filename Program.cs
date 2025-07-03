using OracleHMS.Data;
using Oracle.ManagedDataAccess.Client;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<OracleDbService>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();


// ✅ Test DB connection endpoint
// app.MapGet("/testdb", async (OracleDbService dbService) =>
// {
//     try
//     {
//         var roomsData = await dbService.GetEmployeesAsync();
//         return Results.Ok("Database connection successful!");
//     }
//     catch (Exception ex)
//     {
//         return Results.Problem($"Database connection failed: {ex.Message}");
//     }
// });


app.MapPost("/register", async (OracleDbService dbService, HttpRequest request) =>
{
    try
    {
        var form = await request.ReadFormAsync();
        string username = form["username"];
        string email = form["email"];
        string password = form["password"];
        string role = form["role"];

        bool success = await dbService.RegisterUserAsync(username,email, password,role);
        return success ? Results.Ok(new { message = "User registered successfully.", role = role }) : Results.Problem("Failed to register user.");
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(ex.Message); // for "Username not available"
    }
    catch (Exception ex)
    {
        return Results.Problem($"Registration error: {ex.Message}");
    }
});

app.MapPost("/login", async (OracleDbService dbService, HttpRequest request) =>
{
    try
    {
        var form = await request.ReadFormAsync();
        string usernameOrEmail = form["username"];
        string password = form["password"];
        string role = form["role"];

        var user = await dbService.GetUserAsync(usernameOrEmail, password, role);

        if (user != null)
        {
            return Results.Ok(new
            {
                message = "Login successful",
                userid = user.UserId,
                username = user.Username,
                role = user.Role
            });
        }
        else
        {
            return Results.BadRequest(new
            {
                message = "Sorry, your username/email/role or password was incorrect."
            });
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("⚠️ Login Exception: " + ex.Message);
    Console.WriteLine(ex.StackTrace);
    return Results.Problem($"Login error: {ex.Message}");
    }
});

app.MapGet("/rooms", async (OracleDbService dbService) =>
{
    try
    {
        var rooms = await dbService.GetRoomsAsync();
        return Results.Ok(rooms);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Failed to fetch rooms: {ex.Message}");
    }
});

app.MapGet("/feedbacks", async (OracleDbService dbService) =>
{
    try
    {
        var feedbacks = await dbService.GetFeedbacksAsync();
        return Results.Ok(feedbacks);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Failed to fetch feedbacks: {ex.Message}");
    }
});

app.MapGet("/inventory", async (OracleDbService db) =>
{
    var inventory = await db.GetInventoryAsync();
    return Results.Ok(inventory);
});

app.MapGet("/suppliers", async (OracleDbService db) =>
{
    try
    {
        var suppliers = await db.GetSuppliersAsync();
        return Results.Ok(suppliers);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Failed to fetch suppliers: {ex.Message}");
    }
});

app.MapPost("/api/bookings", async (HttpContext http, OracleDbService db) =>
{
    var booking = await http.Request.ReadFromJsonAsync<Booking>();
    if (booking is null) return Results.BadRequest("Invalid booking data");

    string? generatedBookingId = await db.InsertBookingAsync(booking);
    return generatedBookingId != null
        ? Results.Ok(new { bookingId = generatedBookingId })
        : Results.Problem("Insert failed");
});

app.MapGet("/tasks", async (HttpContext context, OracleDbService db) =>
{
    var staffid = context.Request.Query["staff"].ToString();

    if (string.IsNullOrEmpty(staffid))
        return Results.BadRequest("Missing staff username.");

    var tasks = await db.GetTasksByStaffAsync(staffid);
    Console.WriteLine($"Fetched {tasks.Count} tasks for {staffid}");

    return Results.Ok(tasks);
});

//staff mgt in admin:
app.MapGet("/staff", async (OracleDbService db) =>
{
    var staff = await db.GetAllStaffAsync();
    return Results.Ok(staff);
});

// GET all guests
app.MapGet("/guests", async (OracleDbService db) =>
{
    var guests = await db.GetAllGuestsAsync();
    return Results.Ok(guests);
});

// DELETE a guest by ID
app.MapDelete("/guests/{id}", async (string id, OracleDbService db) =>
{
    var deleted = await db.DeleteUserAsync(id);
    return deleted ? Results.Ok() : Results.NotFound();
});

app.MapDelete("/staff/{id}", async (string id, OracleDbService db) =>
{
    var deleted = await db.DeleteUserAsync(id);
    return deleted ? Results.Ok() : Results.NotFound();
});

app.MapGet("/feedback", async (OracleDbService db) =>
{
    var feedback = await db.GetAllFeedbackAsync();
    return Results.Ok(feedback);
});

app.MapDelete("/feedback/{id}", async (string id, OracleDbService db) =>
{
    var deleted = await db.DeleteFeedbackAsync(id);
    return deleted ? Results.Ok() : Results.NotFound();
});

app.MapGet("/api/bookinghistory/{username}", async (string username, OracleDbService db) =>
{
    var bookings = await db.GetGuestBookingHistory(username);
    return Results.Ok(bookings);
});

app.MapPost("/submitfeedback", async (HttpContext http, OracleDbService db) =>
{
    var feedback = await http.Request.ReadFromJsonAsync<Feedback>();
    if (feedback is null) return Results.BadRequest("Invalid feedback data");

    string? newFeedbackId = await db.InsertFeedbackAsync(feedback);
    return newFeedbackId != null
        ? Results.Ok(new { status = "success", feedbackId = newFeedbackId })
        : Results.Problem("Failed to insert feedback");
});
app.MapGet("/payroll/{staffId}", async (string staffId, OracleDbService db) =>
{
     Console.WriteLine($"Received request for staffId: {staffId}");
    var payrollList = await db.GetPayrollByStaffIdAsync(staffId);
    
    if (payrollList == null || payrollList.Count == 0)
    {
           Console.WriteLine($"No payroll records found for staffId: {staffId}");
        return Results.NotFound(new { message = $"No payroll found for staffId: {staffId}" });
    }

    return Results.Ok(payrollList);
});

app.UseCors("AllowAll");

app.Run();


