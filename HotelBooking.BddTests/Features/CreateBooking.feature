Feature: Create booking
As a visitor
I want to create a booking
So that I can reserve a room if one is available

    Background:
        Given the database is freshly seeded

    @happy
    Scenario: Book before the fully-booked window (success)
        When I create a booking from day +1 to day +3 for customer 1
        Then the HTTP status should be 201

    @happy
    Scenario: Book after the fully-booked window (success)
        When I create a booking from day +19 to day +21 for customer 1
        Then the HTTP status should be 201

    @conflict
    Scenario: End date touches lower edge (conflict)
        When I create a booking from day +1 to day +4 for customer 1
        Then the HTTP status should be 409

    @conflict
    Scenario: Inside fully booked range (conflict)
        When I create a booking from day +10 to day +12 for customer 1
        Then the HTTP status should be 409

    @conflict
    Scenario: Start date touches upper edge (conflict)
        When I create a booking from day +18 to day +20 for customer 1
        Then the HTTP status should be 409

    @badrequest
    Scenario: Null body
        When I submit an empty request to create a booking
        Then the HTTP status should be 400