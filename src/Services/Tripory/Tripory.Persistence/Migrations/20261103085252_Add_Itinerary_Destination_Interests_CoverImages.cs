using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tripory.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_Itinerary_Destination_Interests_CoverImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string[]>(
                name: "CoverImageUrls",
                schema: "itinerary",
                table: "itineraries",
                type: "text[]",
                nullable: false,
                defaultValueSql: "'{}'");

            migrationBuilder.AddColumn<string>(
                name: "DestinationCode",
                schema: "itinerary",
                table: "itineraries",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string[]>(
                name: "Interests",
                schema: "itinerary",
                table: "itineraries",
                type: "text[]",
                nullable: false,
                defaultValueSql: "'{}'");

            // Chuyển ảnh bìa cũ sang danh sách mới (04-BE-CONTRACT mục 10, D-99, D-104).
            // SQL thô nên không đi qua bộ lọc xóa mềm: áp dụng cho MỌI dòng, kể cả dòng đã xóa mềm.
            // 1) URL hợp lệ (https, tối đa 500 ký tự) trở thành phần tử duy nhất của danh sách; CoverImageUrl giữ nguyên làm ảnh đang chọn.
            migrationBuilder.Sql(@"
                UPDATE itinerary.itineraries
                SET ""CoverImageUrls"" = ARRAY[""CoverImageUrl""]
                WHERE ""CoverImageUrl"" IS NOT NULL
                  AND ""CoverImageUrl"" ~* '^https://'
                  AND length(""CoverImageUrl"") <= 500;
            ");

            // 2) Ảnh cũ không hợp lệ (rỗng, không phải https hoặc quá dài) bị bỏ: danh sách vẫn rỗng nên CoverImageUrl về NULL.
            migrationBuilder.Sql(@"
                UPDATE itinerary.itineraries
                SET ""CoverImageUrl"" = NULL
                WHERE ""CoverImageUrl"" IS NOT NULL
                  AND cardinality(""CoverImageUrls"") = 0;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoverImageUrls",
                schema: "itinerary",
                table: "itineraries");

            migrationBuilder.DropColumn(
                name: "DestinationCode",
                schema: "itinerary",
                table: "itineraries");

            migrationBuilder.DropColumn(
                name: "Interests",
                schema: "itinerary",
                table: "itineraries");
        }
    }
}
