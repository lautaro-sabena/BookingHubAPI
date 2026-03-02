# Authentication Specification

## Purpose

Secure user registration and login for the BookingHubAPI multi-tenant system. Users can register as either business owners (Company admins) or customers.

## Requirements

### Requirement: User Registration

The system MUST allow users to register with email, password, and role (Owner or Customer).

#### Scenario: Successful owner registration

- GIVEN a valid email that is not already registered
- WHEN the user submits registration with email "owner@clinic.com", password "Secure123!", and role "Owner"
- THEN the system SHALL create a new user record
- AND return a JWT token for authentication
- AND the user SHALL be associated with a new Company

#### Scenario: Successful customer registration

- GIVEN a valid email that is not already registered
- WHEN the user submits registration with email "customer@example.com", password "Secure123!", and role "Customer"
- THEN the system SHALL create a new user record
- AND return a JWT token for authentication

#### Scenario: Registration with duplicate email

- GIVEN a user with email "existing@test.com" already exists
- WHEN a new user attempts to register with the same email
- THEN the system SHALL return a validation error
- AND no new user SHALL be created

#### Scenario: Registration with weak password

- GIVEN a user submits a password that does not meet complexity requirements
- WHEN the registration is attempted
- THEN the system SHALL reject the request
- AND return a validation error specifying password requirements

### Requirement: User Login

The system MUST authenticate registered users and issue JWT tokens.

#### Scenario: Successful login

- GIVEN a user with email "user@test.com" and password "Secure123!" exists
- WHEN the user submits login with correct credentials
- THEN the system SHALL return a valid JWT token
- AND the token SHALL contain the user ID and role

#### Scenario: Login with wrong password

- GIVEN a user with email "user@test.com" exists with password "CorrectPass123!"
- WHEN the user submits login with password "WrongPass"
- THEN the system SHALL return an authentication error
- AND no token SHALL be issued

#### Scenario: Login with non-existent user

- GIVEN no user with email "nonexistent@test.com" exists
- WHEN login is attempted with that email
- THEN the system SHALL return an authentication error

### Requirement: JWT Token Validation

The system MUST validate JWT tokens on protected endpoints.

#### Scenario: Access protected endpoint with valid token

- GIVEN a valid JWT token is obtained
- WHEN a request is made to a protected endpoint with the token
- THEN the request SHALL succeed
- AND the user context SHALL be available

#### Scenario: Access protected endpoint with expired token

- GIVEN an expired JWT token
- WHEN a request is made to a protected endpoint
- THEN the system SHALL return 401 Unauthorized

#### Scenario: Access protected endpoint without token

- GIVEN no JWT token is provided
- WHEN a request is made to a protected endpoint
- THEN the system SHALL return 401 Unauthorized
