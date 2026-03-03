# UI Company Management Specification

## Purpose

Provide a web interface for company owners to manage their business profile, services, and availability.

## Requirements

### Requirement: Company Dashboard

The system MUST display a dashboard for company owners to manage their business.

#### Scenario: Owner views their company dashboard

- GIVEN an authenticated owner with an associated company
- WHEN the owner navigates to the dashboard
- THEN the system SHALL display the company name
- AND display company description
- AND display current timezone
- AND show navigation to services and availability management

#### Scenario: Owner updates company profile

- GIVEN an authenticated owner viewing their company profile
- WHEN the owner updates the company name to "New Clinic Name"
- AND clicks "Save"
- THEN the system SHALL display a success message
- AND the updated name SHALL appear on the dashboard

### Requirement: Company Profile Form

The system MUST provide a form for editing company details.

#### Scenario: Owner edits company details

- GIVEN an authenticated owner viewing the edit company form
- WHEN the owner modifies the name, description, and timezone
- AND submits the form
- THEN the system SHALL validate the input
- AND save the changes
- AND return to the dashboard with updated information

#### Scenario: Form validation errors

- GIVEN an authenticated owner viewing the edit company form
- WHEN the owner submits with an empty required field
- THEN the system SHALL display validation errors
- AND prevent submission until corrected
