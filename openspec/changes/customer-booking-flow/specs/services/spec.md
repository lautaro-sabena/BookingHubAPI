# Delta for Services

## ADDED Requirements

### Requirement: Service Listing MUST Include Company Information

When a Customer views the list of available services, the system SHALL display the company name and description associated with each service.

#### Scenario: Customer views available services

- GIVEN there are active services in the system with associated companies
- WHEN the Customer navigates to the services page
- THEN the system displays each service with its name, description, price, duration, and company name
- AND the company description is displayed if available

#### Scenario: Customer sees only active services

- GIVEN a service exists but is marked as inactive
- WHEN the Customer views the services list
- THEN the inactive service SHALL NOT appear in the list

### Requirement: Service Details Endpoint MUST Return Company Data

The API endpoint for getting a single service SHALL include the associated company's information in the response.

#### Scenario: Customer views service details

- GIVEN a service exists with an active company
- WHEN the Customer requests service details via the API
- THEN the response includes service.id, service.name, service.description, service.price, service.durationMinutes, and company.id, company.name

## REMOVED Requirements

(None)
