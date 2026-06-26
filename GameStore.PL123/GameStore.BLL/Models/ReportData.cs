namespace GameStore.BLL.Models
{
    public class MonthlyRevenue
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string Label => $"{Year}-{Month:D2}";
        public decimal Revenue { get; set; }
    }

    public class DailyOrderCount
    {
        public string Date { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class TopGameSale
    {
        public string Title { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Revenue { get; set; }
    }

    public class CategoryRevenue
    {
        public string Category { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
    }

    public class UsersByRole
    {
        public string Role { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class UsersByMonth
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string Label => $"{Year}-{Month:D2}";
        public int Count { get; set; }
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasPrevious => Page > 1;
        public bool HasNext => Page < TotalPages;
    }

    public class GamesByCategory
    {
        public string Category { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
