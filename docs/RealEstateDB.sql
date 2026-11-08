/* ============================================================
   REAL ESTATE LISTING AND INQUIRY SYSTEM  (Topic 10) - Microsoft SQL Server 2022

   Du lieu mau:
     - 1 quan tri vien, 5 moi gioi, 5 khach hang
     - 6 loai bat dong san, 2 loai giao dich (Ban, Cho thue)
     - 30 tin dang (Active / Pending / Sold_Rented), 5 tin noi bat
     - 120 hinh anh (4 anh / tin), 15 yeu cau cua khach (New / Replied)

   Tai khoan dang nhap thu (mat khau chung: Demo@123 /  riêng tk admin và agent1  mật khẩu là 123456):
     Quan tri vien : estora.admin@gmail.com
     Moi gioi      : estora.agent1@gmail.com ... estora.agent5@gmail.com
     Khach hang    : estora.customer1@gmail.com ... estora.customer5@gmail.com


   ============================================================ */


CREATE DATABASE RealEstateDB;
GO

USE RealEstateDB;
GO

/* ############################################################
   PHAN 1: CAU TRUC CO SO DU LIEU (14 BANG)
   ############################################################ */

/* ============================================================
   NHOM 1: BANG LOI - USERS
   ============================================================ */

CREATE TABLE users (
    user_id         INT IDENTITY(1,1) PRIMARY KEY,
    role            NVARCHAR(20)   NOT NULL
                    CONSTRAINT CK_users_role CHECK (role IN (N'admin', N'user')),
    full_name       NVARCHAR(150)  NOT NULL,
    email           NVARCHAR(150)  NOT NULL UNIQUE,
    phone           NVARCHAR(15)   NOT NULL UNIQUE,
    password_hash   NVARCHAR(255)  NOT NULL,
    status          NVARCHAR(20)   NOT NULL DEFAULT N'active'
                    CONSTRAINT CK_users_status CHECK (status IN (N'active', N'locked')),
    created_at      DATETIME2      NOT NULL DEFAULT GETDATE()
);
GO

/* ============================================================
   NHOM 2: BANG DANH MUC
   ============================================================ */

CREATE TABLE property_types (
    property_type_id  INT IDENTITY(1,1) PRIMARY KEY,
    name               NVARCHAR(100) NOT NULL UNIQUE
);
GO

CREATE TABLE transaction_types (
    transaction_type_id  INT IDENTITY(1,1) PRIMARY KEY,
    name                  NVARCHAR(50) NOT NULL UNIQUE
);
GO

CREATE TABLE provinces (
    province_id  INT IDENTITY(1,1) PRIMARY KEY,
    name         NVARCHAR(100) NOT NULL UNIQUE
);
GO

CREATE TABLE districts (
    district_id  INT IDENTITY(1,1) PRIMARY KEY,
    province_id  INT NOT NULL,
    name         NVARCHAR(100) NOT NULL,
    CONSTRAINT FK_districts_province
        FOREIGN KEY (province_id) REFERENCES provinces(province_id)
);
GO

CREATE TABLE amenities (
    amenity_id  INT IDENTITY(1,1) PRIMARY KEY,
    name        NVARCHAR(100) NOT NULL UNIQUE
);
GO

CREATE TABLE legal_statuses (
    legal_status_id  INT IDENTITY(1,1) PRIMARY KEY,
    name             NVARCHAR(100) NOT NULL UNIQUE
);
GO

CREATE TABLE furniture_statuses (
    furniture_status_id  INT IDENTITY(1,1) PRIMARY KEY,
    name                  NVARCHAR(100) NOT NULL UNIQUE
);
GO

/* ============================================================
   NHOM 1 (tiep theo): PROPERTIES - bang trung tam
   ============================================================ */

CREATE TABLE properties (
    property_id           INT IDENTITY(1,1) PRIMARY KEY,
    owner_user_id          INT NOT NULL,
    property_type_id       INT NOT NULL,
    transaction_type_id    INT NOT NULL,
    district_id            INT NOT NULL,
    title                  NVARCHAR(200) NOT NULL,
    price                  DECIMAL(18,2) NOT NULL,
    area                   DECIMAL(10,2) NOT NULL,
    legal_status_id        INT NOT NULL,
    furniture_status_id    INT NOT NULL,
    bedrooms               INT NULL,
    bathrooms              INT NULL,
    floors                 INT NULL,
    description            NVARCHAR(MAX) NULL,
    status                 NVARCHAR(20) NOT NULL DEFAULT N'Pending'
                           CONSTRAINT CK_properties_status
                           CHECK (status IN (N'Active', N'Pending', N'Sold_Rented')),
    featured_until         DATETIME2 NULL,
    view_count             INT NOT NULL DEFAULT 0,
    created_at             DATETIME2 NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_properties_owner
        FOREIGN KEY (owner_user_id) REFERENCES users(user_id),
    CONSTRAINT FK_properties_type
        FOREIGN KEY (property_type_id) REFERENCES property_types(property_type_id),
    CONSTRAINT FK_properties_transaction
        FOREIGN KEY (transaction_type_id) REFERENCES transaction_types(transaction_type_id),
    CONSTRAINT FK_properties_district
        FOREIGN KEY (district_id) REFERENCES districts(district_id),
    CONSTRAINT FK_properties_legal
        FOREIGN KEY (legal_status_id) REFERENCES legal_statuses(legal_status_id),
    CONSTRAINT FK_properties_furniture
        FOREIGN KEY (furniture_status_id) REFERENCES furniture_statuses(furniture_status_id)
);
GO

