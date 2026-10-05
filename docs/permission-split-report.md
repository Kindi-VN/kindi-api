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

---

# Đợt bổ sung — Cây quyền đệ quy, kế thừa, phân quyền khôi phục & endpoint còn thiếu

Nhánh: `feat/api-batch-2026-10-04` (commit chồng lên, **chưa push**).

## 9. Cây quyền đệ quy bằng self-reference

- `Permissions.ParentCode` **đổi từ FK trỏ `PermissionGroups.Code` sang TỰ THAM CHIẾU `Permissions.Code`** (cây 3 tầng,
  sẵn sàng sâu hơn): `quản trị (group)` → `Màn hình người dùng (screen)` → `hành động (P###)`.
- `Permission` thêm `NodeKind` (`1 group | 2 screen | 3 action`) và `NameKey` (khoá dịch để UI tra resource).
- **Tái dùng `PermissionScreens.cs`/`PermissionScreenCatalog`** làm tầng màn hình — không tạo danh mục màn hình mới.
  Mỗi màn hình là một node `Permissions` có mã riêng (ví dụ `OFFERS`, `PARTNERS`), hành động trỏ `ParentCode` = mã màn hình.
- `GET /api/v1/permissions/tree?role=<int>` trả **cây đệ quy**; mỗi node:
  `{ code, nameKey, kind (group|screen|action), parentCode, isGranted, isEffective, children: [] }`.
  - `nameKey` dùng khoá đã seed (`PermissionGroup_<MÃ>`, `PermissionScreen_<MÃ>`, `Permission_<P###>`), bản dịch nằm ở
    `SharedResource.vi/en.resx`.
  - `isGranted` = node đang được tick trực tiếp; `isEffective` = còn hiệu lực sau kế thừa.
  - Tổng số node thật trả về: **132** = 3 nhóm + 29 màn hình + 100 hành động. `role` bỏ trống = Admin (3).

### Kế thừa quyền (mới — chốt bởi chủ dự án)

- **Hiệu lực = bản thân được cấp VÀ mọi tổ tiên đều được cấp.** Tắt màn hình ⇒ mọi hành động trong màn hình mất hiệu lực
  (dù dữ liệu gán vẫn còn tick); tắt nhóm ⇒ toàn bộ màn hình con tắt theo (đệ quy nhiều tầng).
- Enforce **ở server**, không phụ thuộc UI:
  - `PermissionTreeCatalog.ApplyInheritance(granted)` — phép tính **thuần trên danh mục tĩnh** (cha–con khai trong code)
    nên không query DB khi kiểm.
  - `PermissionAuthorizationHandler` (chỗ `[HasPermission]` resolve) áp `ApplyInheritance` lên claim `perm` trước khi so khớp.
  - `PermissionService.GetUserPermissionsAsync` cũng trả tập **hiệu lực** để token mang đúng quyền (AuthService/JwtService).
- **Tương thích token cũ:** token phát hành trước khi có cây quyền không mang mã node cha (màn hình/nhóm); handler chỉ áp
  kế thừa khi token **có** mã cấu trúc, nếu không thì giữ nguyên hành vi cũ → phiên đang đăng nhập không bị đá ra khi deploy.

### Quyết định khi LƯU gán quyền (giữ nguyên tập đã lưu)

- `SetRolePermissionsAsync`/`SetUserPermissionsAsync` **lưu đúng tập client gửi lên** — **không tự xoá con khi cha tắt**
  và **không tự bật cha khi con bật**. Kế thừa **chỉ áp khi KIỂM tra**, không áp khi ghi.
- Hệ quả mong muốn: tắt một màn hình rồi bật lại ⇒ các hành động con **trở về đúng trạng thái tick cũ** (không bị mất).
- Bù lại, **seeder** khi thấy role/tài khoản có quyền con mà thiếu node cha sẽ **cấp thêm node cha còn thiếu** (idempotent):
  nếu không, quy tắc “hiệu lực = bản thân + tổ tiên” sẽ chặn hết quyền cũ sau khi nâng cấp.

