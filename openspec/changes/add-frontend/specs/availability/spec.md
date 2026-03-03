# UI Availability Management Specification

## Purpose

Provide a web interface for company owners to define their working hours and availability.

## Requirements

### Requirement: Working Hours Configuration

The system MUST allow owners to configure their weekly schedule.

#### Scenario: Owner sets working hours

- GIVEN an authenticated owner on the availability page
- WHEN the owner sets Monday from 9:00 AM to 5:00 PM
- AND saves the configuration
- THEN the system SHALL store the working hours
- AND display them on the availability page

#### Scenario: Owner sets different hours per day

- GIVEN an authenticated owner on the availability page
- WHEN the owner configures different hours for each day of the week
- AND saves
- THEN the system SHALL store all configurations
- AND apply them when customers book appointments

#### Scenario: Owner marks day as unavailable

- GIVEN an authenticated owner on the availability page
- WHEN the owner marks Sunday as "Closed"
- AND saves
- THEN the system SHALL not allow bookings on Sunday
- AND Sunday SHALL appear as unavailable to customers

### Requirement: Availability Display

The system MUST show available time slots based on working hours.

#### Scenario: Customer sees available slots

- GIVEN a customer selecting a date to book
- WHEN the system calculates available slots
- THEN it SHALL only show slots within the company's working hours
- AND exclude slots that already have reservations
