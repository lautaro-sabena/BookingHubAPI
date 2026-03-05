# Delta for Reservations

## MODIFIED Requirements

### Requirement: Customer Booking Flow

The Customer MUST be able to browse services, select a date and time, and create a reservation.

#### Scenario: Customer successfully books a service

- GIVEN the Customer is logged in with role Customer
- AND the Customer has selected a service
- AND the Customer has selected a date in the future
- AND the selected time slot is available (no conflicts)
- WHEN the Customer submits the booking request
- THEN the system creates a reservation with status Pending
- AND the system returns a success response with reservation details
- AND the Customer sees a confirmation message

#### Scenario: Customer tries to book a past date

- GIVEN the Customer is on the booking page
- AND the Customer selects a date in the past
- WHEN the Customer attempts to submit the booking
- THEN the system SHALL return an error "Cannot book in the past"

#### Scenario: Customer tries to book an unavailable time slot

- GIVEN another Customer has already booked the time slot
- AND the Customer selects the same date and time
- WHEN the Customer submits the booking request
- THEN the system SHALL return an error "Time slot is not available"

### Requirement: Owner Calendar View

The Owner MUST be able to view all reservations for their company in a calendar format with customer details.

#### Scenario: Owner views reservation calendar

- GIVEN the User is logged in with role Owner
- AND the Owner has an associated company
- WHEN the Owner navigates to the calendar view
- THEN the system displays all reservations for that company
- AND each reservation shows: date, time, service name, customer email/name

#### Scenario: Owner views reservation details

- GIVEN the Owner is viewing their calendar
- AND there is a reservation on a specific date/time
- WHEN the Owner clicks or hovers on the reservation
- THEN the system displays: customer name, customer email, service name, reservation status, notes (if any)

## ADDED Requirements

### Requirement: Reservation List MUST Include Service and Customer Details

When fetching reservations, the system SHALL include the service name and customer email for each reservation.

#### Scenario: Customer views their reservations

- GIVEN the Customer is logged in
- WHEN the Customer requests their reservations
- THEN each reservation item includes: id, serviceName, startTime, endTime, status, customerEmail (same as logged-in user)

#### Scenario: Owner views company reservations

- GIVEN the Owner is logged in and has a company
- WHEN the Owner requests reservations for their company
- THEN each reservation item includes: id, serviceName, startTime, endTime, status, customerEmail
