
using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(o => o.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

// Initialize SQLite DB on startup
string dbPath = Path.Combine(AppContext.BaseDirectory, "owolabi.db");
bool dbExists = File.Exists(dbPath);
if (!dbExists)
{
    using var conn = new SqliteConnection($"Data Source={dbPath}");
    conn.Open();
    var cmd = conn.CreateCommand();
    cmd.CommandText = @"
        CREATE TABLE Students (Id INTEGER PRIMARY KEY AUTOINCREMENT, MatricNo TEXT UNIQUE, FullName TEXT, Department TEXT, WalletId TEXT UNIQUE);
        CREATE TABLE Wallets (Id INTEGER PRIMARY KEY AUTOINCREMENT, WalletId TEXT UNIQUE, StudentId INTEGER, Balance REAL DEFAULT 0, QRToken TEXT, QRTokenExpiry TEXT, LastUpdated TEXT);
        CREATE TABLE Transactions (Id INTEGER PRIMARY KEY AUTOINCREMENT, WalletId TEXT, Type TEXT, Amount REAL, ItemSold TEXT, VendorShop TEXT, BalanceBefore REAL, BalanceAfter REAL, CreatedAt TEXT DEFAULT (datetime('now')));
        INSERT INTO Students (MatricNo, FullName, Department, WalletId) VALUES ('OU/2026/SE/001','Adebayo Samuel','Software Engineering','OU-WAL-0001');
        INSERT INTO Students (MatricNo, FullName, Department, WalletId) VALUES ('OU/2026/NS/002','Owolabi Grace','Nursing Science','OU-WAL-0002');
        INSERT INTO Wallets (WalletId, StudentId, Balance) VALUES ('OU-WAL-0001', 1, 15000);
        INSERT INTO Wallets (WalletId, StudentId, Balance) VALUES ('OU-WAL-0002', 2, 5000);
    ";
    cmd.ExecuteNonQuery();
    Console.WriteLine($"DB Created at {dbPath}");
}

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI(c => {
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Owolabi API v1");
    c.RoutePrefix = "swagger";
});
app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapControllers();
app.Run();
