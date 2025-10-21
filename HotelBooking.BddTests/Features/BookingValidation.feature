Feature: Booking date validation
  The manager rejects invalid date ranges

  Background:
    Given the database is freshly seeded

  @invalid
  Scenario: Start date is today
    When I ask the manager for an available room from day +0 to day +1
    Then an argument error is thrown

  @invalid
  Scenario: Start date after end date
    When I ask the manager for an available room from day +10 to day +9
    Then an argument error is thrown
