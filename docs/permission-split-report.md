# Báo cáo tách quyền Xem / Sửa / Xoá — `kindi-api`

Nhánh: `feat/permission-split-view-edit-delete` (tách từ `production`). **Chưa merge `production`.**

## 1. Mục tiêu và kết quả

- Tách quyền theo cấu trúc **Nhóm (Module) → Màn hình (Screen) → hành động (View/Update/Delete)**.
- Thay các mã gộp kiểu “Sửa / xoá …” bằng bộ mã xem/sửa/xoá riêng cho từng màn hình, **giữ nguyên số đã phát hành**;
  mã mới cấp từ **P115** trở đi. Thêm `PermissionKind.Update = 3` và `PermissionKind.Delete = 4`.
- Ánh xạ quyền đang cấp sang mã mới **idempotent**, **không ai mất quyền** (kể cả quyền tắt riêng của tài khoản).
- Chấp nhận **cả mã cũ lẫn mã mới** ở mọi endpoint đã tách (phương án chuyển tiếp (a)) → **không cần đăng nhập lại sau deploy**.

Kết quả kiểm chứng: `dotnet build` exit 0, `dotnet test` exit 0 — **31/31 test pass** (24 test cũ + 7 test mới).

## 2. Cấu trúc danh mục cho UI

- `PermissionInfoAttribute` thêm `Screen` (mã màn hình) và `Replaces` (mã gộp cũ bị thay thế).
- `PermissionScreens` + `PermissionScreenCatalog` (`Domain/Enums/PermissionScreens.cs`): danh mục màn hình kèm tên hiển thị.
- `PermissionDefinition` mang thêm `Screen`, `ScreenName`, `Replaces`.
- API `GET /api/v1/permissions` (ma trận quyền) trả thêm `screen`, `screenName` cho từng quyền → UI gom nhóm
  **Module → Screen → Kind**. Mọi quyền đều đã khai `Screen` (có test bắt buộc).

## 3. Bảng mã cũ → mã mới

| Mã cũ (gộp) | Mã mới | Nhóm | Màn hình | Hành động | Endpoint gác (thay đổi) |
|---|---|---|---|---|---|
| P027 Xem hồ sơ CTV đã xoá / khôi phục | **P115** | User | Collaborators | View | `GET /collaborators/deleted`, `POST /collaborators/{id}/restore` |
| P045 (phần khôi phục) | **P116** | Partner | Partners | Delete | `POST /partners/{id}/restore` |
| P046 (phần xoá) | **P118** | Partner | PartnerProducts | Delete | `DELETE /partners/{id}/products/{productId}` |
| P065 (phần khôi phục) | **P119** | Purchase | Offers | Delete | `POST /offerrequests/{id}/restore` |
| P067 (phần xoá) | **P120** | Purchase | GroupBuying | Delete | `DELETE /groupbuyingrequests/{id}`, `.../participants/{id}` |
| P071 (phần xoá) | **P121** | Group | Groups | Delete | `DELETE /businessgroups/{id}` |
| P106 (phần xoá) | **P124** | SuperAdmin | CommissionConfig | Delete | `DELETE /commissions/{id}` |
| P109 (phần xem) | **P125** | SuperAdmin | MembershipTiers | View | `GET /membership-tiers` |
| P109 (phần sửa) | **P126** | SuperAdmin | MembershipTiers | Update | `POST/PUT /membership-tiers`, `POST /membership-tiers/evaluate` |
| P109 (phần xoá) | **P127** | SuperAdmin | MembershipTiers | Delete | `DELETE /membership-tiers/{id}` |
| P110 (phần xem) | **P128** | SuperAdmin | BankAccounts | View | `GET /bank-accounts` |
| P114 (phần xem) | **P129** | SuperAdmin | RevenueConfig | View | `GET /RevenueExpenseTypes`, `GET /RevenueExpenseTypes/config` |
| P114 (phần sửa) | **P130** | SuperAdmin | RevenueConfig | Update | `POST/PUT /RevenueExpenseTypes`, `PUT /assign`, `PUT /config` |
| P114 (phần xoá) | **P131** | SuperAdmin | RevenueConfig | Delete | `DELETE /RevenueExpenseTypes/{id}` |

