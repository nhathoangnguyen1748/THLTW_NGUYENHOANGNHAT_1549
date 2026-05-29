IF DB_ID(N'WebBanHang_NHNHAT') IS NULL
BEGIN
    CREATE DATABASE [WebBanHang_NHNHAT];
END
GO

USE [WebBanHang_NHNHAT];
GO

IF OBJECT_ID(N'dbo.Categories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Categories
    (
        CategoryId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Categories PRIMARY KEY,
        Name NVARCHAR(120) NOT NULL,
        Slug NVARCHAR(140) NOT NULL,
        Description NVARCHAR(300) NULL,
        ImageUrl NVARCHAR(600) NULL,
        DisplayOrder INT NOT NULL CONSTRAINT DF_Categories_DisplayOrder DEFAULT 0,
        IsActive BIT NOT NULL CONSTRAINT DF_Categories_IsActive DEFAULT 1
    );
END
GO

IF OBJECT_ID(N'dbo.Products', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Products
    (
        ProductId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Products PRIMARY KEY,
        CategoryId INT NOT NULL,
        Name NVARCHAR(180) NOT NULL,
        Slug NVARCHAR(200) NOT NULL,
        Description NVARCHAR(MAX) NULL,
        Brand NVARCHAR(120) NULL,
        Price DECIMAL(18, 2) NOT NULL,
        OldPrice DECIMAL(18, 2) NULL,
        Stock INT NOT NULL CONSTRAINT DF_Products_Stock DEFAULT 0,
        Sold INT NOT NULL CONSTRAINT DF_Products_Sold DEFAULT 0,
        Rating DECIMAL(3, 2) NOT NULL CONSTRAINT DF_Products_Rating DEFAULT 5,
        ImageUrl NVARCHAR(600) NULL,
        IsFeatured BIT NOT NULL CONSTRAINT DF_Products_IsFeatured DEFAULT 0,
        IsActive BIT NOT NULL CONSTRAINT DF_Products_IsActive DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Products_CreatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_Products_Categories FOREIGN KEY (CategoryId) REFERENCES dbo.Categories(CategoryId)
    );
END
GO

IF OBJECT_ID(N'dbo.Customers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Customers
    (
        CustomerId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Customers PRIMARY KEY,
        FullName NVARCHAR(140) NOT NULL,
        Phone NVARCHAR(30) NOT NULL,
        Email NVARCHAR(160) NULL,
        Address NVARCHAR(300) NOT NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Customers_CreatedAt DEFAULT SYSUTCDATETIME()
    );
END
GO

IF OBJECT_ID(N'dbo.Orders', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Orders
    (
        OrderId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Orders PRIMARY KEY,
        OrderCode NVARCHAR(30) NOT NULL,
        CustomerId INT NULL,
        CustomerName NVARCHAR(140) NOT NULL,
        Phone NVARCHAR(30) NOT NULL,
        Email NVARCHAR(160) NULL,
        ShippingAddress NVARCHAR(300) NOT NULL,
        Note NVARCHAR(500) NULL,
        Status NVARCHAR(40) NOT NULL CONSTRAINT DF_Orders_Status DEFAULT N'Chờ xác nhận',
        Subtotal DECIMAL(18, 2) NOT NULL,
        ShippingFee DECIMAL(18, 2) NOT NULL CONSTRAINT DF_Orders_ShippingFee DEFAULT 0,
        Total DECIMAL(18, 2) NOT NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Orders_CreatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_Orders_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(CustomerId)
    );
END
GO

IF OBJECT_ID(N'dbo.DF_Orders_Status', N'D') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Orders DROP CONSTRAINT DF_Orders_Status;
    ALTER TABLE dbo.Orders ADD CONSTRAINT DF_Orders_Status DEFAULT N'Chờ xác nhận' FOR Status;
END
GO

IF OBJECT_ID(N'dbo.OrderItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OrderItems
    (
        OrderItemId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrderItems PRIMARY KEY,
        OrderId INT NOT NULL,
        ProductId INT NOT NULL,
        ProductName NVARCHAR(180) NOT NULL,
        UnitPrice DECIMAL(18, 2) NOT NULL,
        Quantity INT NOT NULL,
        LineTotal DECIMAL(18, 2) NOT NULL,
        CONSTRAINT FK_OrderItems_Orders FOREIGN KEY (OrderId) REFERENCES dbo.Orders(OrderId) ON DELETE CASCADE,
        CONSTRAINT FK_OrderItems_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products(ProductId)
    );
END
GO

IF OBJECT_ID(N'dbo.AdminUsers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AdminUsers
    (
        AdminUserId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AdminUsers PRIMARY KEY,
        Username NVARCHAR(80) NOT NULL,
        PasswordHash NVARCHAR(64) NOT NULL,
        FullName NVARCHAR(140) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_AdminUsers_IsActive DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_AdminUsers_CreatedAt DEFAULT SYSUTCDATETIME()
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Categories_Slug' AND object_id = OBJECT_ID(N'dbo.Categories'))
    CREATE UNIQUE INDEX UX_Categories_Slug ON dbo.Categories(Slug);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Products_Slug' AND object_id = OBJECT_ID(N'dbo.Products'))
    CREATE UNIQUE INDEX UX_Products_Slug ON dbo.Products(Slug);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Orders_OrderCode' AND object_id = OBJECT_ID(N'dbo.Orders'))
    CREATE UNIQUE INDEX UX_Orders_OrderCode ON dbo.Orders(OrderCode);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_AdminUsers_Username' AND object_id = OBJECT_ID(N'dbo.AdminUsers'))
    CREATE UNIQUE INDEX UX_AdminUsers_Username ON dbo.AdminUsers(Username);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Categories)
BEGIN
    INSERT INTO dbo.Categories (Name, Slug, Description, ImageUrl, DisplayOrder)
    VALUES
        (N'Điện thoại', N'dien-thoai', N'Thiết bị di động, phụ kiện và công nghệ mới.', N'https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?auto=format&fit=crop&w=900&q=80', 1),
        (N'Thời trang', N'thoi-trang', N'Giày, túi, áo khoác và phụ kiện phong cách.', N'https://images.unsplash.com/photo-1445205170230-053b83016050?auto=format&fit=crop&w=900&q=80', 2),
        (N'Gia dụng', N'gia-dung', N'Đồ dùng thông minh cho căn bếp và nhà cửa.', N'https://images.unsplash.com/photo-1556911220-bff31c812dba?auto=format&fit=crop&w=900&q=80', 3),
        (N'Làm đẹp', N'lam-dep', N'Mỹ phẩm, chăm sóc da và quà tặng cá nhân.', N'https://images.unsplash.com/photo-1596462502278-27bfdc403348?auto=format&fit=crop&w=900&q=80', 4);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Products)
BEGIN
    INSERT INTO dbo.Products
        (CategoryId, Name, Slug, Description, Brand, Price, OldPrice, Stock, Sold, Rating, ImageUrl, IsFeatured)
    VALUES
        ((SELECT CategoryId FROM dbo.Categories WHERE Slug = N'dien-thoai'), N'iPhone 15 Pro 256GB', N'iphone-15-pro-256gb', N'Máy mới fullbox, màn hình sáng, camera chuyên nghiệp và hiệu năng mạnh cho công việc hằng ngày.', N'Apple', 26990000, 29990000, 24, 318, 4.9, N'https://images.unsplash.com/photo-1695048133142-1a20484d2569?auto=format&fit=crop&w=900&q=80', 1),
        ((SELECT CategoryId FROM dbo.Categories WHERE Slug = N'dien-thoai'), N'Tai nghe Bluetooth ProSound', N'tai-nghe-bluetooth-prosound', N'Chống ồn chủ động, hộp sạc nhỏ gọn, âm bass chắc và pin dùng cả ngày.', N'ProSound', 1290000, 1890000, 86, 742, 4.8, N'https://images.unsplash.com/photo-1505740420928-5e560c06d30e?auto=format&fit=crop&w=900&q=80', 1),
        ((SELECT CategoryId FROM dbo.Categories WHERE Slug = N'thoi-trang'), N'Giày sneaker Urban Flex', N'giay-sneaker-urban-flex', N'Đệm êm, form gọn, hợp với đi học, đi làm và dạo phố cuối tuần.', N'Urban Flex', 890000, 1250000, 58, 521, 4.7, N'https://images.unsplash.com/photo-1542291026-7eec264c27ff?auto=format&fit=crop&w=900&q=80', 1),
        ((SELECT CategoryId FROM dbo.Categories WHERE Slug = N'thoi-trang'), N'Ba lô laptop Daily 15 inch', N'balo-laptop-daily-15-inch', N'Nhiều ngăn tiện ích, vải chống nước nhẹ và quai đeo êm vai.', N'Daily', 520000, 690000, 104, 287, 4.6, N'https://images.unsplash.com/photo-1553062407-98eeb64c6a62?auto=format&fit=crop&w=900&q=80', 0),
        ((SELECT CategoryId FROM dbo.Categories WHERE Slug = N'gia-dung'), N'Nồi chiên không dầu SmartCook 5L', N'noi-chien-khong-dau-smartcook-5l', N'Dung tích lớn, dễ vệ sinh, nhiều chế độ nấu nhanh cho bữa cơm gia đình.', N'SmartCook', 1490000, 1990000, 33, 196, 4.8, N'https://images.unsplash.com/photo-1585515320310-259814833e62?auto=format&fit=crop&w=900&q=80', 1),
        ((SELECT CategoryId FROM dbo.Categories WHERE Slug = N'gia-dung'), N'Máy xay đa năng HomeMix', N'may-xay-da-nang-homemix', N'Xay sinh tố, hạt và gia vị với cối thủy tinh chắc chắn.', N'HomeMix', 680000, 850000, 41, 154, 4.5, N'https://images.unsplash.com/photo-1570222094114-d054a817e56b?auto=format&fit=crop&w=900&q=80', 0),
        ((SELECT CategoryId FROM dbo.Categories WHERE Slug = N'lam-dep'), N'Serum cấp ẩm GlowSkin', N'serum-cap-am-glowskin', N'Kết cấu mỏng nhẹ, cấp ẩm sâu và làm da căng bóng tự nhiên.', N'GlowSkin', 390000, 490000, 72, 604, 4.7, N'https://images.unsplash.com/photo-1620916566398-39f1143ab7be?auto=format&fit=crop&w=900&q=80', 1),
        ((SELECT CategoryId FROM dbo.Categories WHERE Slug = N'lam-dep'), N'Nước hoa mini Amber Note', N'nuoc-hoa-mini-amber-note', N'Hương ấm, giữ mùi lâu, kích thước gọn để mang theo mỗi ngày.', N'Amber Note', 450000, 590000, 65, 244, 4.6, N'https://images.unsplash.com/photo-1541643600914-78b084683601?auto=format&fit=crop&w=900&q=80', 0);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AdminUsers WHERE Username = N'admin')
BEGIN
    INSERT INTO dbo.AdminUsers (Username, PasswordHash, FullName)
    VALUES (N'admin', N'240BE518FABD2724DDB6F04EEB1DA5967448D7E831C08C8FA822809F74C720A9', N'Quản trị viên');
END
GO

UPDATE dbo.Categories
SET Name = CASE Slug
        WHEN N'dien-thoai' THEN N'Điện thoại'
        WHEN N'thoi-trang' THEN N'Thời trang'
        WHEN N'gia-dung' THEN N'Gia dụng'
        WHEN N'lam-dep' THEN N'Làm đẹp'
        ELSE Name
    END,
    Description = CASE Slug
        WHEN N'dien-thoai' THEN N'Thiết bị di động, phụ kiện và công nghệ mới.'
        WHEN N'thoi-trang' THEN N'Giày, túi, áo khoác và phụ kiện phong cách.'
        WHEN N'gia-dung' THEN N'Đồ dùng thông minh cho căn bếp và nhà cửa.'
        WHEN N'lam-dep' THEN N'Mỹ phẩm, chăm sóc da và quà tặng cá nhân.'
        ELSE Description
    END
WHERE Slug IN (N'dien-thoai', N'thoi-trang', N'gia-dung', N'lam-dep');
GO

UPDATE dbo.Products
SET Name = CASE Slug
        WHEN N'giay-sneaker-urban-flex' THEN N'Giày sneaker Urban Flex'
        WHEN N'balo-laptop-daily-15-inch' THEN N'Ba lô laptop Daily 15 inch'
        WHEN N'noi-chien-khong-dau-smartcook-5l' THEN N'Nồi chiên không dầu SmartCook 5L'
        WHEN N'may-xay-da-nang-homemix' THEN N'Máy xay đa năng HomeMix'
        WHEN N'serum-cap-am-glowskin' THEN N'Serum cấp ẩm GlowSkin'
        WHEN N'nuoc-hoa-mini-amber-note' THEN N'Nước hoa mini Amber Note'
        ELSE Name
    END,
    Description = CASE Slug
        WHEN N'iphone-15-pro-256gb' THEN N'Máy mới fullbox, màn hình sáng, camera chuyên nghiệp và hiệu năng mạnh cho công việc hằng ngày.'
        WHEN N'tai-nghe-bluetooth-prosound' THEN N'Chống ồn chủ động, hộp sạc nhỏ gọn, âm bass chắc và pin dùng cả ngày.'
        WHEN N'giay-sneaker-urban-flex' THEN N'Đệm êm, form gọn, hợp với đi học, đi làm và dạo phố cuối tuần.'
        WHEN N'balo-laptop-daily-15-inch' THEN N'Nhiều ngăn tiện ích, vải chống nước nhẹ và quai đeo êm vai.'
        WHEN N'noi-chien-khong-dau-smartcook-5l' THEN N'Dung tích lớn, dễ vệ sinh, nhiều chế độ nấu nhanh cho bữa cơm gia đình.'
        WHEN N'may-xay-da-nang-homemix' THEN N'Xay sinh tố, hạt và gia vị với cối thủy tinh chắc chắn.'
        WHEN N'serum-cap-am-glowskin' THEN N'Kết cấu mỏng nhẹ, cấp ẩm sâu và làm da căng bóng tự nhiên.'
        WHEN N'nuoc-hoa-mini-amber-note' THEN N'Hương ấm, giữ mùi lâu, kích thước gọn để mang theo mỗi ngày.'
        ELSE Description
    END
WHERE Slug IN (
    N'iphone-15-pro-256gb',
    N'tai-nghe-bluetooth-prosound',
    N'giay-sneaker-urban-flex',
    N'balo-laptop-daily-15-inch',
    N'noi-chien-khong-dau-smartcook-5l',
    N'may-xay-da-nang-homemix',
    N'serum-cap-am-glowskin',
    N'nuoc-hoa-mini-amber-note'
);
GO

UPDATE dbo.Orders
SET Status = CASE Status
    WHEN N'Cho xac nhan' THEN N'Chờ xác nhận'
    WHEN N'Dang giao' THEN N'Đang giao'
    WHEN N'Hoan thanh' THEN N'Hoàn thành'
    WHEN N'Da huy' THEN N'Đã hủy'
    ELSE Status
END;
GO

UPDATE dbo.Orders
SET Status = N'Chờ xác nhận'
WHERE Status NOT IN (N'Chờ xác nhận', N'Đang giao', N'Hoàn thành', N'Đã hủy');
GO

UPDATE dbo.AdminUsers
SET FullName = N'Quản trị viên'
WHERE Username = N'admin';
GO
