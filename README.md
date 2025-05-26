# Chicks Gold Backend Challenge

This is attempting to solve [Chicks Gold Backend Challenge](./chicks-gold-be-challenge.pdf)

## Problem Description

Given:
- Two jugs with capacities X and Y liters (no markings on the jugs)
- An infinite water supply
- Target amount Z liters

Goal: Find the shortest sequence of steps to measure exactly Z liters using only these operations:
1. Fill a jug completely
2. Empty a jug completely
3. Pour water from one jug to another until either:
   - The source jug is empty
   - The destination jug is full

## Getting Started

### Installation

1. Install dotnet SDK if you don't already have it
```bash
brew install --cask dotnet-sdk
```

2. Run this command at the root of this project to run the backend
```bash
dotnet run
```

The API will be available at:
- HTTP: http://localhost:5125

## API Documentation

### Solve Endpoint

Finds the shortest sequence of steps to measure the target amount using two jugs.

**Endpoint:** `POST /solve`

**Request Format:**
```json
{
    "x_capacity": int,
    "y_capacity": int,
    "z_amount_wanted": int
}
```

**Parameters:**
- `x_capacity`: Capacity of the first jug (X)
- `y_capacity`: Capacity of the second jug (Y)
- `z_amount_wanted`: Target amount to measure (Z)

**Response Format:**
```json
{
    "solution": [
        {
            "step": int,
            "bucketX": int,
            "bucketY": int,
            "action": string,
            "status": string
        }
        // ... more steps
    ]
}
```

**Status Codes:**
- `200 OK`: Solution found successfully
- `400 Bad Request`: Invalid input or impossible solution
- `500 Internal Server Error`: Server-side error

**Example Request:**
```json
{
    "x_capacity": 4,
    "y_capacity": 3,
    "z_amount_wanted": 2
}
```

**Example Response:**
```json
{
    "solution": [
        {
            "step": 0,
            "bucketX": 0,
            "bucketY": 0,
            "action": "Start",
            "status": "Initial State"
        },
        {
            "step": 1,
            "bucketX": 4,
            "bucketY": 0,
            "action": "Fill bucket X",
            "status": null
        },
        {
            "step": 2,
            "bucketX": 1,
            "bucketY": 3,
            "action": "Transfer from bucket X to bucket Y",
            "status": null
        },
        {
            "step": 3,
            "bucketX": 1,
            "bucketY": 0,
            "action": "Empty bucket Y",
            "status": null
        },
        {
            "step": 4,
            "bucketX": 0,
            "bucketY": 1,
            "action": "Transfer from bucket X to bucket Y",
            "status": null
        },
        {
            "step": 5,
            "bucketX": 4,
            "bucketY": 1,
            "action": "Fill bucket X",
            "status": null
        },
        {
            "step": 6,
            "bucketX": 2,
            "bucketY": 3,
            "action": "Transfer from bucket X to bucket Y",
            "status": "Solved"
        }
    ]
}
```

## Algorithm Explanation

The solution uses a Breadth-First Search (BFS) algorithm to find the shortest sequence of steps to reach the target amount. Here's how it works:

1. **Input Validation:**
   - Checks if input values are non-negative
   - Verifies if the target is achievable using GCD properties
   - Ensures target isn't larger than combined jug capacity

2. **Solution Search:**
   - Uses BFS to explore all possible states (x, y) where x and y are current amounts in each jug
   - Maintains a visited set to avoid cycles
   - Tracks parent states for path reconstruction
   - Explores six possible operations at each state:
     - Fill X
     - Fill Y
     - Empty X
     - Empty Y
     - Pour X → Y
     - Pour Y → X

3. **Optimization:**
   - Implements solution caching to avoid recalculating known problems
   - Returns immediately for special cases (e.g., target = 0)

## Performance Considerations

- Time Complexity: O(x*y) where x and y are jug capacities
- Space Complexity: O(x*y) for storing visited states
- Solution Caching: O(1) lookup for previously solved problems

### Complexity Example
Consider two jugs with:
- Jug X capacity = 3 liters
- Jug Y capacity = 2 liters

All possible states (x,y) that could exist:

(0,0) (0,1) (0,2) # X=0, Y varies from 0-2

(1,0) (1,1) (1,2) # X=1, Y varies from 0-2

(2,0) (2,1) (2,2) # X=2, Y varies from 0-2

(3,0) (3,1) (3,2) # X=3, Y varies from 0-2

Total states = (3+1) × (2+1) = 4 × 3 = 12 states

This demonstrates why both time and space complexity are O(x*y):
- Jug X: (x+1) possible values [0,1,2,...,x]
- Jug Y: (y+1) possible values [0,1,2,...,y]
- Total possible states = (x+1)(y+1) ≈ O(x*y)

## Error Handling

The API includes comprehensive error handling for:
- Invalid input values (negative numbers)
- Impossible target amounts