Ngoài ra nhiều mã **giữ nguyên số nhưng thu hẹp phạm vi** (bỏ endpoint đã tách) và sửa Kind:
P026→Delete, P044→Update, P045→Delete (chỉ DELETE), P046→Update (POST+PUT), P047→View, P048→Update,
P065→Delete, P067→Update, P071→Update, P073→View, P101→Update, P106→Update, P110→Update, P111→Update.

> **P125 khai `Replaces = { P109, P107 }`**: quyền xem hạng thành viên vốn gác bằng P107 (xem chi trả) nên
> người có P107 cũng được cấp P125; người có P109 cũng được cấp P125/P126/P127.

### Mã KHÔNG tạo trong đợt này (tránh mã mồ côi)

Thiết kế khảo sát có đề xuất P117 (xem sản phẩm đối tác), P122/P123 (sửa/xoá bài đăng admin), P132 (xoá bản khai
doanh thu). Các mã này **không có endpoint thật** trong repo (không có `GET /partners/{id}/products`; `PUT/DELETE
/social/posts/{id}` là endpoint người dùng, không phải admin; không có `DELETE /revenues/{id}`) nên **không tạo** để
tránh dòng quyền mồ côi. Khi bổ sung endpoint tương ứng thì thêm mã và khai `Replaces` tương tự.

## 4. Ai được gì sau ánh xạ (không ai mất quyền)

1. **Admin thường**: tự nhận mọi mã mới có `Module != SuperAdmin` (P115–P121) qua bước gán mặc định của seeder —
   đây là lưới an toàn chống sự cố “admin mất màn hình mới”.
2. **Mã nhóm SuperAdmin (P124–P131)**: **không** cấp mặc định; seeder sao chép từ mã gộp cũ theo `Replaces`:
   - Ai đang được cấp P106 → nhận P124; P109 → P125/P126/P127; P110 → P128; P114 → P129/P130/P131.
   - Áp dụng cho **cả** dòng `RolePermissions` **và** `UserPermissions` (cấp riêng từng tài khoản).
   - Sao chép **cả dòng tắt quyền** (`IsGranted = false`) — quyền bị tắt riêng vẫn bị tắt ở mã mới (không tự bật lại).
   - **Idempotent**: chỉ thêm dòng thiếu/khôi phục dòng xoá mềm, không ghi đè cấu hình đã có; chạy lại vô hại.
3. **SuperAdmin**: bypass toàn bộ handler → không bao giờ mất màn hình.
4. **View cấp theo quyền cũ, không tràn lan**: mã xem mới (P115/P125/P128/P129) chỉ đến từ quyền cũ tương ứng
   hoặc từ mặc định Admin cho nhóm nghiệp vụ; tài khoản Người dùng/Đối tác **không** được cấp thêm (có test chặn).

Bằng chứng tự động:
- `Seeder_chuyen_quyen_da_cap_sang_ma_moi_va_khong_lam_mat_quyen` — chạy seeder thật (EF InMemory):
  giả lập Admin có P106/P109/P110/P114 và một tài khoản bị **tắt** P109 → sau seed, Admin có P124–P131,
  tài khoản có P125/P126/P127 ở trạng thái tắt; chạy lại seeder số dòng **không đổi** (idempotent).
- `Ma_xem_moi_khong_duoc_cap_mac_dinh_tran_lan_cho_thanh_vien`.
- `Ma_tach_moi_khong_mo_coi` (mọi mã mới phải gác endpoint thật).

## 5. Token cũ — có cần đăng nhập lại không?

**Không bắt buộc đăng nhập lại. Áp dụng phương án (a).**

- Mọi endpoint đã tách khai `[HasPermission(mãMới, mãCũ)]`; handler cho qua khi có **một** mã → token đang đăng nhập
  (mang mã cũ) vẫn gọi được trong suốt thời gian chuyển tiếp. Ví dụ: `[HasPermission(RestorePartner, DeletePartner)]`.
- Có test `Endpoint_dung_ma_moi_van_chap_nhan_ca_ma_cu_chuyen_tiep` quét **toàn bộ controller** để bắt buộc
  mọi endpoint dùng mã mới đều còn nhận mã cũ tương ứng — chặn tái diễn tình trạng “đá người dùng ra sau deploy”.
