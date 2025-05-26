var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Returns the GCD of a and b
int gcd(int a, int b)
{
    if (b == 0) return a;
    return gcd(b, a % b);
}

app.MapPost("/solve", (SolveRequest request) =>
{
    (int x, int y, int target) = (request.x_capacity, request.y_capacity, request.z_amount_wanted);

    // Check if we have cached solution for these parameters 
    if (SolutionCacheManager.TryGetSolution(x, y, target, out var cachedSteps))
    {
        return Results.Ok(new SolveResponse { solution = cachedSteps });
    }

    // If any of the incoming values are negative, then we return a bad request
    if (x < 0 || y < 0 || target < 0) {
        return Results.BadRequest(new { error = "Jug capacities and target amount must be greater or equal to 0" });
    }

    // If the target is not a multiple of the gcd of x and y, then it is not possible to measure the target amount or if the target is greater than the sum of the two jugs
    if (target % gcd(x, y) != 0 || target > x + y)
    {
        return Results.BadRequest(new { error = "It's not possible to measure the target amount with the given jug capacities" });
    }


    var steps = new List<Step>();

    // If the target is 0, then the solution is to not fill any of the jugs and return immediately
    if (target == 0) {
        steps.Add(new Step 
        { 
            step = 1, 
            bucketX = 0, 
            bucketY = 0,
            status = "Solved" 
        });

        // Before returning the result, cache it
        SolutionCacheManager.CacheSolution(x, y, target, steps);
        return Results.Ok(new SolveResponse { solution = steps });
    }

    // Doing a BFS to find the shortest path to the target

    // Visited is all the states we have already visited so we don't visit the same state twice
    var visited = new HashSet<(int, int)>();

    // Parent is the parent of the current state
    var parent = new Dictionary<(int, int), Step>();

    // BFS Queue
    var queue = new Queue<Step>();
    
    // Enqueue the initial state
    queue.Enqueue(new Step {
        step = 0,
        bucketX = 0,
        bucketY = 0,
        status = "Initial State",
        action = "Start"
    });

    while (queue.Count > 0) {
        var currentStep = queue.Dequeue();
        var (currX, currY) = (currentStep.bucketX, currentStep.bucketY);

        // If either bucket x or bucket y has the target amount, then we have found a solution
        if (currX == target || currY == target) {
            // We backtrack from the solution to the initial state
            var curr = currentStep;
            while (curr != null) {
                steps.Add(curr);
                curr = parent.ContainsKey((curr.bucketX, curr.bucketY)) ? parent[(curr.bucketX, curr.bucketY)] : null;
            }

            // Now we reverse the steps so we get from initial state to solution
            steps.Reverse();

            // Set the status of the last step to solved
            steps[steps.Count - 1].status = "Solved";
            break;
        }

        // If we have already visited this state, then we skip it
        if (visited.Contains((currX, currY))) continue;
        visited.Add((currX, currY));

        // We get all the next states from the current state
        var nextStates = new List<(int, int, string)> {
            (x, currY, "Fill bucket X"),
            (currX, y, "Fill bucket Y"),
            (0, currY, "Empty bucket X"),
            (currX, 0, "Empty bucket Y"),
            (currX - Math.Min(currX, y - currY), currY + Math.Min(currX, y - currY), "Transfer from bucket X to bucket Y"), // We want to make sure when we transfer from bucket x to bucket y, we don't overflow bucket y
            (currX + Math.Min(currY, x - currX), currY - Math.Min(currY, x - currX), "Transfer from bucket Y to bucket X") // We want to make sure when we transfer from bucket y to bucket x, we don't overflow bucket x
        };

        // We iterate through all the next states
        foreach (var (newX, newY, action) in nextStates) {
            // If we haven't visited one of the next states, we queue it up for processing
            if (!visited.Contains((newX, newY))) {
                var newStep = new Step {
                    step = currentStep.step + 1,
                    bucketX = newX,
                    bucketY = newY,
                    action = action,
                };
                queue.Enqueue(newStep);
                parent[(newX, newY)] = currentStep;
            }
        }
    }

    // Before returning the result, cache it 
    SolutionCacheManager.CacheSolution(x, y, target, steps);
    return Results.Ok(new SolveResponse { solution = steps });
});


app.Run();


// Request Object 
public class SolveRequest
{
    public int x_capacity { get; set; }
    public int y_capacity { get; set; }
    public int z_amount_wanted { get; set; }
}

// Individual step in the solution
public class Step
{
    public int step { get; set; }
    public int bucketX { get; set; }
    public int bucketY { get; set; }
    public string action { get; set; }
    public string? status { get; set; } 
}

// Response Object which contains the list of steps
public class SolveResponse
{
    public List<Step> solution { get; set; }
}

// Solution Cache Manager for caching the solutions to avoid recalculating them
public static class SolutionCacheManager
{
    private static readonly Dictionary<(int x, int y, int target), List<Step>> solutionCache = new();

    public static bool TryGetSolution(int x, int y, int target, out List<Step> steps)
    {
        return solutionCache.TryGetValue((x, y, target), out steps);
    }

    public static void CacheSolution(int x, int y, int target, List<Step> steps)
    {
        solutionCache[(x, y, target)] = steps;
    }
}
