namespace Tripory.Domain.Catalogs;

public static class DestinationCatalog
{
    public static readonly IReadOnlyList<string> Codes = [
        "vn-ha-noi",
        "vn-cao-bang",
        "vn-tuyen-quang",
        "vn-dien-bien",
        "vn-lai-chau",
        "vn-son-la",
        "vn-lao-cai",
        "vn-thai-nguyen",
        "vn-lang-son",
        "vn-quang-ninh",
        "vn-bac-ninh",
        "vn-phu-tho",
        "vn-hai-phong",
        "vn-hung-yen",
        "vn-ninh-binh",
        "vn-thanh-hoa",
        "vn-nghe-an",
        "vn-ha-tinh",
        "vn-quang-tri",
        "vn-hue",
        "vn-da-nang",
        "vn-quang-ngai",
        "vn-gia-lai",
        "vn-khanh-hoa",
        "vn-dak-lak",
        "vn-lam-dong",
        "vn-dong-nai",
        "vn-ho-chi-minh",
        "vn-tay-ninh",
        "vn-dong-thap",
        "vn-vinh-long",
        "vn-an-giang",
        "vn-can-tho",
        "vn-ca-mau"
    ];

    public static bool IsKnown(string? code) {
        return Codes.Contains(code);
    }
}