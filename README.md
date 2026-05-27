# CyberCafe Booking & Management API

Hệ thống backend quản lý giờ chơi và đặt máy trạm cho phòng máy (Cyber Cafe) được xây dựng trên nền tảng ASP.NET Core Web API 9, tuân thủ nghiêm ngặt các nguyên lý kiến trúc Clean Architecture (N-Tier Architecture).

---

## Tính năng hệ thống

* **Xác thực và Phân quyền JWT**: Quản lý truy cập dựa trên phân quyền giữa ba nhóm tài khoản chính: Admin (Quản trị viên), Staff (Nhân viên), và Customer (Khách hàng). Mật khẩu được mã hóa an toàn sử dụng thuật toán BCrypt.
* **Đổi mật khẩu**: Cho phép người dùng đã đăng nhập tự thay đổi mật khẩu của mình một cách bảo mật.
* **Ví điện tử thành viên (Wallet) & Nạp tiền (Top-Up)**: 
  - Cơ chế tự động thiết lập ví điện tử trống tương ứng với mỗi tài khoản khách hàng mới đăng ký.
  - Cho phép Admin hoặc Staff nạp tiền trực tiếp cho khách hàng nhanh chóng tại quầy.
* **Quản lý phòng máy (Zones & Computers)**: CRUD đầy đủ (Thêm/Xem/Sửa/Xóa) khu vực phòng máy (Standard, VIP) và máy trạm với cơ chế ràng buộc toàn vẹn cơ sở dữ liệu.
* **Phiên làm việc Kế hoạch chơi (StartSession)**:
  - Kiểm duyệt điều kiện số dư tối thiểu trong ví (tương đương tối thiểu một giờ chơi của máy mục tiêu).
  - Chuyển đổi trạng thái máy trạm sang trạng thái đang hoạt động (InUse).
  - Bảo đảm tính toàn vẹn và chống xung đột đặt máy đồng thời thông qua cơ chế EF Core Transactions và Optimistic Concurrency (RowVersion).
* **Kết thúc phiên làm việc (EndSession)**:
  - Cơ chế tính phí dịch vụ chính xác theo từng phút hoạt động thực tế nhằm đảm bảo tính công bằng tối đa cho khách hàng.
  - Tự động khấu trừ số dư ví thành viên theo chi phí phát sinh thực tế.
  - Ghi nhận lịch sử giao dịch chi tiết (Transactions) phục vụ mục đích kiểm toán.
  - Giải phóng máy trạm về trạng thái sẵn sàng sử dụng (Available).
* **Quản lý tài khoản và Giao dịch**:
  - Quản trị viên dễ dàng quản lý danh sách người dùng, sửa thông tin hoặc xóa tài khoản.
  - Xem lịch sử biến động số dư chi tiết của từng ví (Nạp tiền, tiêu dùng giờ chơi).

---

## Kiến trúc dự án (Clean Architecture)

Hệ thống được chia thành bốn phân lớp độc lập nhằm tối ưu hóa khả năng kiểm thử, bảo trì và phát triển tính năng mới:

```
CyberCafe.sln
├── CyberCafe.Core           (Class Library) - Phân lớp Domain
│   ├── Entities/            - Định nghĩa các thực thể nghiệp vụ (User, Wallet, Computer, Session...)
│   ├── Enums/               - Các tập hợp định nghĩa trạng thái tĩnh (UserRole, ComputerStatus...)
│   ├── Interfaces/          - Thiết lập hợp đồng dịch vụ cốt lõi (IAuthService, IBookingService, IAccountService)
│   └── DTOs/                - Các đối tượng trung chuyển dữ liệu (Auth, Booking, Account)
│
├── CyberCafe.Infrastructure (Class Library) - Phân lớp Infrastructure
│   └── Data/
│       ├── CyberCafeDbContext - Cấu hình Fluent API, ánh xạ quan hệ và hạt giống dữ liệu (Seed Data)
│       └── Migrations/      - Bản ghi lịch sử cấu trúc cơ sở dữ liệu
│
├── CyberCafe.Services       (Class Library) - Phân lớp Nghiệp vụ (Application Services)
│   ├── AuthService          - Logic xử lý đăng ký, đăng nhập, đổi mật khẩu và phát hành Token bảo mật
│   ├── BookingService       - Logic điều khiển phiên chơi, tính cước chính xác và giao dịch an toàn
│   └── AccountService       - Xử lý CRUD người dùng, nạp tiền vào ví và tra cứu lịch sử giao dịch
│
└── CyberCafe.API            (Web API Project) - Phân lớp Hiển thị (Presentation Layer)
    ├── Controllers/         - RESTful API Controllers (Auth, Booking, Zones, Computers, Account)
    ├── Properties/          - launchSettings.json (Cấu hình môi trường vận hành)
    └── Program.cs           - Điểm khởi chạy ứng dụng, cấu hình DI, JWT Auth và Swagger UI (Http Bearer)
```

---

## Sơ đồ Cơ sở Dữ liệu (Database Schema)

