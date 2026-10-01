using BookingHubAPI.Domain.Entities;
using BookingHubAPI.Domain.Exceptions;
using FluentAssertions;

namespace BookingHubAPI.UnitTests.Domain;

public class ReservationTransitionTests
{
    private static Reservation With(ReservationStatus status) => new() { Status = status };

    [Fact]
    public void NewReservation_IsPending()
    {
        new Reservation().Status.Should().Be(ReservationStatus.Pending);
    }

    [Theory]
    [InlineData(ReservationStatus.Pending, true)]
    [InlineData(ReservationStatus.Confirmed, false)]
    [InlineData(ReservationStatus.Cancelled, false)]
    [InlineData(ReservationStatus.Completed, false)]
    public void CanBeConfirmed_IsTrueOnlyForPending(ReservationStatus status, bool expected)
    {
        With(status).CanBeConfirmed.Should().Be(expected);
    }

    [Theory]
    [InlineData(ReservationStatus.Pending, true)]
    [InlineData(ReservationStatus.Confirmed, true)]
    [InlineData(ReservationStatus.Cancelled, false)]
    [InlineData(ReservationStatus.Completed, false)]
    public void CanBeCancelled_IsTrueOnlyForPendingAndConfirmed(ReservationStatus status, bool expected)
    {
        With(status).CanBeCancelled.Should().Be(expected);
    }

    [Fact]
    public void Confirm_MovesPendingToConfirmed()
    {
        var reservation = With(ReservationStatus.Pending);

        reservation.Confirm();

        reservation.Status.Should().Be(ReservationStatus.Confirmed);
    }

    [Theory]
    [InlineData(ReservationStatus.Confirmed)]
    [InlineData(ReservationStatus.Cancelled)]
    [InlineData(ReservationStatus.Completed)]
    public void Confirm_FromAnyOtherStatus_ThrowsAndKeepsTheStatus(ReservationStatus status)
    {
        var reservation = With(status);

        var act = reservation.Confirm;

        act.Should().Throw<DomainException>().WithMessage(Reservation.OnlyPendingCanBeConfirmedMessage);
        reservation.Status.Should().Be(status);
    }

    [Theory]
    [InlineData(ReservationStatus.Pending)]
    [InlineData(ReservationStatus.Confirmed)]
    public void Cancel_FromPendingOrConfirmed_MovesToCancelled(ReservationStatus status)
    {
        var reservation = With(status);

        reservation.Cancel();

        reservation.Status.Should().Be(ReservationStatus.Cancelled);
    }

    [Theory]
    [InlineData(ReservationStatus.Cancelled)]
    [InlineData(ReservationStatus.Completed)]
    public void Cancel_FromAFinalStatus_ThrowsAndKeepsTheStatus(ReservationStatus status)
    {
        var reservation = With(status);

        var act = reservation.Cancel;

        act.Should().Throw<DomainException>().WithMessage(Reservation.CannotBeCancelledMessage);
        reservation.Status.Should().Be(status);
    }
}
