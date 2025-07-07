using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class CommandHandler : MonoBehaviour
{
    private NetworkManager networkManager;

    private void Awake()
    {
        networkManager = GetComponent<NetworkManager>();
        if (networkManager == null)
        {
            Debug.LogError("NetworkManager component not found!");
        }
    }

    private async Task HandleConnect(string[] args)
    {
        if (args.Length < 2)
        {
            Debug.LogError("Usage: connect <server_url>");
            return;
        }

        string serverUrl = args[1];
        try
        {
            Debug.Log($"Connecting to server: {serverUrl}");
            await networkManager.Connect(serverUrl);
            Debug.Log("Connected successfully!");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to connect: {ex.Message}");
        }
    }

    private async Task HandleDisconnect()
    {
        try
        {
            Debug.Log("Disconnecting from server...");
            await networkManager.Disconnect();
            Debug.Log("Disconnected successfully!");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to disconnect: {ex.Message}");
        }
    }

    private async Task HandleServerList()
    {
        try
        {
            Debug.Log("Fetching server list...");
            var servers = await networkManager.GetServers();
            Debug.Log($"Found {servers.Count} servers:");
            foreach (var server in servers)
            {
                Debug.Log($"- {server.name} (ID: {server.id}, Status: {server.status}, Players: {server.playerCount})");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to get server list: {ex.Message}");
        }
    }

    private async Task HandleRoomList()
    {
        try
        {
            Debug.Log("Fetching room list...");
            var rooms = await networkManager.GetRooms();
            Debug.Log($"Found {rooms.Count} rooms:");
            foreach (var room in rooms)
            {
                Debug.Log($"- {room.name} (ID: {room.id}, Players: {room.players}/{room.maxPlayers})");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to get room list: {ex.Message}");
        }
    }

    private async Task HandleRoomCreate(string[] args)
    {
        if (args.Length < 2)
        {
            Debug.LogError("Usage: room-create <room_name>");
            return;
        }

        string roomName = args[1];
        try
        {
            Debug.Log($"Creating room: {roomName}");
            var room = await networkManager.CreateRoom(roomName);
            Debug.Log($"Room created successfully: {room.name} (ID: {room.id})");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to create room: {ex.Message}");
        }
    }

    private async void ProcessCommand(string command)
    {
        string[] args = command.Split(' ');
        string cmd = args[0].ToLower();

        switch (cmd)
        {
            case "connect":
                await HandleConnect(args);
                break;
            case "disconnect":
                await HandleDisconnect();
                break;
            case "server-list":
                await HandleServerList();
                break;
            case "room-list":
                await HandleRoomList();
                break;
            case "room-create":
                await HandleRoomCreate(args);
                break;
            default:
                Debug.LogWarning($"Unknown command: {cmd}");
                break;
        }
    }
} 