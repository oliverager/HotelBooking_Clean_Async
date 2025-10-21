Feature: Fully occupied dates
    Scenario: With the seeded data
        When I ask for fully occupied dates from day +1 to day +21
        Then the result should contain every date from day +4 to day +18
        And the result should contain exactly 15 dates