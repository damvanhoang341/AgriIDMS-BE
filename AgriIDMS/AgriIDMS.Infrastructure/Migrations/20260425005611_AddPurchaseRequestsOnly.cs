using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriIDMS.Infrastructure.Migrations
{
    /// <summary>
    /// Chỉ tạo bảng PurchaseRequests / PurchaseRequestDetails nếu chưa tồn tại.
    /// (Tránh tái tạo thay đổi lớn trùng với DB đã cập nhật thủ công từ migration trước.)
    /// </summary>
    public partial class AddPurchaseRequestsOnly : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[dbo].[PurchaseRequests]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[PurchaseRequests] (
                        [Id] int NOT NULL IDENTITY(1,1),
                        [RequestCode] nvarchar(50) NOT NULL,
                        [Status] int NOT NULL,
                        [RequestedDate] datetime2 NOT NULL,
                        [CreatedBy] nvarchar(450) NOT NULL,
                        [Notes] nvarchar(500) NULL,
                        CONSTRAINT [PK_PurchaseRequests] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_PurchaseRequests_AspNetUsers_CreatedBy] FOREIGN KEY ([CreatedBy])
                            REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE NO ACTION
                    );
                    CREATE INDEX [IX_PurchaseRequests_CreatedBy] ON [dbo].[PurchaseRequests] ([CreatedBy]);
                END

                IF OBJECT_ID(N'[dbo].[PurchaseRequestDetails]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[PurchaseRequestDetails] (
                        [Id] int NOT NULL IDENTITY(1,1),
                        [PurchaseRequestId] int NOT NULL,
                        [ProductId] int NOT NULL,
                        [RequestedWeight] decimal(18,3) NOT NULL,
                        [AllocatedWeight] decimal(18,3) NOT NULL DEFAULT 0,
                        [TargetUnitPrice] decimal(18,2) NOT NULL DEFAULT 0,
                        CONSTRAINT [PK_PurchaseRequestDetails] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_PurchaseRequestDetails_Products_ProductId] FOREIGN KEY ([ProductId])
                            REFERENCES [dbo].[Products] ([Id]) ON DELETE NO ACTION,
                        CONSTRAINT [FK_PurchaseRequestDetails_PurchaseRequests_PurchaseRequestId] FOREIGN KEY ([PurchaseRequestId])
                            REFERENCES [dbo].[PurchaseRequests] ([Id]) ON DELETE CASCADE
                    );
                    CREATE INDEX [IX_PurchaseRequestDetails_ProductId] ON [dbo].[PurchaseRequestDetails] ([ProductId]);
                    CREATE INDEX [IX_PurchaseRequestDetails_PurchaseRequestId] ON [dbo].[PurchaseRequestDetails] ([PurchaseRequestId]);
                END
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[dbo].[PurchaseRequestDetails]', N'U') IS NOT NULL
                    DROP TABLE [dbo].[PurchaseRequestDetails];
                IF OBJECT_ID(N'[dbo].[PurchaseRequests]', N'U') IS NOT NULL
                    DROP TABLE [dbo].[PurchaseRequests];
                """);
        }
    }
}
