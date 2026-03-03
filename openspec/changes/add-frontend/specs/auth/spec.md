# UI Authentication Specification

## Purpose

Provide a web interface for user registration and login, enabling users to authenticate and access the booking system through a visual interface.

## Requirements

### Requirement: Registration Page

The system MUST provide a registration page where users can create accounts.

#### Scenario: Owner successfully registers

- GIVEN the user navigates to the registration page
- WHEN the user enters email "owner@clinic.com", password "Secure123!", confirms password, and selects role "Owner"
- AND clicks the "Register" button
- THEN the system SHALL display a success message
- AND redirect to the owner dashboard
- AND the JWT token SHALL be stored securely

#### Scenario: Customer successfully registers

- GIVEN the user navigates to the registration page
- WHEN the user enters email "customer@example.com", password "Secure123!", confirms password, and selects role "Customer"
- AND clicks the "Register" button
- THEN the system SHALL display a success message
- AND redirect to the services listing page

#### Scenario: Registration with existing email

- GIVEN the user navigates to the registration page
- WHEN the user enters an email that is already registered
- AND clicks the "Register" button
- THEN the system SHALL display an error message
- AND the form SHALL remain filled with entered data

#### Scenario: Registration with password mismatch

- GIVEN the user navigates to the registration page
- WHEN the user enters different values in password and confirm password fields
- AND clicks the "Register" button
- THEN the system SHALL display a validation error
- AND prevent form submission

### Requirement: Login Page

The system MUST provide a login page for authenticated users.

#### Scenario: Successful login

- GIVEN the user navigates to the login page
- WHEN the user enters valid credentials
- AND clicks the "Login" button
- THEN the system SHALL store the JWT token
- AND redirect to the appropriate dashboard based on role

#### Scenario: Login with invalid credentials

- GIVEN the user navigates to the login page
- WHEN the user enters wrong email or password
- AND clicks the "Login" button
- THEN the system SHALL display an error message
- AND clear the password field

#### Scenario: Redirect to login when accessing protected page

- GIVEN a user is not authenticated
- WHEN the user attempts to access a protected page directly
- THEN the system SHALL redirect to the login page
- AND after successful login, redirect back to the original requested page

### Requirement: Session Management

The system MUST maintain user sessions securely.

#### Scenario: User stays logged in on page refresh

- GIVEN a user is logged in
- WHEN the user refreshes the browser page
- THEN the user SHALL remain authenticated
- AND the session SHALL persist

#### Scenario: User logs out

- GIVEN a user is logged in
- WHEN the user clicks the "Logout" button
- THEN the system SHALL clear the authentication token
- AND redirect to the login page
