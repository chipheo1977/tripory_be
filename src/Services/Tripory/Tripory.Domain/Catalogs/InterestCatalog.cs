namespace Tripory.Domain.Catalogs;

public static class InterestCatalog
{
    
    public static readonly IReadOnlyList<string> Codes = [
        "history-culture",
        "nature",
        "shopping",
        "food-drink",
        "entertainment",
        "wellness",
        "other"
    ];

    public static bool IsKnown(string? code) {
        return Codes.Contains(code);
    }
}