using DMS.Shared;
using FluentAssertions;
using Xunit;

namespace DMS.UnitTests;

public sealed class SmokeTests
{
    [Fact]
    public void Paged_result_calculates_total_pages()
    {
        var result = new PagedResult<int>([1, 2], Page: 1, PageSize: 2, TotalCount: 5);

        result.TotalPages.Should().Be(3);
    }
}