## 10. Phân quyền khôi phục (ViewRestore / Restore)

Thêm cặp quyền theo đúng pattern có sẵn ở `CollaboratorsController` (`ViewRestoreCollaborator` + `RestoreCollaborator`):
mọi màn có xoá mềm giờ có **`ViewRestore<X>`** (xem tab đã xoá) + **`Restore<X>`** (khôi phục), endpoint dùng
`[HasPermission(mới, cũ)]` một nhịp nên **token cũ vẫn gọi được**.

| Màn | Mã xem đã xoá | Mã khôi phục | Endpoint gác |
|---|---|---|---|
| Partners | **P132** ViewRestorePartner | (dùng lại P116 RestorePartner) | `GET /partners/deleted`, `POST /partners/{id}/restore` |
| Offer (yêu cầu nhận offer) | **P133** ViewRestoreOfferRequest | (P119) | `GET /offerrequests/deleted`, `POST /offerrequests/{id}/restore` |
| Bài đăng (admin) | **P134** ViewRestoreSocialPost | (P097 RestoreSocialPost) | `GET /social/posts/deleted`, `POST /social/posts/{id}/restore` |
| Mua chung | **P135** ViewRestoreGroupBuyingRequest | **P136** RestoreGroupBuyingRequest | `GET /groupbuyingrequests/deleted`, `POST /groupbuyingrequests/{id}/restore` |
| Nhóm | **P137** ViewRestoreGroup | **P138** RestoreGroup | `GET /businessgroups/deleted`, `POST /businessgroups/{id}/restore` |
| Cấu hình hoa hồng | **P139** ViewRestoreCommissionConfig | **P140** RestoreCommissionConfig | `GET /commissions/deleted`, `POST /commissions/{id}/restore` |
| Hạng thành viên | **P141** ViewRestoreMembershipTier | **P142** RestoreMembershipTier | `GET /membership-tiers/deleted`, `POST /membership-tiers/{id}/restore` |
| Loại thu/chi | **P143** ViewRestoreRevenueConfig | **P144** RestoreRevenueConfig | `GET /RevenueExpenseTypes/deleted`, `POST /RevenueExpenseTypes/{id}/restore` |
| Yêu cầu mua hàng | **P146** ViewRestorePurchaseRequest | **P147** RestorePurchaseRequest | `GET /purchaserequests/deleted`, `POST /purchaserequests/{id}/restore` |

Các mã mới thuộc `Module != SuperAdmin` nên **Admin tự nhận mặc định**; SuperAdmin bypass toàn bộ (không cần seed dòng riêng,
giữ nguyên quy ước cũ — `kindi_super` đi qua handler và `GetRolePermissionsAsync(SuperAdmin)` trả toàn bộ mã).

## 11. Endpoint xoá/khôi phục còn thiếu

- **Offer requests** (`OfferRequestsController`): thêm `GET /offerrequests/deleted` (lọc `IncludeDeleted` cho admin) —
  đã có sẵn `DELETE` và `POST /{id}/restore`.
- **Mua chung** (`GroupBuyingRequestsController`): đã có `DELETE` + restore; **bổ sung `GET /groupbuyingrequests/deleted`**.
- **Yêu cầu mua hàng** (`PurchaseRequestsController`) — trước đây **không có** bộ xoá/khôi phục, nay thêm đủ:
  `DELETE /purchaserequests/{id}` (P145), `GET /purchaserequests/deleted` (P146), `POST /purchaserequests/{id}/restore` (P147).
  Service thêm `DeleteAsync`/`RestoreAsync` + `PurchaseRequestQueryDto.IsDeleted` (xem tab đã xoá, `IgnoreQueryFilters`).
- **Nhóm / hoa hồng / hạng thành viên / loại thu-chi / đối tác**: bổ sung `GET .../deleted` (trước đó chỉ có restore).
- Chuẩn tìm kiếm giữ nguyên **ILike + Unaccent**; soft-delete dùng đúng global filter + `IgnoreQueryFilters` toàn hệ.

## 12. Migration (data-preserving)

