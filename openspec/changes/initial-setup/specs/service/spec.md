# Service Specification

## Purpose

Manage services offered by companies (e.g., "Medical Consultation", "Haircut", "Therapy Session"). Each service has duration, pricing, and description.

## Requirements

### Requirement: Service Creation

The system MUST allow company owners to create services.

#### Scenario: Owner creates a service

- GIVEN an authenticated owner with an associated company
- WHEN the user submits service details with name "Medical Consultation", duration 30 minutes, price 100.00, description "Initial medical checkup"
- THEN the system SHALL create a new Service record
- AND associate it with the company
- AND return the service details

#### Scenario: Service creation with invalid duration

- GIVEN an authenticated owner with an associated company
- WHEN the user submits a service with duration 0 or negative
- THEN the system SHALL return a validation error
- AND no service SHALL be created

#### Scenario: Service creation with negative price

- GIVEN an authenticated owner with an associated company
- WHEN the user submits a service with negative price
- THEN the system SHALL return a validation error

### Requirement: Service Listing

The system MUST allow listing services with pagination and filtering.

#### Scenario: List company services with pagination

- GIVEN a company with 15 services
- WHEN a request is made to list services with page 1 and pageSize 5
- THEN the system SHALL return 5 services
- AND include pagination metadata (total count, current page, total pages)

#### Scenario: Filter services by name

- GIVEN a company with services "Medical Consultation", "Follow-up", "Emergency"
- WHEN a request is made to filter services by name containing "Consultation"
- THEN the system SHALL return only "Medical Consultation"

#### Scenario: Customer views active services

- GIVEN a company with some active and some inactive services
- WHEN a customer requests available services
- THEN the system SHALL return only active services

### Requirement: Service Update

The system MUST allow company owners to update their services.

#### Scenario: Owner updates service price

- GIVEN an authenticated owner with an existing service priced at 100.00
- WHEN the user updates the price to 120.00
- THEN the system SHALL persist the change
- AND future reservations SHALL use the new price

### Requirement: Service Deletion

The system MUST allow soft-deleting services (deactivation).

#### Scenario: Owner deactivates service

- GIVEN an authenticated owner with an active service
- WHEN the user deactivates the service
- THEN the service SHALL be marked as inactive
- AND new reservations SHALL not be allowed
- AND existing reservations SHALL remain valid
