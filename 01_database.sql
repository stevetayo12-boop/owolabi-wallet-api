
-- Owolabi University Ventures Food Wallet DB
CREATE DATABASE OwolabiVenturesDB;
GO
USE OwolabiVenturesDB;
GO

CREATE TABLE Students (
    Id INT PRIMARY KEY IDENTITY(1,1),
    MatricNo NVARCHAR(50) UNIQUE NOT NULL,
    FullName NVARCHAR(100) NOT NULL,
    Department NVARCHAR(100),
    WalletId NVARCHAR(50) UNIQUE NOT NULL
);

CREATE TABLE Wallets (
    Id INT PRIMARY KEY IDENTITY(1,1),
    WalletId NVARCHAR(50) UNIQUE NOT NULL,
    StudentId INT FOREIGN KEY REFERENCES Students(Id),
    Balance DECIMAL(18,2) DEFAULT 0.00,
    QRToken NVARCHAR(100),
    QRTokenExpiry DATETIME,
    LastUpdated DATETIME DEFAULT GETDATE()
);

CREATE TABLE Transactions (
    Id INT PRIMARY KEY IDENTITY(1,1),
    WalletId NVARCHAR(50) NOT NULL,
    Type NVARCHAR(20), -- FUND, DEDUCT, REFUND
    Amount DECIMAL(18,2) NOT NULL,
    ItemSold NVARCHAR(100),
    VendorShop NVARCHAR(100),
    BalanceBefore DECIMAL(18,2),
    BalanceAfter DECIMAL(18,2),
    CreatedAt DATETIME DEFAULT GETDATE()
);

CREATE TABLE Vendors (
    Id INT PRIMARY KEY IDENTITY(1,1),
    ShopName NVARCHAR(100),
    StaffName NVARCHAR(100),
    TerminalId NVARCHAR(50)
);

-- SAMPLE DATA
INSERT INTO Students (MatricNo, FullName, Department, WalletId) VALUES 
('OU/2026/SE/001', 'Adebayo Samuel', 'Software Engineering', 'OU-WAL-0001'),
('OU/2026/NS/002', 'Owolabi Grace', 'Nursing Science', 'OU-WAL-0002');

INSERT INTO Wallets (WalletId, StudentId, Balance) VALUES
('OU-WAL-0001', 1, 15000.00),
('OU-WAL-0002', 2, 5000.00);

SELECT * FROM Wallets;
