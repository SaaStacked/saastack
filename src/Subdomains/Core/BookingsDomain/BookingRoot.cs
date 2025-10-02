﻿using Common;
using Common.Extensions;
using Domain.Common.Entities;
using Domain.Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Bookings;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
using Domain.Interfaces.ValueObjects;
using JetBrains.Annotations;
using QueryAny;

namespace BookingsDomain;

[EntityName("Booking")]
public sealed class BookingRoot : AggregateRootBase
{
    public static Result<BookingRoot, Error> Create(IRecorder recorder, IIdentifierFactory idFactory,
        Identifier organizationId)
    {
        var root = new BookingRoot(recorder, idFactory);
        root.RaiseCreateEvent(BookingsDomain.Events.Created(root.Id, organizationId));
        return root;
    }

    private BookingRoot(IRecorder recorder, IIdentifierFactory idFactory) : base(recorder, idFactory)
    {
    }

    private BookingRoot(ISingleValueObject<string> identifier, IDependencyContainer container,
        HydrationProperties rehydratingProperties) : base(
        identifier, container, rehydratingProperties)
    {
        Start = rehydratingProperties.GetValueOrDefault<DateTime>(nameof(Start));
        End = rehydratingProperties.GetValueOrDefault<DateTime>(nameof(End));
        CarId = rehydratingProperties.GetValueOrDefault<Identifier>(nameof(CarId));
        BorrowerId = rehydratingProperties.GetValueOrDefault<Identifier>(nameof(BorrowerId));
        OrganizationId = rehydratingProperties.GetValueOrDefault<Identifier>(nameof(OrganizationId));
    }

    public Optional<Identifier> BorrowerId { get; private set; }

    private bool CanBeCanceled => Start.HasValue && Start.Value > DateTime.UtcNow;

    public Optional<Identifier> CarId { get; private set; }

    public Optional<DateTime> End { get; private set; }

    public Identifier OrganizationId { get; private set; } = Identifier.Empty();

    public Optional<DateTime> Start { get; private set; }

    public Trips Trips { get; } = new();

    public override HydrationProperties Dehydrate()
    {
        var properties = base.Dehydrate();
        properties.Add(nameof(Start), Start);
        properties.Add(nameof(End), End);
        properties.Add(nameof(CarId), CarId);
        properties.Add(nameof(BorrowerId), BorrowerId);
        properties.Add(nameof(OrganizationId), OrganizationId);
        return properties;
    }

    [UsedImplicitly]
    public static AggregateRootFactory<BookingRoot> Rehydrate()
    {
        return (identifier, container, properties) => new BookingRoot(identifier, container, properties);
    }

    public override Result<Error> EnsureInvariants()
    {
        var ensureInvariants = base.EnsureInvariants();
        if (ensureInvariants.IsFailure)
        {
            return ensureInvariants.Error;
        }

        if (BorrowerId.Exists())
        {
            if (!CarId.HasValue)
            {
                return Error.RuleViolation(Resources.BookingRoot_ReservationRequiresCar);
            }
        }

        return Result.Ok;
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event, bool isReconstituting)
    {
        switch (@event)
        {
            case Created created:
            {
                OrganizationId = created.OrganizationId.ToId();
                return Result.Ok;
            }

            case ReservationMade changed:
            {
                BorrowerId = changed.BorrowerId.ToId();
                Start = changed.Start;
                End = changed.End;
                return Result.Ok;
            }

            case CarChanged changed:
            {
                CarId = changed.CarId.ToId();
                return Result.Ok;
            }

            case TripAdded changed:
            {
                var trip = RaiseEventToChildEntity(isReconstituting, changed, idFactory =>
                    Trip.Create(Recorder, idFactory, RaiseChangeEvent), e => e.TripId!);
                if (trip.IsFailure)
                {
                    return trip.Error;
                }

                Trips.Add(trip.Value);
                Recorder.TraceDebug(null, "Booking {Id} has created a new trip", Id);
                return Result.Ok;
            }

            case TripBegan changed:
            {
                Recorder.TraceDebug(null, "Booking {Id} has started trip {TripId} from {From}",
                    Id, changed.TripId, changed.BeganFrom);
                return Result.Ok;
            }

            case TripEnded changed:
            {
                Recorder.TraceDebug(null, "Booking {Id} has ended trip {TripId} at {To}",
                    Id, changed.TripId, changed.EndedTo);
                return Result.Ok;
            }

            default:
                return HandleUnKnownStateChangedEvent(@event);
        }
    }

    public Result<Error> Cancel()
    {
        if (!CanBeCanceled)
        {
            return Error.RuleViolation(Resources.BookingRoot_BookingAlreadyStarted);
        }

        return Result.Ok;
    }

    public Result<Error> ChangeCar(Identifier carId)
    {
        var nothingHasChanged = carId == CarId;
        if (nothingHasChanged)
        {
            return Result.Ok;
        }

        return RaiseChangeEvent(BookingsDomain.Events.CarChanged(Id, OrganizationId, carId));
    }

    public Result<Error> MakeReservation(Identifier borrowerId, DateTime start, DateTime end)
    {
        if (!CarId.HasValue)
        {
            return Error.RuleViolation(Resources.BookingRoot_ReservationRequiresCar);
        }

        if (end.IsInvalidParameter(e => e > start, nameof(end), Resources.BookingRoot_EndBeforeStart, out var error1))
        {
            return error1;
        }

        if (end.IsInvalidParameter(e => e.Subtract(start).Duration() >= Validations.Booking.MinimumBookingDuration,
                nameof(end), Resources.BookingRoot_BookingDurationTooShort, out var error3))
        {
            return error3;
        }

        if (end.IsInvalidParameter(e => e.Subtract(start).Duration() <= Validations.Booking.MaximumBookingDuration,
                nameof(end), Resources.BookingRoot_BookingDurationTooLong, out var error4))
        {
            return error4;
        }

        var nothingHasChanged = borrowerId == BorrowerId
                                && start == Start
                                && end == End;
        if (nothingHasChanged)
        {
            return Result.Ok;
        }

        return RaiseChangeEvent(
            BookingsDomain.Events.ReservationMade(Id, OrganizationId, borrowerId, start, end));
    }

    public Result<Error> StartTrip(Location from)
    {
        if (!CarId.HasValue)
        {
            return Error.RuleViolation(Resources.BookingRoot_ReservationRequiresCar);
        }

        var added = RaiseChangeEvent(BookingsDomain.Events.TripAdded(Id, OrganizationId));
        if (added.IsFailure)
        {
            return added.Error;
        }

        var trip = Trips.Latest()!;
        return trip.Begin(from);
    }
}