`src/Kindi.API.Infrastructure/Migrations/20261004131552_RecursivePermissionTreeAndRestorePermissions.cs`

- Bỏ FK `FK_Permissions_PermissionGroups_ParentCode`, bỏ `AK_PermissionGroups_Code`; `Code` nới `varchar(8) → varchar(50)`.
- Thêm cột `NameKey` (nullable) + `NodeKind` (default 3 = action).
- **Backfill dữ liệu TRƯỚC khi thêm FK tự tham chiếu**: insert node nhóm (ADMIN/MEMBER/SHARED) + 29 node màn hình
  (idempotent `ON CONFLICT DO NOTHING`), rồi `UPDATE` `ParentCode` của 100 hành động về mã màn hình tương ứng
  (`USERS`, `OFFERS`, …) + đặt `NodeKind=3`, `NameKey='Permission_<P###>'`. **Giữ nguyên mọi mã quyền cũ và mọi gán
  quyền role/user** (không xoá/không đổi Id dòng quyền hành động).
- Sau đó mới thêm `AK_Permissions_Code` + FK `FK_Permissions_Permissions_ParentCode` (self-FK, `Restrict`).
- `Down` xoá node group/screen đã seed và trả FK về `PermissionGroups`.

Seeder chạy sau migration tiếp tục đồng bộ `Name`/`NameKey`/`NodeKind`/`ParentCode` theo danh mục trong code (idempotent).

## 13. Bằng chứng build/test

- `dotnet build Kindi.API.slnx` → **exit 0**.
- `dotnet test tests/Kindi.API.UnitTests` → **47/47 pass** (0 fail, 0 skip); không test nào cũ bị đỏ.
- Shape JSON `/permissions/tree` (chụp từ code thật qua harness InMemory, đã camelCase như cấu hình Web):
  xem mẫu ở mục 9 / phần báo cáo kèm theo — minh hoạ kế thừa: node `P040` (`isGranted: true`) nằm dưới màn hình
  `PARTNERS` (`isGranted: false`) nên có `isEffective: false`.

## 14. File thay đổi (đợt bổ sung)

- `Domain/Entities/Permission.cs`, `Infrastructure/Data/Configurations/PermissionConfiguration.cs` — self-reference + NodeKind/NameKey.
- `Domain/Enums/PermissionModule.cs` — `PermissionNodeKind`; `PermissionScreens.cs` — mã màn hình; `PermissionTreeCatalog.cs` — **mới** (cây tĩnh + kế thừa).
- `Domain/Enums/PermissionCatalog.cs`, `PermissionCode.cs` — mã mới P132–P147 + nameKey.
- `Infrastructure/Data/PermissionSeeder.cs` — seed 3 tầng + cấp node cha (kế thừa) + chuyển tiếp.
- `Infrastructure/Migrations/20261004131552_RecursivePermissionTreeAndRestorePermissions.*` — **mới** (data-preserving).
- `Application/Services/PermissionService.cs`, `Common/Interfaces/IPermissionService.cs` — `GetTreeAsync`, raw-vs-effective, Normalize node nhóm/màn hình.
- `Application/DTOs/responses/PermissionResponses.cs` — `PermissionTreeNodeResponse` (+`isGranted`/`isEffective`).
- `WebApi/Authorization/PermissionAuthorizationHandler.cs` — enforce kế thừa.
- `WebApi/Controllers/Admin/PermissionsController.cs` — endpoint `tree`, ma trận `parentCode` = nhóm.
- `WebApi/Controllers/v1/*` — Partners, OfferRequests, GroupBuyingRequests, BusinessGroups, Commissions, MembershipTiers, RevenueExpenseTypes, Social, PurchaseRequests.
- `Application/Services/PurchaseRequestService.cs`, `Common/Interfaces/IPurchaseRequestService.cs`, `DTOs/requests/PurchaseRequestQueryDto.cs` — delete/restore/paged-deleted.
- `Application/Resources/SharedResource.vi.resx`, `SharedResource.en.resx` — 132 khoá `nameKey` + `PurchaseRequest_DeleteSuccess`.