/* ============================================================
   NHOM 3: BANG PHU TRO
   ============================================================ */

CREATE TABLE property_images (
    property_image_id  INT IDENTITY(1,1) PRIMARY KEY,
    property_id        INT NOT NULL,
    image_url          NVARCHAR(500) NOT NULL,
    is_primary         BIT NOT NULL DEFAULT 0,
    sort_order         INT NOT NULL DEFAULT 0,

    CONSTRAINT FK_images_property
        FOREIGN KEY (property_id) REFERENCES properties(property_id)
        ON DELETE CASCADE
);
GO

CREATE TABLE property_amenities (
    property_id  INT NOT NULL,
    amenity_id   INT NOT NULL,

    CONSTRAINT PK_property_amenities PRIMARY KEY (property_id, amenity_id),
    CONSTRAINT FK_propamenities_property
        FOREIGN KEY (property_id) REFERENCES properties(property_id)
        ON DELETE CASCADE,
    CONSTRAINT FK_propamenities_amenity
        FOREIGN KEY (amenity_id) REFERENCES amenities(amenity_id)
);
GO

CREATE TABLE conversations (
    conversation_id    INT IDENTITY(1,1) PRIMARY KEY,
    property_id        INT NULL,
    customer_user_id   INT NOT NULL,
    agent_user_id      INT NOT NULL,
    status             NVARCHAR(20) NOT NULL DEFAULT N'New'
                       CONSTRAINT CK_conversations_status CHECK (status IN (N'New', N'Replied')),
    created_at         DATETIME2 NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_conversations_property
        FOREIGN KEY (property_id) REFERENCES properties(property_id)
        ON DELETE SET NULL,
    CONSTRAINT FK_conversations_customer
        FOREIGN KEY (customer_user_id) REFERENCES users(user_id),
    CONSTRAINT FK_conversations_agent
        FOREIGN KEY (agent_user_id) REFERENCES users(user_id)
);
GO

CREATE TABLE messages (
    message_id       INT IDENTITY(1,1) PRIMARY KEY,
    conversation_id  INT NOT NULL,
    sender_user_id   INT NOT NULL,
    content          NVARCHAR(MAX) NOT NULL,
    created_at       DATETIME2 NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_messages_conversation
        FOREIGN KEY (conversation_id) REFERENCES conversations(conversation_id)
        ON DELETE CASCADE,
    CONSTRAINT FK_messages_sender
        FOREIGN KEY (sender_user_id) REFERENCES users(user_id)
);
GO

CREATE TABLE zalo_contacts (
    zalo_contact_id  INT IDENTITY(1,1) PRIMARY KEY,
    property_id      INT NOT NULL,
    clicked_at       DATETIME2 NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_zalocontacts_property
        FOREIGN KEY (property_id) REFERENCES properties(property_id)
        ON DELETE CASCADE
);
GO

CREATE TABLE saved_listings (
    saved_id     INT IDENTITY(1,1) PRIMARY KEY,
    user_id      INT NOT NULL,
    property_id  INT NOT NULL,
    created_at   DATETIME2 NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_saved_user
        FOREIGN KEY (user_id) REFERENCES users(user_id)
        ON DELETE CASCADE,
    CONSTRAINT FK_saved_property
        FOREIGN KEY (property_id) REFERENCES properties(property_id)
        ON DELETE CASCADE,
    CONSTRAINT UQ_saved_user_property UNIQUE (user_id, property_id)
);
GO

/* ############################################################
   PHAN 2: DU LIEU MAU
   ############################################################ */

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @pwd NVARCHAR(255) = N'$2a$10$EzZgukiy2hBkWFt.dd837evlzr.K0dXc9cGypS2Zj1Wmps/Rfh.La';

BEGIN TRANSACTION;

