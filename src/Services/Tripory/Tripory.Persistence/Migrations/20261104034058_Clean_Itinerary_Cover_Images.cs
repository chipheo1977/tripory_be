using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tripory.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Clean_Itinerary_Cover_Images : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Không đổi cấu trúc bảng. Làm sạch dữ liệu ảnh bìa cho khớp luật của validator
            // (https, có host, không khoảng trắng/ký tự điều khiển, tối đa 500 ký tự), nếu không
            // mọi lần tự lưu sau đó sẽ bị Itinerary.CoverImageInvalidUrl từ chối (04-BE-REVIEW WR-04).
            // SQL thô nên áp dụng cho MỌI dòng, kể cả dòng đã xóa mềm. Giữ nguyên thứ tự phần tử.

            // 1) Bỏ các URL không hợp lệ khỏi danh sách; chỉ đụng các dòng có ít nhất một URL không hợp lệ.
            migrationBuilder.Sql(@"
                UPDATE itinerary.itineraries
                SET ""CoverImageUrls"" = ARRAY(
                    SELECT t.url
                    FROM unnest(""CoverImageUrls"") WITH ORDINALITY AS t(url, position)
                    WHERE t.url ~* '^https://[^/?#[:space:][:cntrl:]]+([/?#][^[:space:][:cntrl:]]*)?$'
                      AND length(t.url) <= 500
                    ORDER BY t.position)
                WHERE EXISTS (
                    SELECT 1
                    FROM unnest(""CoverImageUrls"") AS u(url)
                    WHERE NOT (u.url ~* '^https://[^/?#[:space:][:cntrl:]]+([/?#][^[:space:][:cntrl:]]*)?$'
                               AND length(u.url) <= 500));
            ");

            // 2) Giữ bất biến: CoverImageUrl phải là một phần tử của danh sách, hoặc NULL khi danh sách rỗng.
            //    Nếu ảnh đang chọn đã bị bỏ thì chọn ảnh đầu tiên còn lại.
            migrationBuilder.Sql(@"
                UPDATE itinerary.itineraries
                SET ""CoverImageUrl"" = CASE
                        WHEN cardinality(""CoverImageUrls"") = 0 THEN NULL
                        ELSE ""CoverImageUrls""[1]
                    END
                WHERE (cardinality(""CoverImageUrls"") = 0 AND ""CoverImageUrl"" IS NOT NULL)
                   OR (cardinality(""CoverImageUrls"") > 0
                       AND (""CoverImageUrl"" IS NULL OR NOT (""CoverImageUrl"" = ANY(""CoverImageUrls""))));
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Không khôi phục được: các URL không hợp lệ đã bị bỏ ở Up(). Quay lui bằng bản sao lưu (04-BE-CONTRACT mục 10).
        }
    }
}
