using Microsoft.AspNetCore.SignalR;

namespace OnlyOfficeServer.Hubs;

public class OnlyOfficeHub : Hub
{
    public async Task JoinFileRoom(string fileId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"file-{fileId}");
        Console.WriteLine($"[SIGNALR] Client {Context.ConnectionId} joined room for file {fileId}");
    }

    public async Task LeaveFileRoom(string fileId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"file-{fileId}");
        Console.WriteLine($"[SIGNALR] Client {Context.ConnectionId} left room for file {fileId}");
    }

    public override async Task OnConnectedAsync()
    {
        Console.WriteLine($"[SIGNALR] Client connected: {Context.ConnectionId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Console.WriteLine($"[SIGNALR] Client disconnected: {Context.ConnectionId}");
        await base.OnDisconnectedAsync(exception);
    }
}