/* ---------- 1. Danh muc ---------- */
INSERT INTO property_types (name)
SELECT v.n FROM (VALUES
    (N'Căn hộ chung cư'),
    (N'Nhà phố'),
    (N'Biệt thự'),
    (N'Đất nền'),
    (N'Văn phòng'),
    (N'Mặt bằng kinh doanh')
) v(n)
WHERE NOT EXISTS (SELECT 1 FROM property_types t WHERE t.name = v.n);
INSERT INTO transaction_types (name)
SELECT v.n FROM (VALUES
    (N'Bán'),
    (N'Cho thuê')
) v(n)
WHERE NOT EXISTS (SELECT 1 FROM transaction_types t WHERE t.name = v.n);
INSERT INTO provinces (name)
SELECT v.n FROM (VALUES
    (N'TP. Hồ Chí Minh'),
    (N'Hà Nội'),
    (N'Đà Nẵng'),
    (N'Bình Dương')
) v(n)
WHERE NOT EXISTS (SELECT 1 FROM provinces t WHERE t.name = v.n);
INSERT INTO legal_statuses (name)
SELECT v.n FROM (VALUES
    (N'Sổ hồng riêng'),
    (N'Sổ đỏ'),
    (N'Hợp đồng mua bán'),
    (N'Đang chờ sổ')
) v(n)
WHERE NOT EXISTS (SELECT 1 FROM legal_statuses t WHERE t.name = v.n);
INSERT INTO furniture_statuses (name)
SELECT v.n FROM (VALUES
    (N'Đầy đủ nội thất'),
    (N'Nội thất cơ bản'),
    (N'Không nội thất')
) v(n)
WHERE NOT EXISTS (SELECT 1 FROM furniture_statuses t WHERE t.name = v.n);
INSERT INTO amenities (name)
SELECT v.n FROM (VALUES
    (N'Hồ bơi'),
    (N'Phòng gym'),
    (N'Bãi đậu xe'),
    (N'Thang máy'),
    (N'An ninh 24/7'),
    (N'Ban công'),
    (N'Sân vườn'),
    (N'Gần trường học'),
    (N'Gần trung tâm thương mại'),
    (N'Điều hòa')
) v(n)
WHERE NOT EXISTS (SELECT 1 FROM amenities t WHERE t.name = v.n);
INSERT INTO districts (province_id, name)
SELECT pr.province_id, v.dname FROM (VALUES
    (N'TP. Hồ Chí Minh', N'Quận 1'),
    (N'TP. Hồ Chí Minh', N'Quận 3'),
    (N'TP. Hồ Chí Minh', N'Quận 7'),
    (N'TP. Hồ Chí Minh', N'TP. Thủ Đức'),
    (N'TP. Hồ Chí Minh', N'Bình Thạnh'),
    (N'TP. Hồ Chí Minh', N'Phú Nhuận'),
    (N'TP. Hồ Chí Minh', N'Tân Bình'),
    (N'TP. Hồ Chí Minh', N'Bình Chánh'),
    (N'Hà Nội', N'Ba Đình'),
    (N'Hà Nội', N'Cầu Giấy'),
    (N'Hà Nội', N'Nam Từ Liêm'),
    (N'Hà Nội', N'Tây Hồ'),
    (N'Hà Nội', N'Hà Đông'),
    (N'Hà Nội', N'Đông Anh'),
    (N'Đà Nẵng', N'Hải Châu'),
    (N'Đà Nẵng', N'Sơn Trà'),
    (N'Đà Nẵng', N'Ngũ Hành Sơn'),
    (N'Bình Dương', N'Thủ Dầu Một'),
    (N'Bình Dương', N'Thuận An')
) v(pname, dname)
JOIN provinces pr ON pr.name = v.pname
WHERE NOT EXISTS (SELECT 1 FROM districts d WHERE d.province_id = pr.province_id AND d.name = v.dname);

/* ---------- 2. Tai khoan: 1 admin, 5 moi gioi, 5 khach hang ---------- */
INSERT INTO users (role, full_name, email, phone, password_hash, status, created_at)
SELECT v.role, v.full_name, v.email, v.phone, @pwd, N'active', DATEADD(DAY, -v.d, GETDATE())
FROM (VALUES
    (N'admin', N'Quản trị viên ESTORA', N'estora.admin@gmail.com', N'0901000000', 120),
    (N'user', N'Nguyễn Minh Khoa', N'estora.agent1@gmail.com', N'0901000001', 100),
    (N'user', N'Trần Thị Thu Hà', N'estora.agent2@gmail.com', N'0901000002', 95),
    (N'user', N'Lê Hoàng Nam', N'estora.agent3@gmail.com', N'0901000003', 90),
    (N'user', N'Phạm Quỳnh Anh', N'estora.agent4@gmail.com', N'0901000004', 85),
    (N'user', N'Võ Đức Thịnh', N'estora.agent5@gmail.com', N'0901000005', 80),
    (N'user', N'Nguyễn Văn An', N'estora.customer1@gmail.com', N'0912000001', 60),
    (N'user', N'Trần Thị Bích', N'estora.customer2@gmail.com', N'0912000002', 55),
    (N'user', N'Lê Quốc Cường', N'estora.customer3@gmail.com', N'0912000003', 50),
    (N'user', N'Phạm Thu Dung', N'estora.customer4@gmail.com', N'0912000004', 45),
    (N'user', N'Hoàng Gia Huy', N'estora.customer5@gmail.com', N'0912000005', 40)
) v(role, full_name, email, phone, d)
WHERE NOT EXISTS (SELECT 1 FROM users u WHERE u.email = v.email);

/* ---------- 3. 30 tin dang ---------- */
CREATE TABLE #seed (
    seq INT PRIMARY KEY,
    owner_email NVARCHAR(150) COLLATE DATABASE_DEFAULT,
    type_name NVARCHAR(100) COLLATE DATABASE_DEFAULT,
    trans_name NVARCHAR(50) COLLATE DATABASE_DEFAULT,
    province_name NVARCHAR(100) COLLATE DATABASE_DEFAULT,
    district_name NVARCHAR(100) COLLATE DATABASE_DEFAULT,
    title NVARCHAR(200) COLLATE DATABASE_DEFAULT,
    price DECIMAL(18,2),
    area DECIMAL(10,2),
    legal_name NVARCHAR(100) COLLATE DATABASE_DEFAULT,
    furniture_name NVARCHAR(100) COLLATE DATABASE_DEFAULT,
    bedrooms INT NULL,
    bathrooms INT NULL,
    floors INT NULL,
    description NVARCHAR(MAX) COLLATE DATABASE_DEFAULT,
    status NVARCHAR(20) COLLATE DATABASE_DEFAULT,
    featured_days INT NULL,
    view_count INT,
    days_ago INT
);