- Sau khi token cũ hết hạn hẳn (> `JwtSettings:ExpiryMinutes` = 60 phút; khuyến nghị ≥ 1–2 ngày) và UI đã phát hành,
  release kế tiếp **bỏ mã cũ** khỏi attribute (các mã mồ côi/giữ nhịp: P027, P109, P114; cân nhắc P022, P102).

## 6. Rủi ro và giảm thiểu

| # | Rủi ro | Mức | Giảm thiểu đã áp dụng |
|---|---|---|---|
| R1 | Admin mất màn hình mới | Cao | Mã mới nhóm nghiệp vụ được Admin tự nhận; mã nhóm SuperAdmin sao chép qua `Replaces`; test seeder xác nhận |
| R2 | User/Partner mất quyền cấp riêng | Cao | Sao chép cả `UserPermissions` (bật và tắt) |
| R3 | Phiên đang đăng nhập bị chặn do token còn mã cũ | Cao | `[HasPermission(mới, cũ)]` + test quét toàn bộ endpoint |
| R4 | Quyền tắt riêng bị mất hiệu lực | Trung | Sao chép cả `IsGranted = false` |
| R5 | Trùng khoá unique khi thêm dòng (bảng có dòng xoá mềm) | Trung | Seeder tái dùng/khôi phục dòng cũ (`IgnoreQueryFilters`) |
| R6 | Mã mồ côi / Kind sai gây nhiễu ma trận | Thấp | Bỏ mã không có endpoint; sửa Kind P047/P073; test “không mồ côi” |
| R7 | Cache quyền 10 phút | Thấp | Process mới sau deploy có cache trống |

## 7. File thay đổi

- `src/Kindi.API.Domain/Enums/PermissionModule.cs` — thêm `PermissionKind.Update`/`Delete`.
- `src/Kindi.API.Domain/Enums/PermissionScreens.cs` — **mới**: danh mục màn hình.
- `src/Kindi.API.Domain/Enums/PermissionCode.cs` — thêm `Screen`/`Replaces`, mã P115–P131, sửa Kind, thu hẹp mã gộp.
- `src/Kindi.API.Domain/Enums/PermissionCatalog.cs` — `Screen`/`ScreenName`/`Replaces` + `Replacements`.
- `src/Kindi.API.Domain/Attributes/PermissionInfoAttribute.cs` — thêm `Screen`, `Replaces`.
- `src/Kindi.API.Infrastructure/Data/PermissionSeeder.cs` — sao chép quyền cũ → mới (idempotent).
- `src/Kindi.API.Application/DTOs/responses/PermissionResponses.cs` — thêm `Screen`/`ScreenName`.
- `src/Kindi.API.WebApi/Authorization/HasPermissionAttribute.cs` — phơi `PermissionCodes` (cho test).
- `src/Kindi.API.WebApi/Controllers/**` — gắn lại endpoint sang mã mới (kèm mã cũ):
  Partners, OfferRequests, GroupBuyingRequests, BusinessGroups, Collaborators, MembershipTiers,
  BankAccounts, Commissions, RevenueExpenseTypes, PermissionsController (ma trận).
- `tests/Kindi.API.UnitTests/PermissionSplitTests.cs` — **mới**: 7 test chốt cấu trúc + ánh xạ + chuyển tiếp + seeder.
- `tests/Kindi.API.UnitTests/Kindi.API.UnitTests.csproj` — thêm `Microsoft.EntityFrameworkCore.InMemory`.

## 8. Ghi chú triển khai

- **Không có thay đổi schema DB.** Bảng `Permissions`/`RolePermissions`/`UserPermissions` giữ nguyên; việc
  “migration/backfill” được thực hiện **idempotent trong `PermissionSeeder`** (chạy sau `ApplyMigrationsAsync`
  mỗi lần khởi động) vì migration chạy trước seeder không thể tham chiếu Guid quyền mới.
- Không kết nối/migrate DB production trong quá trình này; test seeder chạy trên EF InMemory.
- UI (`kindi-web`) cần cập nhật mã mới ở route/menu/guard trong đợt deploy kế tiếp (repo này không bao gồm).
