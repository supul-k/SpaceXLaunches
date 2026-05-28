namespace SpaceXLaunches.Application.DTOs
{
    public record PaginationParams
    {
        public int Page { get; init; }
        public int PageSize { get; init; }

        public PaginationParams(int page = 1, int pageSize = 20)
        {
            Page = page < 1 ? 1 : page;
            PageSize = pageSize < 1 ? 1 : pageSize > 100 ? 100 : pageSize;
        }

        public int Offset => (Page - 1) * PageSize;
    }
}