INSERT INTO #seed VALUES
(1, N'estora.agent4@gmail.com', N'Căn hộ chung cư', N'Bán', N'TP. Hồ Chí Minh', N'TP. Thủ Đức', N'Căn hộ 2PN Vinhomes Grand Park view sông, full nội thất', 3200000000, 69, N'Sổ hồng riêng', N'Đầy đủ nội thất', 2, 2, NULL, N'Căn hộ 2 phòng ngủ tầng cao, view sông thoáng mát, nội thất cao cấp bàn giao đầy đủ.
Cư dân hưởng đầy đủ tiện ích nội khu: hồ bơi, công viên, trường học, trung tâm thương mại.', N'Active', 120, 520, 12),
(2, N'estora.agent1@gmail.com', N'Căn hộ chung cư', N'Cho thuê', N'TP. Hồ Chí Minh', N'TP. Thủ Đức', N'Căn hộ Masteri Thảo Điền 3PN tầng cao cho thuê', 28000000, 95, N'Sổ hồng riêng', N'Đầy đủ nội thất', 3, 2, NULL, N'Căn hộ 3 phòng ngủ, tầng cao, view đẹp, gần ga Metro Thảo Điền.
Chủ nhà cho thuê dài hạn, hợp đồng tối thiểu 12 tháng.', N'Active', NULL, 310, 9),
(3, N'estora.agent1@gmail.com', N'Căn hộ chung cư', N'Cho thuê', N'TP. Hồ Chí Minh', N'Quận 7', N'Căn hộ 1PN Phú Mỹ Hưng Quận 7 gần Crescent Mall', 14500000, 55, N'Sổ hồng riêng', N'Nội thất cơ bản', 1, 1, NULL, N'Căn hộ 1 phòng ngủ sạch sẽ, nội thất cơ bản, phù hợp người đi làm hoặc cặp đôi trẻ.
Gần siêu thị, trường quốc tế và khu vui chơi giải trí.', N'Active', NULL, 185, 15),
(4, N'estora.agent2@gmail.com', N'Căn hộ chung cư', N'Bán', N'Hà Nội', N'Ba Đình', N'Căn hộ 3PN Vinhomes Metropolis Ba Đình view Hồ Tây', 14800000000, 120, N'Sổ hồng riêng', N'Đầy đủ nội thất', 3, 3, NULL, N'Căn hộ cao cấp 3 phòng ngủ, tầng cao view Hồ Tây, nội thất nhập khẩu.
Dự án có hồ bơi, phòng gym, khu mua sắm ngay dưới chân tòa nhà.', N'Active', 100, 640, 20),
(5, N'estora.agent2@gmail.com', N'Căn hộ chung cư', N'Bán', N'Hà Nội', N'Nam Từ Liêm', N'Căn hộ chung cư Mỹ Đình 2PN gần sân vận động', 4600000000, 82, N'Sổ hồng riêng', N'Nội thất cơ bản', 2, 2, NULL, N'Căn hộ 2 phòng ngủ góc, ban công rộng, đón gió tự nhiên.
Gần trường học, bệnh viện, ga Metro và nhiều tiện ích xung quanh.', N'Active', NULL, 240, 25),
(6, N'estora.agent3@gmail.com', N'Căn hộ chung cư', N'Cho thuê', N'Đà Nẵng', N'Hải Châu', N'Căn hộ view sông Hàn 2PN cho thuê trung tâm Đà Nẵng', 16000000, 70, N'Sổ hồng riêng', N'Đầy đủ nội thất', 2, 2, NULL, N'Căn hộ 2 phòng ngủ ngay trung tâm, view sông Hàn nhìn cầu Rồng.
Đầy đủ nội thất, có thể vào ở ngay.', N'Pending', NULL, 12, 2),
(7, N'estora.agent4@gmail.com', N'Nhà phố', N'Bán', N'TP. Hồ Chí Minh', N'Phú Nhuận', N'Nhà phố 4 tầng hẻm xe hơi đường Phan Xích Long', 11500000000, 60, N'Sổ hồng riêng', N'Nội thất cơ bản', 4, 4, 4, N'Nhà 1 trệt 3 lầu, hẻm xe hơi vào tận nhà, khu dân cư an ninh.
Gần chợ, trường học và khu ăn uống sầm uất Phan Xích Long.', N'Active', NULL, 270, 18),
(8, N'estora.agent1@gmail.com', N'Nhà phố', N'Bán', N'TP. Hồ Chí Minh', N'Quận 1', N'Nhà mặt tiền Hai Bà Trưng Quận 1 kinh doanh sầm uất', 38000000000, 90, N'Sổ hồng riêng', N'Không nội thất', 6, 6, 5, N'Nhà mặt tiền 5 tầng vị trí vàng Quận 1, phù hợp mở văn phòng, nhà hàng hoặc kinh doanh thương mại.
Pháp lý rõ ràng, sổ hồng riêng hoàn công đầy đủ.', N'Active', 90, 710, 30),
(9, N'estora.agent4@gmail.com', N'Nhà phố', N'Cho thuê', N'TP. Hồ Chí Minh', N'Tân Bình', N'Nhà phố 3 tầng cho thuê nguyên căn đường Cộng Hòa', 35000000, 80, N'Sổ hồng riêng', N'Nội thất cơ bản', 4, 4, 3, N'Nhà nguyên căn 3 tầng, mặt tiền đường lớn, phù hợp làm văn phòng hoặc showroom nhỏ.
Gần sân bay Tân Sơn Nhất, giao thông thuận tiện.', N'Active', NULL, 205, 11),
(10, N'estora.agent2@gmail.com', N'Nhà phố', N'Bán', N'Hà Nội', N'Hà Đông', N'Nhà liền kề 5 tầng khu đô thị Văn Quán', 13200000000, 75, N'Sổ hồng riêng', N'Đầy đủ nội thất', 5, 5, 5, N'Nhà liền kề xây 5 tầng, có thang máy, nội thất hiện đại.
Khu đô thị đồng bộ hạ tầng, nhiều tiện ích, an ninh tốt.', N'Pending', NULL, 20, 3),
(11, N'estora.agent3@gmail.com', N'Nhà phố', N'Bán', N'Đà Nẵng', N'Hải Châu', N'Nhà 3 tầng kiệt ô tô đường Lê Duẩn', 6900000000, 70, N'Sổ hồng riêng', N'Nội thất cơ bản', 3, 3, 3, N'Nhà 3 tầng kiệt ô tô, trung tâm quận Hải Châu, gần chợ Hàn.
Giao dịch đã hoàn tất.', N'Sold_Rented', NULL, 330, 40),
(12, N'estora.agent5@gmail.com', N'Nhà phố', N'Bán', N'Bình Dương', N'Thủ Dầu Một', N'Nhà phố thương mại Thủ Dầu Một 1 trệt 2 lầu', 5400000000, 100, N'Sổ đỏ', N'Nội thất cơ bản', 4, 3, 3, N'Nhà phố thương mại 1 trệt 2 lầu, mặt tiền đường lớn khu dân cư đông đúc.
Thích hợp ở kết hợp kinh doanh.', N'Active', NULL, 160, 22),
(13, N'estora.agent1@gmail.com', N'Biệt thự', N'Bán', N'TP. Hồ Chí Minh', N'TP. Thủ Đức', N'Biệt thự đơn lập Thảo Điền hồ bơi sân vườn', 45000000000, 350, N'Sổ hồng riêng', N'Đầy đủ nội thất', 5, 6, 3, N'Biệt thự đơn lập rộng 350m2 với hồ bơi riêng, sân vườn xanh mát, nội thất nhập khẩu.
Khu compound an ninh 24/7, gần trường quốc tế và trung tâm thương mại.', N'Active', 80, 880, 35),
(14, N'estora.agent4@gmail.com', N'Biệt thự', N'Bán', N'TP. Hồ Chí Minh', N'Quận 7', N'Biệt thự song lập Phú Mỹ Hưng Quận 7', 28000000000, 220, N'Sổ hồng riêng', N'Đầy đủ nội thất', 4, 5, 3, N'Biệt thự song lập 3 tầng khu Phú Mỹ Hưng, nội thất cao cấp, gara 2 ô tô.
Môi trường sống xanh, an ninh, đầy đủ tiện ích.', N'Active', NULL, 450, 28),
(15, N'estora.agent2@gmail.com', N'Biệt thự', N'Cho thuê', N'Hà Nội', N'Tây Hồ', N'Biệt thự view Hồ Tây cho thuê dài hạn', 120000000, 300, N'Sổ hồng riêng', N'Đầy đủ nội thất', 5, 5, 4, N'Biệt thự 4 tầng gần Hồ Tây, thích hợp làm nơi ở cho gia đình hoặc văn phòng đại diện.
Có sân vườn, gara và khu bếp riêng.', N'Active', NULL, 175, 14),
(16, N'estora.agent3@gmail.com', N'Biệt thự', N'Bán', N'Đà Nẵng', N'Sơn Trà', N'Biệt thự biển Sơn Trà 4PN gần bãi biển Mỹ Khê', 18500000000, 250, N'Sổ hồng riêng', N'Đầy đủ nội thất', 4, 4, 3, N'Biệt thự gần biển Mỹ Khê, thiết kế hiện đại, nhiều cửa kính đón view.
Thích hợp ở hoặc kinh doanh homestay cao cấp.', N'Pending', NULL, 30, 4),
(17, N'estora.agent5@gmail.com', N'Biệt thự', N'Bán', N'Bình Dương', N'Thuận An', N'Biệt thự vườn Thuận An rộng 400m2', 9800000000, 400, N'Sổ hồng riêng', N'Nội thất cơ bản', 4, 4, 2, N'Biệt thự vườn rộng rãi, nhiều cây xanh, phù hợp nghỉ dưỡng cuối tuần.
Giao dịch đã hoàn tất.', N'Sold_Rented', NULL, 290, 45),
(18, N'estora.agent5@gmail.com', N'Đất nền', N'Bán', N'TP. Hồ Chí Minh', N'Bình Chánh', N'Đất nền 100m2 khu dân cư Bình Chánh sổ riêng', 2100000000, 100, N'Sổ đỏ', N'Không nội thất', NULL, NULL, NULL, N'Đất nền thổ cư 100%, sổ đỏ riêng, đường trước nhà rộng 8m.
Khu dân cư hiện hữu, gần chợ và trường học.', N'Active', NULL, 190, 16),
(19, N'estora.agent3@gmail.com', N'Đất nền', N'Bán', N'Đà Nẵng', N'Ngũ Hành Sơn', N'Lô đất mặt tiền Ngũ Hành Sơn gần biển', 7500000000, 150, N'Sổ đỏ', N'Không nội thất', NULL, NULL, NULL, N'Lô đất mặt tiền đường lớn, cách biển vài phút di chuyển, tiềm năng kinh doanh lưu trú.
Pháp lý sổ đỏ chính chủ, sẵn sàng giao dịch.', N'Active', 70, 560, 26),
(20, N'estora.agent5@gmail.com', N'Đất nền', N'Bán', N'Bình Dương', N'Thủ Dầu Một', N'Đất nền thổ cư Thủ Dầu Một sổ sẵn', 1800000000, 90, N'Sổ đỏ', N'Không nội thất', NULL, NULL, NULL, N'Đất nền thổ cư 90m2, sổ sẵn công chứng ngay, hạ tầng điện nước đầy đủ.
Phù hợp xây nhà ở hoặc đầu tư.', N'Active', NULL, 95, 7),
(21, N'estora.agent2@gmail.com', N'Đất nền', N'Bán', N'Hà Nội', N'Đông Anh', N'Đất thổ cư Đông Anh gần khu công nghiệp', 3900000000, 80, N'Sổ đỏ', N'Không nội thất', NULL, NULL, NULL, N'Đất thổ cư 80m2, mặt ngõ ô tô, gần khu công nghiệp và đường vành đai.
Đang chờ xác minh pháp lý.', N'Pending', NULL, 8, 1),
(22, N'estora.agent4@gmail.com', N'Đất nền', N'Bán', N'TP. Hồ Chí Minh', N'TP. Thủ Đức', N'Đất nền TP. Thủ Đức gần ga Metro', 6200000000, 120, N'Hợp đồng mua bán', N'Không nội thất', NULL, NULL, NULL, N'Lô đất đẹp gần ga Metro, thuận tiện di chuyển về trung tâm.
Giao dịch đã hoàn tất.', N'Sold_Rented', NULL, 215, 50),
(23, N'estora.agent1@gmail.com', N'Văn phòng', N'Cho thuê', N'TP. Hồ Chí Minh', N'Quận 1', N'Văn phòng 120m2 Quận 1 tòa nhà hạng B', 85000000, 120, N'Sổ hồng riêng', N'Nội thất cơ bản', NULL, 2, NULL, N'Văn phòng 120m2 mặt sàn vuông vức, sẵn trần sàn và điều hòa trung tâm.
Tòa nhà có hầm giữ xe, bảo vệ 24/7, gần các ngân hàng và khách sạn.', N'Active', NULL, 230, 13),
(24, N'estora.agent2@gmail.com', N'Văn phòng', N'Cho thuê', N'Hà Nội', N'Cầu Giấy', N'Văn phòng 80m2 Duy Tân Cầu Giấy', 32000000, 80, N'Sổ hồng riêng', N'Nội thất cơ bản', NULL, 1, NULL, N'Văn phòng 80m2 tại tòa nhà văn phòng khu Duy Tân, giao thông thuận lợi.
Giá thuê đã gồm phí quản lý.', N'Active', NULL, 140, 10),
(25, N'estora.agent3@gmail.com', N'Văn phòng', N'Cho thuê', N'Đà Nẵng', N'Hải Châu', N'Văn phòng 60m2 mặt tiền Bạch Đằng đã cho thuê', 18000000, 60, N'Sổ hồng riêng', N'Nội thất cơ bản', NULL, 1, NULL, N'Văn phòng 60m2 mặt tiền đường Bạch Đằng, view sông Hàn.
Đã có khách thuê.', N'Sold_Rented', NULL, 120, 35),
(26, N'estora.agent4@gmail.com', N'Văn phòng', N'Bán', N'TP. Hồ Chí Minh', N'Tân Bình', N'Sàn văn phòng 200m2 Tân Bình bán lâu dài', 22000000000, 200, N'Sổ hồng riêng', N'Nội thất cơ bản', NULL, 4, NULL, N'Sàn văn phòng 200m2 sở hữu lâu dài, gần sân bay, hạ tầng giao thông thuận lợi.
Đang chờ duyệt thông tin.', N'Pending', NULL, 15, 3),
(27, N'estora.agent1@gmail.com', N'Mặt bằng kinh doanh', N'Cho thuê', N'TP. Hồ Chí Minh', N'Quận 1', N'Mặt bằng kinh doanh đường Nguyễn Huệ 110m2', 150000000, 110, N'Sổ hồng riêng', N'Không nội thất', NULL, 2, 2, N'Mặt bằng 2 tầng ngay khu phố đi bộ Nguyễn Huệ, lượng khách qua lại rất lớn.
Phù hợp mở cửa hàng thời trang, nhà hàng, thương hiệu F&B.', N'Active', NULL, 360, 8),
(28, N'estora.agent4@gmail.com', N'Mặt bằng kinh doanh', N'Cho thuê', N'TP. Hồ Chí Minh', N'Bình Thạnh', N'Mặt bằng cafe góc 2 mặt tiền Bình Thạnh', 45000000, 90, N'Sổ hồng riêng', N'Nội thất cơ bản', NULL, 2, 2, N'Mặt bằng góc 2 mặt tiền, thiết kế sẵn khu bếp và quầy bar.
Khu vực đông dân cư, gần trường đại học và văn phòng.', N'Active', NULL, 165, 6),
(29, N'estora.agent2@gmail.com', N'Mặt bằng kinh doanh', N'Cho thuê', N'Hà Nội', N'Tây Hồ', N'Cửa hàng mặt tiền Tây Hồ gần Hồ Tây', 60000000, 100, N'Sổ hồng riêng', N'Nội thất cơ bản', NULL, 2, 2, N'Cửa hàng mặt tiền đường lớn khu Tây Hồ, thích hợp kinh doanh nhà hàng, spa hoặc showroom.
Có chỗ đỗ xe rộng rãi phía trước.', N'Active', NULL, 130, 5),
(30, N'estora.agent5@gmail.com', N'Mặt bằng kinh doanh', N'Cho thuê', N'Bình Dương', N'Thuận An', N'Mặt bằng 150m2 Thuận An gần khu công nghiệp', 25000000, 150, N'Sổ đỏ', N'Không nội thất', NULL, 1, 1, N'Mặt bằng rộng 150m2 gần khu công nghiệp, phù hợp mở quán ăn, cửa hàng tiện lợi hoặc kho nhỏ.
Đang chờ xác nhận thông tin chủ nhà.', N'Pending', NULL, 6, 1);

INSERT INTO properties
    (owner_user_id, property_type_id, transaction_type_id, district_id, title, price, area,
     legal_status_id, furniture_status_id, bedrooms, bathrooms, floors, description, status,
     featured_until, view_count, created_at)
SELECT u.user_id, pt.property_type_id, tt.transaction_type_id, d.district_id, s.title, s.price, s.area,
       ls.legal_status_id, fs.furniture_status_id, s.bedrooms, s.bathrooms, s.floors, s.description, s.status,
       CASE WHEN s.featured_days IS NULL THEN NULL ELSE DATEADD(DAY, s.featured_days, GETDATE()) END,
       s.view_count, DATEADD(DAY, -s.days_ago, GETDATE())
FROM #seed s
JOIN users u ON u.email = s.owner_email
JOIN property_types pt ON pt.name = s.type_name
JOIN transaction_types tt ON tt.name = s.trans_name
JOIN provinces pr ON pr.name = s.province_name
JOIN districts d ON d.province_id = pr.province_id AND d.name = s.district_name
JOIN legal_statuses ls ON ls.name = s.legal_name
JOIN furniture_statuses fs ON fs.name = s.furniture_name
WHERE NOT EXISTS (SELECT 1 FROM properties p WHERE p.title = s.title)
ORDER BY s.seq;

/* ---------- 4. Hinh anh: 4 anh / tin (dung 5 anh san co trong Content/images) ---------- */
INSERT INTO property_images (property_id, image_url, is_primary, sort_order)
SELECT p.property_id,
       N'~/Content/images/property-' + CAST(((s.seq + n.k) % 5) + 1 AS NVARCHAR(2)) + N'.jpg',
       CASE WHEN n.k = 0 THEN 1 ELSE 0 END,
       n.k
FROM #seed s
JOIN properties p ON p.title = s.title
CROSS JOIN (VALUES (0), (1), (2), (3)) n(k)
WHERE NOT EXISTS (SELECT 1 FROM property_images i WHERE i.property_id = p.property_id);

/* ---------- 5. Tien ich (tru dat nen) ---------- */
INSERT INTO property_amenities (property_id, amenity_id)
SELECT p.property_id, a.amenity_id
FROM #seed s
JOIN properties p ON p.title = s.title
CROSS JOIN amenities a
WHERE s.type_name <> N'Đất nền'
  AND (a.amenity_id + s.seq) % 3 <> 0
  AND NOT EXISTS (SELECT 1 FROM property_amenities x WHERE x.property_id = p.property_id);

/* ---------- 6. 15 yeu cau tu khach hang (cuoc tro chuyen + tin nhan) ---------- */
CREATE TABLE #conv (
    listing_seq INT,
    customer_email NVARCHAR(150) COLLATE DATABASE_DEFAULT,
    status NVARCHAR(20) COLLATE DATABASE_DEFAULT,
    msg NVARCHAR(MAX) COLLATE DATABASE_DEFAULT,
    reply NVARCHAR(MAX) COLLATE DATABASE_DEFAULT NULL,
    days_ago INT
);
INSERT INTO #conv VALUES
(1, N'estora.customer1@gmail.com', N'New', N'Chào anh/chị, căn hộ này còn không ạ? Giá 3,2 tỷ có thương lượng được không?', NULL, 2),
(1, N'estora.customer2@gmail.com', N'Replied', N'Mình muốn xem căn hộ vào cuối tuần này, anh/chị sắp xếp được không?', N'Chào bạn, cuối tuần này mình sắp xếp được vào 10h sáng thứ Bảy. Bạn để lại số điện thoại giúp mình nhé.', 5),
(2, N'estora.customer3@gmail.com', N'Replied', N'Căn hộ cho thuê tối thiểu bao lâu và có cho nuôi thú cưng không ạ?', N'Hợp đồng tối thiểu 12 tháng, chủ nhà cho nuôi thú cưng nhỏ. Tiền cọc 2 tháng bạn nhé.', 4),
(4, N'estora.customer1@gmail.com', N'New', N'Cho mình hỏi căn hộ còn trống không và phí quản lý mỗi tháng là bao nhiêu?', NULL, 1),
(4, N'estora.customer4@gmail.com', N'Replied', N'Mình quan tâm căn 3PN này, có hỗ trợ vay ngân hàng được không ạ?', N'Dự án có ngân hàng hỗ trợ vay tối đa 70% giá trị. Mình sẽ gửi bạn thông tin chi tiết qua tin nhắn.', 6),
(7, N'estora.customer2@gmail.com', N'New', N'Nhà có sổ hồng hoàn công chưa ạ? Hẻm rộng mấy mét?', NULL, 3),
(8, N'estora.customer5@gmail.com', N'Replied', N'Mình muốn mua để kinh doanh, giá chốt tốt nhất là bao nhiêu ạ?', N'Giá chủ nhà đang chào 38 tỷ, thương lượng thiện chí khoảng 2%. Mình có thể sắp xếp cho bạn xem nhà.', 8),
(8, N'estora.customer3@gmail.com', N'New', N'Cho mình xin thêm hình ảnh thực tế các tầng nhé.', NULL, 1),
(9, N'estora.customer4@gmail.com', N'Replied', N'Nhà có cho thuê làm văn phòng công ty được không ạ?', N'Được bạn nhé, chủ nhà cho thuê làm văn phòng, không kinh doanh ăn uống.', 3),
(13, N'estora.customer5@gmail.com', N'New', N'Biệt thự này có thể xem vào chiều Chủ nhật được không?', NULL, 2),
(13, N'estora.customer1@gmail.com', N'Replied', N'Mình là khách nước ngoài, thủ tục mua có phức tạp không ạ?', N'Chào bạn, mình sẽ hỗ trợ tư vấn đầy đủ thủ tục pháp lý. Bạn để lại email để mình gửi hồ sơ nhé.', 7),
(14, N'estora.customer2@gmail.com', N'Replied', N'Giá 28 tỷ đã bao gồm nội thất chưa ạ?', N'Giá đã gồm nội thất cố định, đồ rời có thể thương lượng thêm.', 9),
(18, N'estora.customer3@gmail.com', N'New', N'Đất đã tách sổ riêng chưa và đường vào rộng bao nhiêu mét?', NULL, 3),
(23, N'estora.customer1@gmail.com', N'New', N'Văn phòng có chỗ để xe ô tô cho nhân viên không ạ?', NULL, 2),
(11, N'estora.customer4@gmail.com', N'Replied', N'Căn này còn không ạ, mình muốn đi xem thử?', N'Cảm ơn bạn đã quan tâm, căn này đã bán xong. Mình sẽ gửi bạn các căn tương tự.', 20);

INSERT INTO conversations (property_id, customer_user_id, agent_user_id, status, created_at)
SELECT p.property_id, cu.user_id, p.owner_user_id, c.status, DATEADD(DAY, -c.days_ago, GETDATE())
FROM #conv c
JOIN #seed s ON s.seq = c.listing_seq
JOIN properties p ON p.title = s.title
JOIN users cu ON cu.email = c.customer_email
WHERE NOT EXISTS (SELECT 1 FROM conversations x WHERE x.property_id = p.property_id AND x.customer_user_id = cu.user_id);

-- Tin nhan dau tien cua khach
INSERT INTO messages (conversation_id, sender_user_id, content, created_at)
SELECT cv.conversation_id, cv.customer_user_id, c.msg, cv.created_at
FROM #conv c
JOIN #seed s ON s.seq = c.listing_seq
JOIN properties p ON p.title = s.title
JOIN users cu ON cu.email = c.customer_email
JOIN conversations cv ON cv.property_id = p.property_id AND cv.customer_user_id = cu.user_id
WHERE NOT EXISTS (SELECT 1 FROM messages m WHERE m.conversation_id = cv.conversation_id);

-- Tin nhan phan hoi cua moi gioi (cac cuoc o trang thai Replied)
INSERT INTO messages (conversation_id, sender_user_id, content, created_at)
SELECT cv.conversation_id, cv.agent_user_id, c.reply, DATEADD(HOUR, 3, cv.created_at)
FROM #conv c
JOIN #seed s ON s.seq = c.listing_seq
JOIN properties p ON p.title = s.title
JOIN users cu ON cu.email = c.customer_email
JOIN conversations cv ON cv.property_id = p.property_id AND cv.customer_user_id = cu.user_id
WHERE c.reply IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM messages m WHERE m.conversation_id = cv.conversation_id AND m.sender_user_id = cv.agent_user_id);

DROP TABLE #conv;
DROP TABLE #seed;

COMMIT TRANSACTION;

SELECT user_id, role, email, status, LEFT(password_hash, 20) AS hash_dau FROM users;
ALTER TABLE properties ADD created_by_user_id INT NULL;
ALTER TABLE properties ADD CONSTRAINT FK_properties_created_by
    FOREIGN KEY (created_by_user_id) REFERENCES users(user_id);

/* ---------- 7. Kiem tra ket qua ---------- */
SELECT N'Quan tri vien' AS hang_muc, COUNT(*) AS so_luong FROM users WHERE role = N'admin'
UNION ALL SELECT N'Tai khoan nguoi dung (moi gioi + khach)', COUNT(*) FROM users WHERE role = N'user'
UNION ALL SELECT N'Loai bat dong san', COUNT(*) FROM property_types
UNION ALL SELECT N'Loai giao dich', COUNT(*) FROM transaction_types
UNION ALL SELECT N'Tin dang', COUNT(*) FROM properties
UNION ALL SELECT N'  - Active', COUNT(*) FROM properties WHERE status = N'Active'
UNION ALL SELECT N'  - Pending', COUNT(*) FROM properties WHERE status = N'Pending'
UNION ALL SELECT N'  - Sold_Rented', COUNT(*) FROM properties WHERE status = N'Sold_Rented'
UNION ALL SELECT N'  - Dang noi bat', COUNT(*) FROM properties WHERE featured_until > GETDATE()
UNION ALL SELECT N'Hinh anh', COUNT(*) FROM property_images
UNION ALL SELECT N'Yeu cau (cuoc tro chuyen)', COUNT(*) FROM conversations
UNION ALL SELECT N'  - New', COUNT(*) FROM conversations WHERE status = N'New'
UNION ALL SELECT N'  - Replied', COUNT(*) FROM conversations WHERE status = N'Replied'
UNION ALL SELECT N'Tin nhan', COUNT(*) FROM messages;
GO
