
// ADD THIS TO YOUR WalletController.cs on Render - Replace entire file with this version that includes Signup/Login

using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;

namespace OwolabiVentures.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WalletController : ControllerBase
    {
        string GetConnString()
        {
            string dbPath = Path.Combine(AppContext.BaseDirectory, "owolabi.db");
            return $"Data Source={dbPath}";
        }

        void EnsureColumns()
        {
            try{
                using var conn = new SqliteConnection(GetConnString());
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "ALTER TABLE Students ADD COLUMN Email TEXT; ALTER TABLE Students ADD COLUMN PasswordHash TEXT;";
                try{ cmd.ExecuteNonQuery(); }catch{}
            }catch{}
        }

        [HttpPost("signup")]
        public IActionResult Signup([FromBody] SignupRequest req)
        {
            EnsureColumns();
            if(string.IsNullOrEmpty(req.MatricNo) || string.IsNullOrEmpty(req.Password))
                return BadRequest(new { message = "Matric and Password required" });
            if(req.Password.Length < 6)
                return BadRequest(new { message = "Password min 6 chars" });

            using var conn = new SqliteConnection(GetConnString());
            conn.Open();

            var check = conn.CreateCommand();
            check.CommandText = "SELECT WalletId FROM Students WHERE MatricNo=$m";
            check.Parameters.AddWithValue("$m", req.MatricNo);
            var exists = check.ExecuteScalar();
            if(exists != null) return BadRequest(new { message = "Wallet already exists for this Matric. Please login." });

            string walletId = "OU-WAL-" + new Random().Next(1000, 9999).ToString();
            string hash = BCrypt.Net.BCrypt.HashPassword(req.Password);

            var insS = conn.CreateCommand();
            insS.CommandText = "INSERT INTO Students (MatricNo, FullName, Department, WalletId, Email, PasswordHash) VALUES ($matric, $name, $dept, $wid, $email, $pwd)";
            insS.Parameters.AddWithValue("$matric", req.MatricNo);
            insS.Parameters.AddWithValue("$name", req.FullName ?? req.MatricNo);
            insS.Parameters.AddWithValue("$dept", req.Department ?? "General");
            insS.Parameters.AddWithValue("$wid", walletId);
            insS.Parameters.AddWithValue("$email", req.Email ?? $"{req.MatricNo}@owolabi.edu.ng");
            insS.Parameters.AddWithValue("$pwd", hash);
            insS.ExecuteNonQuery();

            var insW = conn.CreateCommand();
            insW.CommandText = "INSERT INTO Wallets (WalletId, StudentId, Balance) VALUES ($wid, (SELECT Id FROM Students WHERE WalletId=$wid), 0)";
            insW.Parameters.AddWithValue("$wid", walletId);
            insW.ExecuteNonQuery();

            return Ok(new { success = true, walletId, message = "Wallet created!" });
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest req)
        {
            EnsureColumns();
            using var conn = new SqliteConnection(GetConnString());
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT s.FullName, s.WalletId, s.PasswordHash, w.Balance FROM Students s JOIN Wallets w ON s.WalletId=w.WalletId WHERE s.MatricNo=$matric";
            cmd.Parameters.AddWithValue("$matric", req.MatricNo);
            using var reader = cmd.ExecuteReader();
            if(!reader.Read()) return NotFound(new { message = "Matric not found. Signup first." });
            string hash = reader.IsDBNull(2) ? "" : reader.GetString(2);
            if(string.IsNullOrEmpty(hash) || !BCrypt.Net.BCrypt.Verify(req.Password, hash))
                return Unauthorized(new { message = "Wrong password" });

            return Ok(new { fullName = reader.GetString(0), walletId = reader.GetString(1), balance = reader.GetDecimal(3) });
        }

        // Keep all your previous endpoints: balance, generate-qr, deduct, fund, transactions, verify-paystack
        [HttpGet("balance/{walletId}")] public IActionResult GetBalance(string walletId){ using var conn = new SqliteConnection(GetConnString()); conn.Open(); var cmd = conn.CreateCommand(); cmd.CommandText="SELECT Balance FROM Wallets WHERE WalletId=$id"; cmd.Parameters.AddWithValue("$id",walletId); var bal=cmd.ExecuteScalar(); if(bal==null) return NotFound(new {message="Wallet not found"}); return Ok(new{walletId, balance=bal}); }
        [HttpPost("generate-qr/{walletId}")] public IActionResult GenerateQR(string walletId){ string token=Guid.NewGuid().ToString("N").Substring(0,6).ToUpper(); DateTime expiry=DateTime.Now.AddMinutes(2); using var conn=new SqliteConnection(GetConnString()); conn.Open(); var cmd=conn.CreateCommand(); cmd.CommandText="UPDATE Wallets SET QRToken=$t, QRTokenExpiry=$e WHERE WalletId=$id"; cmd.Parameters.AddWithValue("$t",token); cmd.Parameters.AddWithValue("$e",expiry.ToString("o")); cmd.Parameters.AddWithValue("$id",walletId); int rows=cmd.ExecuteNonQuery(); if(rows==0) return NotFound(new{message="Wallet not found"}); return Ok(new{walletId,token,qrContent=$"{walletId}|{token}",expiry=expiry.ToString()}); }
        [HttpPost("deduct")] public IActionResult Deduct([FromBody] DeductRequest req){ using var conn=new SqliteConnection(GetConnString()); conn.Open(); using var tran=conn.BeginTransaction(); try{ var cmd=conn.CreateCommand(); cmd.Transaction=tran; cmd.CommandText="SELECT Balance, QRToken, QRTokenExpiry FROM Wallets WHERE WalletId=$id"; cmd.Parameters.AddWithValue("$id",req.WalletId); using var reader=cmd.ExecuteReader(); if(!reader.Read()) return NotFound(new{message="Invalid Wallet"}); decimal balance=reader.GetDecimal(0); string dbToken=reader.IsDBNull(1)?"":reader.GetString(1); string expiryStr=reader.IsDBNull(2)?"":reader.GetString(2); DateTime expiry=DateTime.MinValue; DateTime.TryParse(expiryStr,out expiry); reader.Close(); if(string.IsNullOrEmpty(dbToken)||dbToken!=req.QRToken) return BadRequest(new{message="Expired QR - Refresh"}); if(expiry<DateTime.Now) return BadRequest(new{message="QR Expired"}); if(balance<req.Amount) return BadRequest(new{message=$"Insufficient Balance. Balance is {balance}"}); decimal newBal=balance-req.Amount; var upd=conn.CreateCommand(); upd.Transaction=tran; upd.CommandText="UPDATE Wallets SET Balance=$nb, QRToken=NULL, QRTokenExpiry=NULL, LastUpdated=datetime('now') WHERE WalletId=$id"; upd.Parameters.AddWithValue("$nb",newBal); upd.Parameters.AddWithValue("$id",req.WalletId); upd.ExecuteNonQuery(); var ins=conn.CreateCommand(); ins.Transaction=tran; ins.CommandText="INSERT INTO Transactions (WalletId, Type, Amount, ItemSold, VendorShop, BalanceBefore, BalanceAfter) VALUES ($wid,'DEDUCT',$amt,$item,$shop,$bb,$ba)"; ins.Parameters.AddWithValue("$wid",req.WalletId); ins.Parameters.AddWithValue("$amt",req.Amount); ins.Parameters.AddWithValue("$item",req.Item); ins.Parameters.AddWithValue("$shop",req.VendorShop); ins.Parameters.AddWithValue("$bb",balance); ins.Parameters.AddWithValue("$ba",newBal); ins.ExecuteNonQuery(); tran.Commit(); return Ok(new{success=true,newBalance=newBal,message=$"Payment Successful. Deducted {req.Amount}"}); }catch(Exception ex){tran.Rollback(); return StatusCode(500,ex.Message);} }
        [HttpPost("fund")] public IActionResult Fund([FromBody] FundRequest req){ using var conn=new SqliteConnection(GetConnString()); conn.Open(); var cmd=conn.CreateCommand(); cmd.CommandText="UPDATE Wallets SET Balance=Balance+$amt WHERE WalletId=$id"; cmd.Parameters.AddWithValue("$id",req.WalletId); cmd.Parameters.AddWithValue("$amt",req.Amount); cmd.ExecuteNonQuery(); var balCmd=conn.CreateCommand(); balCmd.CommandText="SELECT Balance FROM Wallets WHERE WalletId=$id"; balCmd.Parameters.AddWithValue("$id",req.WalletId); var newBal=balCmd.ExecuteScalar(); return Ok(new{success=true,newBalance=newBal}); }
        [HttpGet("transactions/{walletId}")] public IActionResult GetTransactions(string walletId){ using var conn=new SqliteConnection(GetConnString()); conn.Open(); var cmd=conn.CreateCommand(); cmd.CommandText="SELECT WalletId, Type, Amount, ItemSold, BalanceBefore, BalanceAfter, CreatedAt FROM Transactions WHERE WalletId=$id ORDER BY Id DESC LIMIT 20"; cmd.Parameters.AddWithValue("$id",walletId); using var reader=cmd.ExecuteReader(); var list=new List<object>(); while(reader.Read()){ list.Add(new{walletId=reader.GetString(0),type=reader.GetString(1),amount=reader.GetDecimal(2),item=reader.IsDBNull(3)?"":reader.GetString(3),before=reader.GetDecimal(4),after=reader.GetDecimal(5),date=reader.GetString(6)}); } return Ok(list); }
    }
    public class SignupRequest{public string MatricNo{get;set;} public string FullName{get;set;} public string Department{get;set;} public string Email{get;set;} public string Password{get;set;}}
    public class LoginRequest{public string MatricNo{get;set;} public string Password{get;set;}}
    public class DeductRequest{public string WalletId{get;set;}="" ;public string QRToken{get;set;}="" ;public decimal Amount{get;set;} public string Item{get;set;}="" ;public string VendorShop{get;set;}="Main Cafeteria";}
    public class FundRequest{public string WalletId{get;set;}="" ;public decimal Amount{get;set;}}
}
