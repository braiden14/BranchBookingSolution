using BranchBooking.Application.DTOs;
using BranchBooking.Application.Interfaces.Repositories;
using BranchBooking.Application.Interfaces.Services;
using BranchBooking.Application.Models.Schedule;
using BranchBooking.Core.Exceptions;

namespace BranchBooking.Application.Services;

public sealed class OperationalScheduleService(IOperationalScheduleRepository scheduleRepository) : IOperationalScheduleService
{
    public async Task<long> CreateAsync(long branchId, CreateOperationalScheduleRequest request, CancellationToken cancellationToken = default)
    {
        ValidateRequest(branchId, request);
        return await scheduleRepository.CreateAsync(branchId, request, cancellationToken);
    }

    public async Task<OperationalScheduleDto?> GetForBranchAsync(long branchId, DateOnly asOfDate,
        CancellationToken cancellationToken = default)
    {
        if (branchId <= 0)
        {
            throw new DomainValidationException("Branch ID must be greater than zero.");
        }

        var schedule = await scheduleRepository.GetForBranchAsync(branchId, asOfDate, cancellationToken);
        return schedule is null
            ? null
            : new OperationalScheduleDto(
                schedule.Id,
                schedule.BranchId,
                schedule.EffectiveFrom,
                schedule.EffectiveTo,
                schedule.IsActive,
                schedule.WeeklyHours);
    }

    private static void ValidateRequest(long branchId, CreateOperationalScheduleRequest? request)
    {
        if (branchId <= 0)
        {
            throw new DomainValidationException("Branch ID must be greater than zero.");
        }

        if (request is null)
        {
            throw new DomainValidationException("Operational schedule request is required.");
        }

        if (request.EffectiveTo.HasValue && request.EffectiveTo.Value < request.EffectiveFrom)
        {
            throw new DomainValidationException("Effective to date cannot be before effective from date.");
        }

        if (request.WeeklyHours is null || request.WeeklyHours.Count != 7)
        {
            throw new DomainValidationException("Exactly seven weekly operating-hours entries are required.");
        }

        if (request.WeeklyHours.Select(hours => hours.DayOfWeek).Distinct().Count() != 7 ||
            request.WeeklyHours.Any(hours => hours.DayOfWeek is < 1 or > 7))
        {
            throw new DomainValidationException("Weekly operating hours must contain one entry for each day from Monday to Sunday.");
        }

        foreach (var hours in request.WeeklyHours)
        {
            if (hours.IsClosed)
            {
                continue;
            }

            if (!hours.OpensAt.HasValue || !hours.ClosesAt.HasValue)
            {
                throw new DomainValidationException("Open days must have opening and closing times.");
            }

            if (hours.ClosesAt <= hours.OpensAt)
            {
                throw new DomainValidationException("Closing time must be after opening time.");
            }

            if (hours.Capacity is null or <= 0)
            {
                throw new DomainValidationException("Open days must have a capacity greater than zero.");
            }
        }
    }
}
