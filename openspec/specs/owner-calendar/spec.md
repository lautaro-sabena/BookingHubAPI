# Owner Calendar Navigation Specification

## Purpose

This specification defines the navigation and layout requirements for the Owner Calendar page, ensuring consistency with other Owner dashboard pages.

## Requirements

### Requirement: Calendar Page Navigation

The Owner Calendar page MUST provide a way for users to navigate back to the Owner Dashboard.

#### Scenario: Navigate back to Owner Dashboard from Calendar

- GIVEN a user is on the Calendar page at `/owner/calendar` or `/dashboard/owner/calendar`
- WHEN the user clicks the back button/arrow
- THEN the user SHALL be redirected to `/dashboard/owner`
- AND the Owner Dashboard page SHALL load correctly

#### Scenario: Calendar accessible via sidebar

- GIVEN a user is on any Owner dashboard page with the sidebar visible
- WHEN the user clicks "Calendar" in the sidebar navigation
- THEN the Calendar page SHALL load with the same layout and sidebar
- AND the Calendar link SHALL be marked as active in the sidebar

### Requirement: Calendar Page Layout Integration

The Calendar page MUST use the same layout structure as other Owner dashboard pages.

#### Scenario: Calendar page has consistent layout

- GIVEN a user navigates to the Calendar page
- THEN the page SHALL display the sidebar navigation on the left
- AND the page content SHALL appear in the main content area
- AND the layout SHALL match other pages under `/dashboard/owner/*`

### Requirement: Existing Calendar Functionality Preserved

All existing calendar features MUST continue to work after navigation changes.

#### Scenario: View reservations in calendar

- GIVEN a user is on the Calendar page
- WHEN the calendar displays a date with reservations
- THEN the reservations SHALL show with correct status colors (yellow=Pending, green=Confirmed, red=Cancelled)
- AND clicking a reservation SHALL show reservation details

#### Scenario: Confirm a reservation

- GIVEN a user has clicked on a Pending reservation
- WHEN the user clicks the "Confirm" button
- THEN the reservation status SHALL change to "Confirmed"
- AND the calendar SHALL update to show the new status color

#### Scenario: Cancel a reservation

- GIVEN a user has clicked on a Pending or Confirmed reservation
- WHEN the user clicks the "Cancel" button
- THEN the reservation status SHALL change to "Cancelled"
- AND the calendar SHALL update to show the cancelled status color

#### Scenario: Navigate between months

- GIVEN a user is on the Calendar page
- WHEN the user clicks "Previous" or "Next" buttons
- THEN the calendar SHALL display the previous or next month
- AND any reservations for the displayed month SHALL be shown

### Requirement: Authentication and Authorization

The Calendar page MUST enforce proper access control.

#### Scenario: Unauthenticated access redirect

- GIVEN a user is not logged in
- WHEN the user navigates to the Calendar page
- THEN the user SHALL be redirected to `/login`

#### Scenario: Non-Owner access redirect

- GIVEN a logged-in user with role "Customer"
- WHEN the user navigates to `/owner/calendar` or `/dashboard/owner/calendar`
- THEN the user SHALL be redirected to `/dashboard`
