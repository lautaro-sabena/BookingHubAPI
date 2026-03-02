# Reservation Specification

## Purpose

Manage the booking lifecycle - customers reserve time slots, companies confirm, customers cancel. Includes conflict detection and status management.

## Requirements

### Requirement: Reservation Creation

The system MUST allow customers to book available time slots.

#### Scenario: Successful reservation booking

- GIVEN a customer is authenticated
- AND a service has available slots on a specific date/time
- WHEN the customer submits a reservation for slot 2024-01-15 10:00
- THEN the system SHALL create a reservation with status "Pending"
- AND return confirmation details
- AND send notification (mock) to company

#### Scenario: Double-booking prevention

- GIVEN a time slot at 10:00 is already reserved
- WHEN another customer attempts to reserve the same slot
- THEN the system SHALL return a conflict error
- AND no reservation SHALL be created

#### Scenario: Booking unavailable slot

- GIVEN a slot is outside company working hours
- WHEN a customer attempts to reserve that slot
- THEN the system SHALL return a validation error

#### Scenario: Booking inactive service

- GIVEN a service is marked as inactive
- WHEN a customer attempts to reserve it
- THEN the system SHALL return an error
- AND no reservation SHALL be created

### Requirement: Reservation Status Workflow

The system MUST manage reservation lifecycle through statuses.

#### Statuses:
- Pending: Initial state when customer books
- Confirmed: Company accepts the reservation
- Cancelled: Customer or company cancels
- Completed: Reservation has passed successfully

#### Scenario: Company confirms reservation

- GIVEN a reservation with status "Pending"
- WHEN the company owner confirms it
- THEN the status SHALL change to "Confirmed"
- AND notification SHALL be sent to customer

#### Scenario: Customer cancels reservation

- GIVEN a reservation with status "Confirmed" or "Pending"
- WHEN the customer cancels it
- THEN the status SHALL change to "Cancelled"
- AND the time slot SHALL become available again

#### Scenario: Company cancels reservation

- GIVEN a reservation with status "Confirmed"
- WHEN the company owner cancels it
- THEN the status SHALL change to "Cancelled"
- AND notification SHALL be sent to customer

#### Scenario: Reservation auto-completes

- GIVEN a reservation with status "Confirmed"
- AND the reservation time has passed
- WHEN the system processes it
- THEN the status SHALL change to "Completed"

### Requirement: Conflict Detection

The system MUST detect and prevent scheduling conflicts.

#### Scenario: Overlapping reservation detection

- GIVEN a customer has a reservation at 10:00-11:00
- WHEN they attempt another reservation at 10:30-11:30
- THEN the system SHALL return a conflict error

#### Scenario: Service duration overlap

- GIVEN a 60-minute service reserved at 10:00
- AND another customer attempts to book a 30-minute service at 10:30
- THEN the system SHALL return a conflict error

### Requirement: Reservation History

The system MUST provide reservation history for customers and companies.

#### Scenario: Customer views their reservations

- GIVEN a customer with multiple reservations
- WHEN they request their reservation history
- THEN the system SHALL return all their reservations
- AND include status, service name, date/time
- AND support pagination

#### Scenario: Filter reservations by status

- GIVEN a customer with reservations in various statuses
- WHEN they filter by status "Cancelled"
- THEN only cancelled reservations SHALL be returned

### Requirement: Reservation Validation

The system MUST validate reservation data.

#### Scenario: Reservation in the past

- GIVEN a customer attempts to reserve a past date/time
- THEN the system SHALL return a validation error

#### Scenario: Reservation too far in advance

- GIVEN a company limits bookings to 60 days ahead
- WHEN a customer attempts to book beyond that
- THEN the system SHALL return a validation error