Dự án áp dụng mô hình Code-First với Entity Framework Core, hỗ trợ linh hoạt cả SQLite (phát triển nội bộ) và MySQL/MariaDB (vận hành thực tế).

```mermaid
erDiagram
    USER ||--|| WALLET : "1-to-1"
    USER ||--o{ GAME_SESSION : "has many"
    ZONE ||--o{ COMPUTER : "contains"
    COMPUTER ||--o{ GAME_SESSION : "has many"
    WALLET ||--o{ TRANSACTION : "records"

    USER {
        int Id PK
        string Username
        string PasswordHash
        string Role
    }
    WALLET {
        int Id PK
        int UserId FK
        decimal Balance
    }
    ZONE {
        int Id PK
        string Name
        decimal PricePerHour
    }
    COMPUTER {
        int Id PK
        int ZoneId FK
        string Name
        string Status
        string Specs
        byte[] RowVersion
    }
    GAME_SESSION {
        int Id PK
        int UserId FK
        int ComputerId FK
        DateTime StartTime
        DateTime EndTime
    }
    TRANSACTION {
        int Id PK
        int WalletId FK
        decimal Amount
        string Type
        DateTime Date
        string Description
    }
```

---

## Hướng dẫn cài đặt và Vận hành

### Yêu cầu tiên quyết
* Hệ điều hành: Windows, Linux hoặc macOS.
* Môi trường phát triển: .NET SDK phiên bản 8.0 hoặc 9.0 trở lên.

### Quy trình chạy cục bộ
1. **Sao chép kho lưu trữ**:
   ```bash
   git clone <https://github.com/namnguyen2608/InternetCafeBackend>
   cd InternetCafeBackend
   ```
2. **Khởi tạo dữ liệu và ứng dụng**:
   Khởi chạy kịch bản tự động trong PowerShell để khôi phục gói thư viện, thiết lập cấu trúc cơ sở dữ liệu SQLite cục bộ (`cybercafe.db`) và khởi động dịch vụ:
   ```powershell
   ./setup.ps1
   ```
   *Hoặc chạy trực tiếp bằng lệnh:*
   ```bash
   dotnet run --project CyberCafe.API --launch-profile "CyberCafe.API"
   ```
3. **Môi trường Swagger UI**:
   Sau khi API khởi chạy thành công, giao diện kiểm thử Swagger UI sẵn sàng phục vụ tại địa chỉ mặc định:
   * **http://localhost:5000/swagger**

---

## Danh mục API Endpoints

### 1. Dịch vụ Xác thực (Authentication)
* `POST /api/auth/register` - Đăng ký tài khoản mới (Mặc định Role: Customer)
* `POST /api/auth/login` - Thực hiện đăng nhập và trả về mã bảo mật JWT

### 2. Quản lý Tài khoản & Giao dịch (Account & Wallet)
* `GET /api/account/users` - Danh sách tất cả người dùng kèm số dư ví (Yêu cầu: Admin)
* `GET /api/account/users/{id}` - Chi tiết người dùng theo ID (Yêu cầu: Admin)
* `PUT /api/account/users/{id}` - Sửa đổi username/role của người dùng (Yêu cầu: Admin)
* `DELETE /api/account/users/{id}` - Xóa tài khoản (Chặn nếu đang trong phiên chơi) (Yêu cầu: Admin)
* `POST /api/account/change-password` - Người dùng tự đổi mật khẩu (Yêu cầu: Đã đăng nhập)
* `POST /api/account/topup` - Nạp tiền vào ví của khách hàng (Yêu cầu: Admin, Staff)
* `GET /api/account/users/{id}/transactions` - Tra cứu lịch sử nạp/rút của tài khoản (Yêu cầu: Admin/Staff hoặc chính Customer sở hữu)

### 3. Quản lý Máy trạm và Khu vực (Computers & Zones)
* `GET /api/zones` - Xem toàn bộ thông tin phòng máy và bảng giá giờ chơi
* `GET /api/zones/{id}` - Xem chi tiết một khu vực phòng máy kèm danh sách máy
* `POST /api/zones` - Thiết lập khu vực phòng máy mới (Yêu cầu: Admin)
* `PUT /api/zones/{id}` - Cập nhật tên hoặc giá tiền của khu vực phòng máy (Yêu cầu: Admin)
* `DELETE /api/zones/{id}` - Xóa khu vực (Chặn nếu vẫn còn chứa máy) (Yêu cầu: Admin)
* `GET /api/computers` - Liệt kê tất cả máy trạm cùng trạng thái hiện tại

### Điều khiển Phiên chơi (Booking Services)
* `POST /api/booking/start` - Khởi động phiên chơi máy trạm (Body JSON: `{ UserId, ComputerId }`)
* `POST /api/booking/end` - Đóng phiên chơi máy trạm và kết xuất hóa đơn (Body JSON: `{ UserId, ComputerId }`)

---

## Bản quyền và Giấy phép
Dự án được phân phối chính thức theo giấy phép MIT. Để biết thêm chi tiết, xin vui lòng tham khảo tệp `LICENSE` đính kèm.
