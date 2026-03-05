# Delta for Availability

## ADDED Requirements

### Requirement: Availability MUST Use Company Working Hours

The availability calculation SHALL use the Company's configured working hours instead of hardcoded hours.

#### Scenario: Customer requests availability for a day within company hours

- GIVEN a Company has working hours set for Monday (e.g., 9:00 AM - 5:00 PM)
- AND the service has a duration of 60 minutes
- WHEN the Customer requests availability for a Monday
- THEN the system returns available slots between 9:00 AM and 5:00 PM
- AND slots are generated based on the service duration

#### Scenario: Customer requests availability for a day outside company hours

- GIVEN a Company has no working hours for Sunday
- WHEN the Customer requests availability for Sunday
- THEN the system returns an empty list of available slots

#### Scenario: Company has no working hours configured

- GIVEN a Company has no WorkingHours records
- WHEN any Customer requests availability for that company's service
- THEN the system returns an empty list (no available slots)

### Requirement: Availability Endpoint MUST Accept Date Parameter

The availability API endpoint MUST accept a date parameter to return slots for a specific day.

#### Scenario: Customer requests availability for specific date

- GIVEN a service exists with an active company
- WHEN the Customer calls GET /api/availability/{serviceId}?date=2026-03-15
- THEN the response contains available time slots for that specific date
- AND each slot has: startTime, endTime, isAvailable

## MODIFIED Requirements

### Requirement: Slot Generation Duration

The time slot duration for availability MUST be based on the service's DurationMinutes property.

(Previously: Slot duration was fixed to 60 minutes)

#### Scenario: Service has 30-minute duration

- GIVEN a service with DurationMinutes = 30
- AND the company works 9:00 AM - 5:00 PM
- WHEN availability is requested
- THEN slots are generated at 30-minute intervals (9:00, 9:30, 10:00, etc.)

#### Scenario: Service has 45-minute duration

- GIVEN a service with DurationMinutes = 45
- AND the company works 9:00 AM - 5:00 PM
- WHEN availability is requested
- THEN slots are generated at 45-minute intervals (9:00, 9:45, 10:30, etc.)
