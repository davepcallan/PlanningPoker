using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

public class PlanningPokerHub : Hub
{
    private static ConcurrentDictionary<string, string> ConnectedUsers = new ConcurrentDictionary<string, string>();
    private static ConcurrentDictionary<string, Dictionary<string, string>> TaskVotes = new ConcurrentDictionary<string, Dictionary<string, string>>();

    // Hardcoded tasks for Planning Poker
    private static List<string> Tasks = new List<string>
    {
        "Implement User Login",
        "Create Database Schema",
        "Develop API Endpoints",
        "Design Frontend UI",
        "Write Unit Tests"
    };

    // When a client joins the session
    public async Task Join(string username)
    {
        if (!string.IsNullOrWhiteSpace(username))
        {
            ConnectedUsers[Context.ConnectionId] = username;
            await BroadcastUsers();
        }
    }

    // When a client submits their vote for a task
    public async Task SubmitVote(string taskName, string username, string vote)
    {
        if (!TaskVotes.ContainsKey(taskName))
        {
            TaskVotes[taskName] = new Dictionary<string, string>();
        }

        // Record the vote for the user
        TaskVotes[taskName][username] = vote;

        // Broadcast updated vote status to all clients
        await BroadcastVoteStatus(taskName);

        // If all users have voted for this task, reveal votes
        if (TaskVotes[taskName].Count == ConnectedUsers.Count)
        {
            await RevealVotes(taskName);
        }
    }

    // Broadcast the list of connected users
    private Task BroadcastUsers()
    {
        var users = ConnectedUsers.Values.ToList();
        return Clients.All.SendAsync("UpdateUsers", users);
    }

    // Broadcast the voting status of a specific task
    private Task BroadcastVoteStatus(string taskName)
    {
        var votedUsers = TaskVotes[taskName].Keys.ToList();
        return Clients.All.SendAsync("UpdateVoteStatus", taskName, votedUsers);
    }

    // Reveal votes for a specific task
    private Task RevealVotes(string taskName)
    {
        var votes = TaskVotes[taskName];
        return Clients.All.SendAsync("RevealVotes", taskName, votes);
    }

    // When a client disconnects
    public override Task OnDisconnectedAsync(System.Exception? exception)
    {
        if (ConnectedUsers.TryRemove(Context.ConnectionId, out _))
        {
            BroadcastUsers();
        }

        return base.OnDisconnectedAsync(exception);
    }

    // Get the list of tasks (called by the client when loading)
    public Task<List<string>> GetTasks()
    {
        return Task.FromResult(Tasks);
    }
}
