# Company Specification

## Purpose

Manage business entities (companies/clinics/studios) in the multi-tenant system. Each company operates independently with isolated data.

## Requirements

### Requirement: Company Creation

The system MUST allow company owners to create and configure their company.

#### Scenario: Owner creates company

- GIVEN an authenticated owner user with no associated company
- WHEN the user submits company details with name "My Clinic", description "Medical services", and timezone "America/New_York"
- THEN the system SHALL create a new Company record
- AND associate it with the owner user
- AND return the company details

#### Scenario: Non-owner attempts to create company

- GIVEN an authenticated user with role "Customer"
- WHEN the user attempts to create a company
- THEN the system SHALL return 403 Forbidden

### Requirement: Company Retrieval

The system MUST allow retrieving company details.

#### Scenario: Owner retrieves their company

- GIVEN an authenticated owner with an associated company
- WHEN the user requests their company details
- THEN the system SHALL return the company information
- AND include all configured services and availability

#### Scenario: Customer views company

- GIVEN an authenticated customer
- WHEN the customer requests company details by ID
- THEN the system SHALL return public company information
- AND include available services

### Requirement: Company Update

The system MUST allow company owners to update their company details.

#### Scenario: Owner updates company name

- GIVEN an authenticated owner with an associated company
- WHEN the user updates the company name to "Updated Clinic Name"
- THEN the system SHALL persist the change
- AND return the updated company

### Requirement: Multi-tenant Isolation

The system MUST ensure companies can only access their own data.

#### Scenario: Cross-tenant data access denied

- GIVEN Company A exists with ID "comp-a"
- WHEN an authenticated user from Company B attempts to access Company A data
- THEN the system SHALL return 403 Forbidden
- AND no data SHALL be returned
