# UI Service Management Specification

## Purpose

Provide a web interface for company owners to create, edit, and manage their services, and for customers to browse available services.

## Requirements

### Requirement: Service Listing (Owner)

The system MUST display a list of services managed by the owner.

#### Scenario: Owner views their services

- GIVEN an authenticated owner with existing services
- WHEN the owner navigates to the services page
- THEN the system SHALL display a list of all services
- AND each service SHALL show name, description, duration, and price
- AND provide options to edit or delete each service

#### Scenario: Owner creates a new service

- GIVEN an authenticated owner on the services page
- WHEN the owner clicks "Add Service"
- AND fills in service name, description, duration (minutes), and price
- AND clicks "Save"
- THEN the system SHALL create the service
- AND display it in the services list

#### Scenario: Owner edits a service

- GIVEN an authenticated owner viewing their services
- WHEN the owner clicks "Edit" on a service
- AND modifies the service details
- AND clicks "Save"
- THEN the system SHALL update the service
- AND display the updated information

#### Scenario: Owner deletes a service

- GIVEN an authenticated owner viewing their services
- WHEN the owner clicks "Delete" on a service
- AND confirms the deletion
- THEN the system SHALL remove the service
- AND it SHALL no longer appear in the list

### Requirement: Service Listing (Customer)

The system MUST display available services to customers.

#### Scenario: Customer browses services

- GIVEN an authenticated customer
- WHEN the customer navigates to the services page
- THEN the system SHALL display all active services
- AND each service SHALL show name, description, duration, and price

#### Scenario: Customer views service details

- GIVEN a customer viewing the services list
- WHEN the customer clicks on a service
- THEN the system SHALL display detailed information
- AND provide a "Book Now" button
