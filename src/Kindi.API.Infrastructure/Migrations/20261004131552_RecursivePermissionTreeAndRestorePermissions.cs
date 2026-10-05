using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kindi.API.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RecursivePermissionTreeAndRestorePermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Permissions_PermissionGroups_ParentCode",
                table: "Permissions");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_PermissionGroups_Code",
                table: "PermissionGroups");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Permissions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(8)",
                oldMaxLength: 8);

            migrationBuilder.AddColumn<string>(
                name: "NameKey",
                table: "Permissions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NodeKind",
                table: "Permissions",
                type: "integer",
                nullable: false,
                defaultValue: 3);

            // Data-preserving: seed node NHÓM + MÀN HÌNH rồi trỏ ParentCode của hành động về màn hình
            // TRƯỚC khi thêm FK tự tham chiếu (nếu không, FK sẽ vi phạm vì ParentCode đang là mã nhóm cũ).
            migrationBuilder.Sql(@"
INSERT INTO ""Permissions"" (""Id"",""Code"",""Name"",""NameKey"",""NodeKind"",""Module"",""ParentCode"",""Kind"",""Route"",""Endpoints"",""SortOrder"",""CreatedAt"",""IsDeleted"") VALUES ('e8587516-44b0-4014-b391-73cb70154579','ADMIN','Hệ thống quản trị','PermissionGroup_ADMIN',1,1,NULL,2,NULL,NULL,10,NOW(),FALSE) ON CONFLICT (""Code"") DO NOTHING;
INSERT INTO ""Permissions"" (""Id"",""Code"",""Name"",""NameKey"",""NodeKind"",""Module"",""ParentCode"",""Kind"",""Route"",""Endpoints"",""SortOrder"",""CreatedAt"",""IsDeleted"") VALUES ('4f2fbedb-3657-4627-bb3a-b09994581d91','MEMBER','Khu vực thành viên','PermissionGroup_MEMBER',1,1,NULL,2,NULL,NULL,20,NOW(),FALSE) ON CONFLICT (""Code"") DO NOTHING;
INSERT INTO ""Permissions"" (""Id"",""Code"",""Name"",""NameKey"",""NodeKind"",""Module"",""ParentCode"",""Kind"",""Route"",""Endpoints"",""SortOrder"",""CreatedAt"",""IsDeleted"") VALUES ('1d613571-3a99-4937-9b2c-57a38268ba5b','SHARED','Dùng chung','PermissionGroup_SHARED',1,1,NULL,2,NULL,NULL,40,NOW(),FALSE) ON CONFLICT (""Code"") DO NOTHING;
INSERT INTO ""Permissions"" (""Id"",""Code"",""Name"",""NameKey"",""NodeKind"",""Module"",""ParentCode"",""Kind"",""Route"",""Endpoints"",""SortOrder"",""CreatedAt"",""IsDeleted"") VALUES ('4dc97032-ec03-4208-9bb7-bc35841f115e','DASHBOARD','Bảng điều khiển','PermissionScreen_DASHBOARD',2,1,'ADMIN',2,NULL,NULL,10,NOW(),FALSE) ON CONFLICT (""Code"") DO NOTHING;
INSERT INTO ""Permissions"" (""Id"",""Code"",""Name"",""NameKey"",""NodeKind"",""Module"",""ParentCode"",""Kind"",""Route"",""Endpoints"",""SortOrder"",""CreatedAt"",""IsDeleted"") VALUES ('2af56e48-53f9-4e10-98d6-fbafe21d4fea','CRM_DASHBOARD','Dashboard CRM','PermissionScreen_CRM_DASHBOARD',2,1,'ADMIN',2,NULL,NULL,20,NOW(),FALSE) ON CONFLICT (""Code"") DO NOTHING;
INSERT INTO ""Permissions"" (""Id"",""Code"",""Name"",""NameKey"",""NodeKind"",""Module"",""ParentCode"",""Kind"",""Route"",""Endpoints"",""SortOrder"",""CreatedAt"",""IsDeleted"") VALUES ('6be8fcd6-4c0b-4af2-9deb-91e16c57278f','REPORTS','Báo cáo','PermissionScreen_REPORTS',2,1,'ADMIN',2,NULL,NULL,30,NOW(),FALSE) ON CONFLICT (""Code"") DO NOTHING;
INSERT INTO ""Permissions"" (""Id"",""Code"",""Name"",""NameKey"",""NodeKind"",""Module"",""ParentCode"",""Kind"",""Route"",""Endpoints"",""SortOrder"",""CreatedAt"",""IsDeleted"") VALUES ('6d21c38b-a831-4a2f-a752-32f041264f3a','AUDIT_LOGS','Nhật ký hệ thống','PermissionScreen_AUDIT_LOGS',2,1,'ADMIN',2,NULL,NULL,40,NOW(),FALSE) ON CONFLICT (""Code"") DO NOTHING;
INSERT INTO ""Permissions"" (""Id"",""Code"",""Name"",""NameKey"",""NodeKind"",""Module"",""ParentCode"",""Kind"",""Route"",""Endpoints"",""SortOrder"",""CreatedAt"",""IsDeleted"") VALUES ('64ed8039-4222-40c0-8ad7-93ac7bef5e29','SYSTEM_SETTINGS','Cấu hình hệ thống','PermissionScreen_SYSTEM_SETTINGS',2,1,'ADMIN',2,NULL,NULL,50,NOW(),FALSE) ON CONFLICT (""Code"") DO NOTHING;
INSERT INTO ""Permissions"" (""Id"",""Code"",""Name"",""NameKey"",""NodeKind"",""Module"",""ParentCode"",""Kind"",""Route"",""Endpoints"",""SortOrder"",""CreatedAt"",""IsDeleted"") VALUES ('f6d94bb3-42af-4e52-a1fc-5f77a0c1fce8','REFERRAL_STATS','Thống kê giới thiệu','PermissionScreen_REFERRAL_STATS',2,1,'ADMIN',2,NULL,NULL,60,NOW(),FALSE) ON CONFLICT (""Code"") DO NOTHING;
INSERT INTO ""Permissions"" (""Id"",""Code"",""Name"",""NameKey"",""NodeKind"",""Module"",""ParentCode"",""Kind"",""Route"",""Endpoints"",""SortOrder"",""CreatedAt"",""IsDeleted"") VALUES ('c0e303b8-9aa2-4a46-8e90-7e939091b577','USERS','Người dùng','PermissionScreen_USERS',2,1,'ADMIN',2,NULL,NULL,100,NOW(),FALSE) ON CONFLICT (""Code"") DO NOTHING;
INSERT INTO ""Permissions"" (""Id"",""Code"",""Name"",""NameKey"",""NodeKind"",""Module"",""ParentCode"",""Kind"",""Route"",""Endpoints"",""SortOrder"",""CreatedAt"",""IsDeleted"") VALUES ('87e73d70-15c6-4a8a-917b-8e72973566ec','COLLABORATORS','Hồ sơ cộng tác viên','PermissionScreen_COLLABORATORS',2,1,'ADMIN',2,NULL,NULL,110,NOW(),FALSE) ON CONFLICT (""Code"") DO NOTHING;
INSERT INTO ""Permissions"" (""Id"",""Code"",""Name"",""NameKey"",""NodeKind"",""Module"",""ParentCode"",""Kind"",""Route"",""Endpoints"",""SortOrder"",""CreatedAt"",""IsDeleted"") VALUES ('2f5abe1f-4b2a-4440-a17f-cfc68cc140d3','PARTNERS','Đối tác','PermissionScreen_PARTNERS',2,1,'ADMIN',2,NULL,NULL,120,NOW(),FALSE) ON CONFLICT (""Code"") DO NOTHING;
INSERT INTO ""Permissions"" (""Id"",""Code"",""Name"",""NameKey"",""NodeKind"",""Module"",""ParentCode"",""Kind"",""Route"",""Endpoints"",""SortOrder"",""CreatedAt"",""IsDeleted"") VALUES ('d18bf517-996a-4a42-b43d-f115dd50b0c1','PARTNER_PRODUCTS','Sản phẩm đối tác','PermissionScreen_PARTNER_PRODUCTS',2,1,'ADMIN',2,NULL,NULL,130,NOW(),FALSE) ON CONFLICT (""Code"") DO NOTHING;
INSERT INTO ""Permissions"" (""Id"",""Code"",""Name"",""NameKey"",""NodeKind"",""Module"",""ParentCode"",""Kind"",""Route"",""Endpoints"",""SortOrder"",""CreatedAt"",""IsDeleted"") VALUES ('0a7b5207-28b9-4d9d-9e00-b5e6c3910f0f','COMPANIES','Công ty','PermissionScreen_COMPANIES',2,1,'ADMIN',2,NULL,NULL,140,NOW(),FALSE) ON CONFLICT (""Code"") DO NOTHING;
INSERT INTO ""Permissions"" (""Id"",""Code"",""Name"",""NameKey"",""NodeKind"",""Module"",""ParentCode"",""Kind"",""Route"",""Endpoints"",""SortOrder"",""CreatedAt"",""IsDeleted"") VALUES ('888797aa-c66d-409f-9af1-a993707066a6','PURCHASE_REQUESTS','Yêu cầu mua hàng','PermissionScreen_PURCHASE_REQUESTS',2,1,'ADMIN',2,NULL,NULL,150,NOW(),FALSE) ON CONFLICT (""Code"") DO NOTHING;
INSERT INTO ""Permissions"" (""Id"",""Code"",""Name"",""NameKey"",""NodeKind"",""Module"",""ParentCode"",""Kind"",""Route"",""Endpoints"",""SortOrder"",""CreatedAt"",""IsDeleted"") VALUES ('d38bbed5-f29f-44c5-a618-916775b6e30e','OFFERS','Offer','PermissionScreen_OFFERS',2,1,'ADMIN',2,NULL,NULL,160,NOW(),FALSE) ON CONFLICT (""Code"") DO NOTHING;
INSERT INTO ""Permissions"" (""Id"",""Code"",""Name"",""NameKey"",""NodeKind"",""Module"",""ParentCode"",""Kind"",""Route"",""Endpoints"",""SortOrder"",""CreatedAt"",""IsDeleted"") VALUES ('41628f62-7812-4281-a062-c6485479c5a1','GROUP_BUYING','Yêu cầu mua chung','PermissionScreen_GROUP_BUYING',2,1,'ADMIN',2,NULL,NULL,170,NOW(),FALSE) ON CONFLICT (""Code"") DO NOTHING;
INSERT INTO ""Permissions"" (""Id"",""Code"",""Name"",""NameKey"",""NodeKind"",""Module"",""ParentCode"",""Kind"",""Route"",""Endpoints"",""SortOrder"",""CreatedAt"",""IsDeleted"") VALUES ('f27ff155-5638-460b-8734-4f6c7f7d9321','GROUPS','Nhóm','PermissionScreen_GROUPS',2,1,'ADMIN',2,NULL,NULL,180,NOW(),FALSE) ON CONFLICT (""Code"") DO NOTHING;
INSERT INTO ""Permissions"" (""Id"",""Code"",""Name"",""NameKey"",""NodeKind"",""Module"",""ParentCode"",""Kind"",""Route"",""Endpoints"",""SortOrder"",""CreatedAt"",""IsDeleted"") VALUES ('f72f8cb3-c76c-4bca-be37-0a69d52e3e52','SOCIAL_POSTS','Bài đăng','PermissionScreen_SOCIAL_POSTS',2,1,'ADMIN',2,NULL,NULL,190,NOW(),FALSE) ON CONFLICT (""Code"") DO NOTHING;
INSERT INTO ""Permissions"" (""Id"",""Code"",""Name"",""NameKey"",""NodeKind"",""Module"",""ParentCode"",""Kind"",""Route"",""Endpoints"",""SortOrder"",""CreatedAt"",""IsDeleted"") VALUES ('512bb470-7a51-48e3-9373-f97fc646a98a','PERMISSION_MATRIX','Phân quyền','PermissionScreen_PERMISSION_MATRIX',2,1,'ADMIN',2,NULL,NULL,200,NOW(),FALSE) ON CONFLICT (""Code"") DO NOTHING;
INSERT INTO ""Permissions"" (""Id"",""Code"",""Name"",""NameKey"",""NodeKind"",""Module"",""ParentCode"",""Kind"",""Route"",""Endpoints"",""SortOrder"",""CreatedAt"",""IsDeleted"") VALUES ('1081ca20-5e79-4405-93a5-09621cdcbf6e','COMMISSION_CONFIG','Cấu hình hoa hồng','PermissionScreen_COMMISSION_CONFIG',2,1,'ADMIN',2,NULL,NULL,210,NOW(),FALSE) ON CONFLICT (""Code"") DO NOTHING;
INSERT INTO ""Permissions"" (""Id"",""Code"",""Name"",""NameKey"",""NodeKind"",""Module"",""ParentCode"",""Kind"",""Route"",""Endpoints"",""SortOrder"",""CreatedAt"",""IsDeleted"") VALUES ('e136c9dd-939b-45ce-9b43-f591d38e6377','PAYOUTS','Chi trả & giải ngân','PermissionScreen_PAYOUTS',2,1,'ADMIN',2,NULL,NULL,220,NOW(),FALSE) ON CONFLICT (""Code"") DO NOTHING;
INSERT INTO ""Permissions"" (""Id"",""Code"",""Name"",""NameKey"",""NodeKind"",""Module"",""ParentCode"",""Kind"",""Route"",""Endpoints"",""SortOrder"",""CreatedAt"",""IsDeleted"") VALUES ('e5e00c83-e363-41a3-bb22-c37712ceafc2','MEMBERSHIP_TIERS','Hạng thành viên','PermissionScreen_MEMBERSHIP_TIERS',2,1,'ADMIN',2,NULL,NULL,230,NOW(),FALSE) ON CONFLICT (""Code"") DO NOTHING;
INSERT INTO ""Permissions"" (""Id"",""Code"",""Name"",""NameKey"",""NodeKind"",""Module"",""ParentCode"",""Kind"",""Route"",""Endpoints"",""SortOrder"",""CreatedAt"",""IsDeleted"") VALUES ('1dc5c55c-9bf0-41c5-89f3-fe65a4e855b1','BANK_ACCOUNTS','Tài khoản ngân hàng','PermissionScreen_BANK_ACCOUNTS',2,1,'ADMIN',2,NULL,NULL,240,NOW(),FALSE) ON CONFLICT (""Code"") DO NOTHING;
INSERT INTO ""Permissions"" (""Id"",""Code"",""Name"",""NameKey"",""NodeKind"",""Module"",""ParentCode"",""Kind"",""Route"",""Endpoints"",""SortOrder"",""CreatedAt"",""IsDeleted"") VALUES ('fd2f3d0d-6c54-4537-b366-822ba53b0c1f','REVENUES','Doanh thu giao dịch','PermissionScreen_REVENUES',2,1,'ADMIN',2,NULL,NULL,250,NOW(),FALSE) ON CONFLICT (""Code"") DO NOTHING;
INSERT INTO ""Permissions"" (""Id"",""Code"",""Name"",""NameKey"",""NodeKind"",""Module"",""ParentCode"",""Kind"",""Route"",""Endpoints"",""SortOrder"",""CreatedAt"",""IsDeleted"") VALUES ('fb28f0a4-dee4-418c-aad0-c6cc9a9bfb35','REVENUE_CONFIG','Cấu hình doanh thu','PermissionScreen_REVENUE_CONFIG',2,1,'ADMIN',2,NULL,NULL,260,NOW(),FALSE) ON CONFLICT (""Code"") DO NOTHING;
INSERT INTO ""Permissions"" (""Id"",""Code"",""Name"",""NameKey"",""NodeKind"",""Module"",""ParentCode"",""Kind"",""Route"",""Endpoints"",""SortOrder"",""CreatedAt"",""IsDeleted"") VALUES ('e9f1cd99-830b-4a64-9910-96c435c1c97e','MY_REFERRAL','Giới thiệu của tôi','PermissionScreen_MY_REFERRAL',2,1,'MEMBER',2,NULL,NULL,300,NOW(),FALSE) ON CONFLICT (""Code"") DO NOTHING;
INSERT INTO ""Permissions"" (""Id"",""Code"",""Name"",""NameKey"",""NodeKind"",""Module"",""ParentCode"",""Kind"",""Route"",""Endpoints"",""SortOrder"",""CreatedAt"",""IsDeleted"") VALUES ('05bf0b81-e52f-438a-b35a-f0fe285119dc','MY_COMMISSION','Hoa hồng của tôi','PermissionScreen_MY_COMMISSION',2,1,'MEMBER',2,NULL,NULL,310,NOW(),FALSE) ON CONFLICT (""Code"") DO NOTHING;
INSERT INTO ""Permissions"" (""Id"",""Code"",""Name"",""NameKey"",""NodeKind"",""Module"",""ParentCode"",""Kind"",""Route"",""Endpoints"",""SortOrder"",""CreatedAt"",""IsDeleted"") VALUES ('6eae74a8-d6c3-4536-ae51-a1e65d96a9bf','MY_GROUP_BUYING','Mua chung của tôi','PermissionScreen_MY_GROUP_BUYING',2,1,'MEMBER',2,NULL,NULL,320,NOW(),FALSE) ON CONFLICT (""Code"") DO NOTHING;
INSERT INTO ""Permissions"" (""Id"",""Code"",""Name"",""NameKey"",""NodeKind"",""Module"",""ParentCode"",""Kind"",""Route"",""Endpoints"",""SortOrder"",""CreatedAt"",""IsDeleted"") VALUES ('db833c19-84a0-44ea-920c-98d5f580aa35','MY_REQUESTS','Yêu cầu của tôi','PermissionScreen_MY_REQUESTS',2,1,'MEMBER',2,NULL,NULL,330,NOW(),FALSE) ON CONFLICT (""Code"") DO NOTHING;
INSERT INTO ""Permissions"" (""Id"",""Code"",""Name"",""NameKey"",""NodeKind"",""Module"",""ParentCode"",""Kind"",""Route"",""Endpoints"",""SortOrder"",""CreatedAt"",""IsDeleted"") VALUES ('38df48a5-e567-4d5d-ad49-98ab2eafb0ca','MY_POSTS','Bài viết của tôi','PermissionScreen_MY_POSTS',2,1,'MEMBER',2,NULL,NULL,340,NOW(),FALSE) ON CONFLICT (""Code"") DO NOTHING;
INSERT INTO ""Permissions"" (""Id"",""Code"",""Name"",""NameKey"",""NodeKind"",""Module"",""ParentCode"",""Kind"",""Route"",""Endpoints"",""SortOrder"",""CreatedAt"",""IsDeleted"") VALUES ('09baed40-fcaa-4609-894a-ecd65b111a05','MY_GROUPS','Nhóm của tôi','PermissionScreen_MY_GROUPS',2,1,'MEMBER',2,NULL,NULL,350,NOW(),FALSE) ON CONFLICT (""Code"") DO NOTHING;
UPDATE ""Permissions"" SET ""ParentCode""='DASHBOARD', ""NodeKind""=3, ""NameKey""='Permission_P001' WHERE ""Code""='P001';
UPDATE ""Permissions"" SET ""ParentCode""='CRM_DASHBOARD', ""NodeKind""=3, ""NameKey""='Permission_P002' WHERE ""Code""='P002';
UPDATE ""Permissions"" SET ""ParentCode""='REPORTS', ""NodeKind""=3, ""NameKey""='Permission_P003' WHERE ""Code""='P003';
UPDATE ""Permissions"" SET ""ParentCode""='REPORTS', ""NodeKind""=3, ""NameKey""='Permission_P004' WHERE ""Code""='P004';
UPDATE ""Permissions"" SET ""ParentCode""='REPORTS', ""NodeKind""=3, ""NameKey""='Permission_P005' WHERE ""Code""='P005';
UPDATE ""Permissions"" SET ""ParentCode""='REPORTS', ""NodeKind""=3, ""NameKey""='Permission_P006' WHERE ""Code""='P006';
UPDATE ""Permissions"" SET ""ParentCode""='REPORTS', ""NodeKind""=3, ""NameKey""='Permission_P007' WHERE ""Code""='P007';
UPDATE ""Permissions"" SET ""ParentCode""='AUDIT_LOGS', ""NodeKind""=3, ""NameKey""='Permission_P008' WHERE ""Code""='P008';
UPDATE ""Permissions"" SET ""ParentCode""='AUDIT_LOGS', ""NodeKind""=3, ""NameKey""='Permission_P009' WHERE ""Code""='P009';
UPDATE ""Permissions"" SET ""ParentCode""='SYSTEM_SETTINGS', ""NodeKind""=3, ""NameKey""='Permission_P010' WHERE ""Code""='P010';
UPDATE ""Permissions"" SET ""ParentCode""='REFERRAL_STATS', ""NodeKind""=3, ""NameKey""='Permission_P011' WHERE ""Code""='P011';
UPDATE ""Permissions"" SET ""ParentCode""='MY_REFERRAL', ""NodeKind""=3, ""NameKey""='Permission_P012' WHERE ""Code""='P012';
UPDATE ""Permissions"" SET ""ParentCode""='USERS', ""NodeKind""=3, ""NameKey""='Permission_P020' WHERE ""Code""='P020';
UPDATE ""Permissions"" SET ""ParentCode""='USERS', ""NodeKind""=3, ""NameKey""='Permission_P021' WHERE ""Code""='P021';
UPDATE ""Permissions"" SET ""ParentCode""='USERS', ""NodeKind""=3, ""NameKey""='Permission_P022' WHERE ""Code""='P022';
UPDATE ""Permissions"" SET ""ParentCode""='COLLABORATORS', ""NodeKind""=3, ""NameKey""='Permission_P023' WHERE ""Code""='P023';
UPDATE ""Permissions"" SET ""ParentCode""='COLLABORATORS', ""NodeKind""=3, ""NameKey""='Permission_P024' WHERE ""Code""='P024';
UPDATE ""Permissions"" SET ""ParentCode""='COLLABORATORS', ""NodeKind""=3, ""NameKey""='Permission_P025' WHERE ""Code""='P025';
UPDATE ""Permissions"" SET ""ParentCode""='COLLABORATORS', ""NodeKind""=3, ""NameKey""='Permission_P026' WHERE ""Code""='P026';
UPDATE ""Permissions"" SET ""ParentCode""='COLLABORATORS', ""NodeKind""=3, ""NameKey""='Permission_P027' WHERE ""Code""='P027';
UPDATE ""Permissions"" SET ""ParentCode""='MY_GROUP_BUYING', ""NodeKind""=3, ""NameKey""='Permission_P028' WHERE ""Code""='P028';
UPDATE ""Permissions"" SET ""ParentCode""='MY_REQUESTS', ""NodeKind""=3, ""NameKey""='Permission_P029' WHERE ""Code""='P029';
UPDATE ""Permissions"" SET ""ParentCode""='MY_POSTS', ""NodeKind""=3, ""NameKey""='Permission_P030' WHERE ""Code""='P030';
UPDATE ""Permissions"" SET ""ParentCode""='MY_GROUPS', ""NodeKind""=3, ""NameKey""='Permission_P031' WHERE ""Code""='P031';
UPDATE ""Permissions"" SET ""ParentCode""='MY_COMMISSION', ""NodeKind""=3, ""NameKey""='Permission_P013' WHERE ""Code""='P013';
UPDATE ""Permissions"" SET ""ParentCode""='PARTNERS', ""NodeKind""=3, ""NameKey""='Permission_P040' WHERE ""Code""='P040';
UPDATE ""Permissions"" SET ""ParentCode""='PARTNERS', ""NodeKind""=3, ""NameKey""='Permission_P041' WHERE ""Code""='P041';
UPDATE ""Permissions"" SET ""ParentCode""='PARTNERS', ""NodeKind""=3, ""NameKey""='Permission_P042' WHERE ""Code""='P042';
UPDATE ""Permissions"" SET ""ParentCode""='PARTNERS', ""NodeKind""=3, ""NameKey""='Permission_P043' WHERE ""Code""='P043';
UPDATE ""Permissions"" SET ""ParentCode""='PARTNERS', ""NodeKind""=3, ""NameKey""='Permission_P044' WHERE ""Code""='P044';
UPDATE ""Permissions"" SET ""ParentCode""='PARTNERS', ""NodeKind""=3, ""NameKey""='Permission_P045' WHERE ""Code""='P045';
UPDATE ""Permissions"" SET ""ParentCode""='PARTNER_PRODUCTS', ""NodeKind""=3, ""NameKey""='Permission_P046' WHERE ""Code""='P046';
UPDATE ""Permissions"" SET ""ParentCode""='COMPANIES', ""NodeKind""=3, ""NameKey""='Permission_P047' WHERE ""Code""='P047';
UPDATE ""Permissions"" SET ""ParentCode""='COMPANIES', ""NodeKind""=3, ""NameKey""='Permission_P048' WHERE ""Code""='P048';
UPDATE ""Permissions"" SET ""ParentCode""='PURCHASE_REQUESTS', ""NodeKind""=3, ""NameKey""='Permission_P060' WHERE ""Code""='P060';
UPDATE ""Permissions"" SET ""ParentCode""='PURCHASE_REQUESTS', ""NodeKind""=3, ""NameKey""='Permission_P061' WHERE ""Code""='P061';
UPDATE ""Permissions"" SET ""ParentCode""='PURCHASE_REQUESTS', ""NodeKind""=3, ""NameKey""='Permission_P062' WHERE ""Code""='P062';
UPDATE ""Permissions"" SET ""ParentCode""='OFFERS', ""NodeKind""=3, ""NameKey""='Permission_P063' WHERE ""Code""='P063';
UPDATE ""Permissions"" SET ""ParentCode""='OFFERS', ""NodeKind""=3, ""NameKey""='Permission_P064' WHERE ""Code""='P064';
UPDATE ""Permissions"" SET ""ParentCode""='OFFERS', ""NodeKind""=3, ""NameKey""='Permission_P065' WHERE ""Code""='P065';
UPDATE ""Permissions"" SET ""ParentCode""='GROUP_BUYING', ""NodeKind""=3, ""NameKey""='Permission_P066' WHERE ""Code""='P066';
UPDATE ""Permissions"" SET ""ParentCode""='GROUP_BUYING', ""NodeKind""=3, ""NameKey""='Permission_P067' WHERE ""Code""='P067';
UPDATE ""Permissions"" SET ""ParentCode""='GROUP_BUYING', ""NodeKind""=3, ""NameKey""='Permission_P068' WHERE ""Code""='P068';
UPDATE ""Permissions"" SET ""ParentCode""='GROUPS', ""NodeKind""=3, ""NameKey""='Permission_P070' WHERE ""Code""='P070';
UPDATE ""Permissions"" SET ""ParentCode""='GROUPS', ""NodeKind""=3, ""NameKey""='Permission_P071' WHERE ""Code""='P071';
UPDATE ""Permissions"" SET ""ParentCode""='GROUPS', ""NodeKind""=3, ""NameKey""='Permission_P072' WHERE ""Code""='P072';
UPDATE ""Permissions"" SET ""ParentCode""='GROUPS', ""NodeKind""=3, ""NameKey""='Permission_P073' WHERE ""Code""='P073';
UPDATE ""Permissions"" SET ""ParentCode""='GROUPS', ""NodeKind""=3, ""NameKey""='Permission_P074' WHERE ""Code""='P074';
UPDATE ""Permissions"" SET ""ParentCode""='SOCIAL_POSTS', ""NodeKind""=3, ""NameKey""='Permission_P080' WHERE ""Code""='P080';
UPDATE ""Permissions"" SET ""ParentCode""='SOCIAL_POSTS', ""NodeKind""=3, ""NameKey""='Permission_P081' WHERE ""Code""='P081';
UPDATE ""Permissions"" SET ""ParentCode""='SOCIAL_POSTS', ""NodeKind""=3, ""NameKey""='Permission_P082' WHERE ""Code""='P082';
UPDATE ""Permissions"" SET ""ParentCode""='SOCIAL_POSTS', ""NodeKind""=3, ""NameKey""='Permission_P083' WHERE ""Code""='P083';
UPDATE ""Permissions"" SET ""ParentCode""='SOCIAL_POSTS', ""NodeKind""=3, ""NameKey""='Permission_P084' WHERE ""Code""='P084';
UPDATE ""Permissions"" SET ""ParentCode""='PERMISSION_MATRIX', ""NodeKind""=3, ""NameKey""='Permission_P100' WHERE ""Code""='P100';
UPDATE ""Permissions"" SET ""ParentCode""='PERMISSION_MATRIX', ""NodeKind""=3, ""NameKey""='Permission_P101' WHERE ""Code""='P101';
UPDATE ""Permissions"" SET ""ParentCode""='PERMISSION_MATRIX', ""NodeKind""=3, ""NameKey""='Permission_P102' WHERE ""Code""='P102';
UPDATE ""Permissions"" SET ""ParentCode""='AUDIT_LOGS', ""NodeKind""=3, ""NameKey""='Permission_P103' WHERE ""Code""='P103';
UPDATE ""Permissions"" SET ""ParentCode""='PERMISSION_MATRIX', ""NodeKind""=3, ""NameKey""='Permission_P104' WHERE ""Code""='P104';
UPDATE ""Permissions"" SET ""ParentCode""='COMMISSION_CONFIG', ""NodeKind""=3, ""NameKey""='Permission_P105' WHERE ""Code""='P105';
UPDATE ""Permissions"" SET ""ParentCode""='COMMISSION_CONFIG', ""NodeKind""=3, ""NameKey""='Permission_P106' WHERE ""Code""='P106';
UPDATE ""Permissions"" SET ""ParentCode""='MY_COMMISSION', ""NodeKind""=3, ""NameKey""='Permission_P014' WHERE ""Code""='P014';
UPDATE ""Permissions"" SET ""ParentCode""='MY_COMMISSION', ""NodeKind""=3, ""NameKey""='Permission_P015' WHERE ""Code""='P015';
UPDATE ""Permissions"" SET ""ParentCode""='PAYOUTS', ""NodeKind""=3, ""NameKey""='Permission_P107' WHERE ""Code""='P107';
UPDATE ""Permissions"" SET ""ParentCode""='PAYOUTS', ""NodeKind""=3, ""NameKey""='Permission_P108' WHERE ""Code""='P108';
UPDATE ""Permissions"" SET ""ParentCode""='MEMBERSHIP_TIERS', ""NodeKind""=3, ""NameKey""='Permission_P109' WHERE ""Code""='P109';
UPDATE ""Permissions"" SET ""ParentCode""='BANK_ACCOUNTS', ""NodeKind""=3, ""NameKey""='Permission_P110' WHERE ""Code""='P110';
UPDATE ""Permissions"" SET ""ParentCode""='SYSTEM_SETTINGS', ""NodeKind""=3, ""NameKey""='Permission_P111' WHERE ""Code""='P111';
UPDATE ""Permissions"" SET ""ParentCode""='REVENUES', ""NodeKind""=3, ""NameKey""='Permission_P112' WHERE ""Code""='P112';
UPDATE ""Permissions"" SET ""ParentCode""='REVENUES', ""NodeKind""=3, ""NameKey""='Permission_P113' WHERE ""Code""='P113';
UPDATE ""Permissions"" SET ""ParentCode""='REVENUE_CONFIG', ""NodeKind""=3, ""NameKey""='Permission_P114' WHERE ""Code""='P114';
UPDATE ""Permissions"" SET ""ParentCode""='COLLABORATORS', ""NodeKind""=3, ""NameKey""='Permission_P115' WHERE ""Code""='P115';
UPDATE ""Permissions"" SET ""ParentCode""='PARTNERS', ""NodeKind""=3, ""NameKey""='Permission_P116' WHERE ""Code""='P116';
UPDATE ""Permissions"" SET ""ParentCode""='PARTNER_PRODUCTS', ""NodeKind""=3, ""NameKey""='Permission_P118' WHERE ""Code""='P118';
UPDATE ""Permissions"" SET ""ParentCode""='OFFERS', ""NodeKind""=3, ""NameKey""='Permission_P119' WHERE ""Code""='P119';
UPDATE ""Permissions"" SET ""ParentCode""='GROUP_BUYING', ""NodeKind""=3, ""NameKey""='Permission_P120' WHERE ""Code""='P120';
UPDATE ""Permissions"" SET ""ParentCode""='GROUPS', ""NodeKind""=3, ""NameKey""='Permission_P121' WHERE ""Code""='P121';
UPDATE ""Permissions"" SET ""ParentCode""='COMMISSION_CONFIG', ""NodeKind""=3, ""NameKey""='Permission_P124' WHERE ""Code""='P124';
UPDATE ""Permissions"" SET ""ParentCode""='MEMBERSHIP_TIERS', ""NodeKind""=3, ""NameKey""='Permission_P125' WHERE ""Code""='P125';
UPDATE ""Permissions"" SET ""ParentCode""='MEMBERSHIP_TIERS', ""NodeKind""=3, ""NameKey""='Permission_P126' WHERE ""Code""='P126';
UPDATE ""Permissions"" SET ""ParentCode""='MEMBERSHIP_TIERS', ""NodeKind""=3, ""NameKey""='Permission_P127' WHERE ""Code""='P127';
UPDATE ""Permissions"" SET ""ParentCode""='BANK_ACCOUNTS', ""NodeKind""=3, ""NameKey""='Permission_P128' WHERE ""Code""='P128';
UPDATE ""Permissions"" SET ""ParentCode""='REVENUE_CONFIG', ""NodeKind""=3, ""NameKey""='Permission_P129' WHERE ""Code""='P129';
UPDATE ""Permissions"" SET ""ParentCode""='REVENUE_CONFIG', ""NodeKind""=3, ""NameKey""='Permission_P130' WHERE ""Code""='P130';
UPDATE ""Permissions"" SET ""ParentCode""='REVENUE_CONFIG', ""NodeKind""=3, ""NameKey""='Permission_P131' WHERE ""Code""='P131';
UPDATE ""Permissions"" SET ""ParentCode""='PARTNERS', ""NodeKind""=3, ""NameKey""='Permission_P132' WHERE ""Code""='P132';
UPDATE ""Permissions"" SET ""ParentCode""='OFFERS', ""NodeKind""=3, ""NameKey""='Permission_P133' WHERE ""Code""='P133';
UPDATE ""Permissions"" SET ""ParentCode""='SOCIAL_POSTS', ""NodeKind""=3, ""NameKey""='Permission_P134' WHERE ""Code""='P134';
UPDATE ""Permissions"" SET ""ParentCode""='GROUP_BUYING', ""NodeKind""=3, ""NameKey""='Permission_P135' WHERE ""Code""='P135';
UPDATE ""Permissions"" SET ""ParentCode""='GROUP_BUYING', ""NodeKind""=3, ""NameKey""='Permission_P136' WHERE ""Code""='P136';
UPDATE ""Permissions"" SET ""ParentCode""='GROUPS', ""NodeKind""=3, ""NameKey""='Permission_P137' WHERE ""Code""='P137';
UPDATE ""Permissions"" SET ""ParentCode""='GROUPS', ""NodeKind""=3, ""NameKey""='Permission_P138' WHERE ""Code""='P138';
UPDATE ""Permissions"" SET ""ParentCode""='COMMISSION_CONFIG', ""NodeKind""=3, ""NameKey""='Permission_P139' WHERE ""Code""='P139';
UPDATE ""Permissions"" SET ""ParentCode""='COMMISSION_CONFIG', ""NodeKind""=3, ""NameKey""='Permission_P140' WHERE ""Code""='P140';
UPDATE ""Permissions"" SET ""ParentCode""='MEMBERSHIP_TIERS', ""NodeKind""=3, ""NameKey""='Permission_P141' WHERE ""Code""='P141';
UPDATE ""Permissions"" SET ""ParentCode""='MEMBERSHIP_TIERS', ""NodeKind""=3, ""NameKey""='Permission_P142' WHERE ""Code""='P142';
UPDATE ""Permissions"" SET ""ParentCode""='REVENUE_CONFIG', ""NodeKind""=3, ""NameKey""='Permission_P143' WHERE ""Code""='P143';
UPDATE ""Permissions"" SET ""ParentCode""='REVENUE_CONFIG', ""NodeKind""=3, ""NameKey""='Permission_P144' WHERE ""Code""='P144';
UPDATE ""Permissions"" SET ""ParentCode""='PURCHASE_REQUESTS', ""NodeKind""=3, ""NameKey""='Permission_P145' WHERE ""Code""='P145';
UPDATE ""Permissions"" SET ""ParentCode""='PURCHASE_REQUESTS', ""NodeKind""=3, ""NameKey""='Permission_P146' WHERE ""Code""='P146';
UPDATE ""Permissions"" SET ""ParentCode""='PURCHASE_REQUESTS', ""NodeKind""=3, ""NameKey""='Permission_P147' WHERE ""Code""='P147';
");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Permissions_Code",
                table: "Permissions",
                column: "Code");

            migrationBuilder.AddForeignKey(
                name: "FK_Permissions_Permissions_ParentCode",
                table: "Permissions",
                column: "ParentCode",
                principalTable: "Permissions",
                principalColumn: "Code",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Permissions_Permissions_ParentCode",
                table: "Permissions");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Permissions_Code",
                table: "Permissions");

            // Bỏ các node nhóm/màn hình đã seed (hành động vẫn còn vì mã P### không nằm trong danh sách này).
            migrationBuilder.Sql(
                "UPDATE \"Permissions\" SET \"ParentCode\" = NULL WHERE \"NodeKind\" <> 3;");
            migrationBuilder.Sql(
                "DELETE FROM \"Permissions\" WHERE \"NodeKind\" <> 3;");

            migrationBuilder.DropColumn(
                name: "NameKey",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "NodeKind",
                table: "Permissions");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Permissions",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_PermissionGroups_Code",
                table: "PermissionGroups",
                column: "Code");

            migrationBuilder.AddForeignKey(
                name: "FK_Permissions_PermissionGroups_ParentCode",
                table: "Permissions",
                column: "ParentCode",
                principalTable: "PermissionGroups",
                principalColumn: "Code",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
