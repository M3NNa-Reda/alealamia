using alealamia.Domain.Constants;

namespace alealamia.Application.Common;

internal static class Paging
{
    public static (int Page, int PageSize) Normalize(int page, int pageSize)
    {
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedSize = pageSize < 1 ? TaskRules.DefaultPageSize : Math.Min(pageSize, TaskRules.MaxPageSize);
        return (normalizedPage, normalizedSize);
    }
}
