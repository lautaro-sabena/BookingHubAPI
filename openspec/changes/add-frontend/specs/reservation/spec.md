# UI Reservation Specification

## Purpose

Provide a web interface for customers to book services and manage their reservations, and for owners to view and manage reservations.

## Requirements

### Requirement: Booking Flow

The system MUST guide customers through the reservation process.

#### Scenario: Customer books a service

- GIVEN an authenticated customer viewing a service
- WHEN the customer clicks "Book Now"
- AND selects a date and time slot
- AND confirms the booking
- THEN the system SHALL create the reservation
- AND display a confirmation message
- AND show the reservation in "My Reservations"

#### Scenario: Customer selects unavailable time slot

- GIVEN an authenticated customer in the booking flow
- WHEN the customer selects a time slot that is already taken
- THEN the system SHALL indicate the slot is unavailable
- AND prompt the customer to select another time

#### Scenario: Customer views available time slots

- GIVEN an authenticated customer viewing a service to book
- WHEN the customer selects a date
- THEN the system SHALL display available time slots
- AND indicate which slots are already booked

### Requirement: My Reservations

The system MUST display customer's reservations.

#### Scenario: Customer views their reservations

- GIVEN an authenticated customer with existing reservations
- WHEN the customer navigates to "My Reservations"
- THEN the system SHALL display a list of reservations
- AND each SHALL show service name, date, time, and status

#### Scenario: Customer cancels a reservation

- GIVEN an authenticated customer viewing their reservations
- WHEN the customer clicks "Cancel" on a pending reservation
- AND confirms the cancellation
- THEN the system SHALL update the reservation status to Cancelled
- AND display a success message

#### Scenario: Customer cannot cancel completed reservation

- GIVEN an authenticated customer with a completed reservation
- WHEN the customer attempts to cancel it
- THEN the system SHALL disable the cancel button
- AND display a message that completed reservations cannot be cancelled

### Requirement: Owner Reservation Management

The system MUST allow owners to view and manage reservations for their company.

#### Scenario: Owner views company reservations

- GIVEN an authenticated owner
- WHEN the owner navigates to reservations
- THEN the system SHALL display all reservations for their company
- AND show customer name, service, date, time, and status

#### Scenario: Owner confirms a reservation

- GIVEN an authenticated owner viewing pending reservations
- WHEN the owner clicks "Confirm" on a reservation
- THEN the system SHALL update status to Confirmed
- AND display a success message
