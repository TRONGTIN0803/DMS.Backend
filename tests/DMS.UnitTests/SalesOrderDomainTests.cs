using DMS.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace DMS.UnitTests;

public sealed class SalesOrderDomainTests
{
    [Fact]
    public void Recalculate_totals_sums_line_amounts_and_vat()
    {
        var order = new SalesOrder
        {
            Details =
            {
                new SalesOrderDetail { Quantity = 2m, UnitPrice = 100m, VatRate = 8m },
                new SalesOrderDetail { Quantity = 1.5m, UnitPrice = 20m, VatRate = 10m }
            }
        };

        order.RecalculateTotals();

        order.SubTotal.Should().Be(230m);
        order.VatAmount.Should().Be(19m);
        order.GrandTotal.Should().Be(249m);
    }
}
