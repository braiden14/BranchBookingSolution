using System;
using System.Collections.Generic;
using System.Text;

namespace BranchBooking.Application.Models.Request
{
    public record CreateBranchRequest(
        string Code,
        string Name,
        string AddressLine1,
        string City,
        string Province,
        string PostalCode);
}
