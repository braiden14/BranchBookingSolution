using BranchBooking.Core.Enums;
using BranchBooking.Core.Exceptions;

namespace BranchBooking.Core.Entities;

public sealed class Branch
{
    private Branch()
    {
    }

    public Branch(
        string code,
        string name,
        string addressLine1,
        string city,
        string province,
        string postalCode,
        BranchStatus status,
        bool acceptsBookings)
    {
        ValidateText(code, nameof(code), 50);
        ValidateText(name, nameof(name), 200);
        ValidateText(addressLine1, nameof(addressLine1), 200);
        ValidateText(city, nameof(city), 100);
        ValidateText(province, nameof(province), 100);
        ValidateText(postalCode, nameof(postalCode), 20);
        ValidateStatus(status);

        if (status == BranchStatus.Inactive && acceptsBookings)
        {
            throw new DomainValidationException("An inactive branch cannot accept bookings.");
        }

        Id = 0;
        Code = code;
        Name = name;
        AddressLine1 = addressLine1;
        City = city;
        Province = province;
        PostalCode = postalCode;
        Status = status;
        AcceptsBookings = acceptsBookings;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public long Id { get; private set; }

    public string Code { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public string AddressLine1 { get; private set; } = null!;

    public string City { get; private set; } = null!;

    public string Province { get; private set; } = null!;

    public string PostalCode { get; private set; } = null!;

    public BranchStatus Status { get; private set; }

    public bool AcceptsBookings { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public void Update(
        string code,
        string name,
        string addressLine1,
        string city,
        string province,
        string postalCode,
        string timezone,
        BranchStatus status,
        bool acceptsBookings)
    {
        ValidateText(code, nameof(code), 50);
        ValidateText(name, nameof(name), 200);
        ValidateText(addressLine1, nameof(addressLine1), 200);
        ValidateText(city, nameof(city), 100);
        ValidateText(province, nameof(province), 100);
        ValidateText(postalCode, nameof(postalCode), 20);
        ValidateText(timezone, nameof(timezone), 100);
        ValidateStatus(status);

        if (status == BranchStatus.Inactive && acceptsBookings)
        {
            throw new DomainValidationException("An inactive branch cannot accept bookings.");
        }

        Code = code;
        Name = name;
        AddressLine1 = addressLine1;
        City = city;
        Province = province;
        PostalCode = postalCode;
        Status = status;
        AcceptsBookings = acceptsBookings;
    }

    public void Deactivate()
    {
        Status = BranchStatus.Inactive;
        AcceptsBookings = false;
    }

    private static void ValidateText(string value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException($"{fieldName} is required.");
        }

        if (value.Length > maxLength)
        {
            throw new DomainValidationException($"{fieldName} cannot exceed {maxLength} characters.");
        }
    }

    private static void ValidateStatus(BranchStatus status)
    {
        if (!Enum.IsDefined(status))
        {
            throw new DomainValidationException("Branch status is invalid.");
        }
    }
}