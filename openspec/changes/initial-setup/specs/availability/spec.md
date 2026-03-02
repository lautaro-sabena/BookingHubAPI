# Availability Specification

## Purpose

Manage working hours and availability windows for companies. Companies define when they offer services.

## Requirements

### Requirement: Working Hours Configuration

The system MUST allow company owners to configure weekly working hours.

#### Scenario: Owner sets weekly schedule

- GIVEN an authenticated owner with an associated company
- WHEN the user configures Monday-Friday 9:00-17:00, Saturday 9:00-13:00
- THEN the system SHALL store the schedule
- AND return confirmation

#### Scenario: Overlapping working hours validation

- GIVEN an authenticated owner configures working hours
- WHEN the user attempts to create overlapping time slots
- THEN the system SHALL return a validation error

### Requirement: Availability Slot Management

The system MUST generate and manage availability slots based on working hours and service durations.

#### Scenario: Generate available slots for a service

- GIVEN a company with working hours Monday 9:00-17:00
- AND a service with 60-minute duration
- WHEN availability is queried for a specific date
- THEN the system SHALL generate slots at 9:00, 10:00, 11:00, 12:00, 13:00, 14:00, 15:00, 16:00
- AND exclude lunch break if configured (e.g., 12:00-13:00)

#### Scenario: Slot generation considers existing reservations

- GIVEN a company with working hours Monday 9:00-17:00
- AND a 60-minute service
- AND an existing reservation at 10:00-11:00
- WHEN availability is queried
- THEN the 10:00 slot SHALL NOT appear as available

### Requirement: Exception Dates

The system MUST support exceptions (holidays, special hours).

#### Scenario: Owner sets holiday

- GIVEN an authenticated owner configures December 25 as a holiday
- WHEN availability is queried for that date
- THEN no slots SHALL be available
- AND the reason SHALL indicate holiday

#### Scenario: Owner sets special hours

- GIVEN an authenticated owner sets December 24 as half-day (9:00-12:00)
- WHEN availability is queried for that date
- THEN slots SHALL only be generated for 9:00-12:00

### Requirement: Availability Query

The system MUST allow querying available slots for a service on a date.

#### Scenario: Customer queries availability

- GIVEN a company with configured working hours and a service
- WHEN the customer requests availability for the service on a specific date
- THEN the system SHALL return available time slots
- AND each slot SHALL indicate start time and be bookable
