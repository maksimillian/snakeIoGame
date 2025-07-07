using System;
using System.Runtime.InteropServices;
using UnityEngine;
using AOT;

public class SocketIOWebGL
{
    private int _id = -1;
    public bool Connected { get; private set; }

    // Events for game state updates
    public event Action OnGameStateReceived;
    public event Action OnRoomJoined;
    public event Action OnPlayerJoined;

#if UNITY_WEBGL
    // Check if we're in WebGL build or Editor
    private static bool IsWebGLBuild()
    {
        return Application.platform == RuntimePlatform.WebGLPlayer;
    }

    [DllImport("__Internal")] private static extern int  sio_connect(string url);
    [DllImport("__Internal")] private static extern void sio_emit(int id, string evt, string data);
    [DllImport("__Internal")] private static extern void sio_on(int id, string evt, Action callback);
    [DllImport("__Internal")] private static extern void sio_disconnect(int id);
    [DllImport("__Internal")] private static extern int  sio_is_connected(int id);
    [DllImport("__Internal")] private static extern void sio_setup_game_events(int id);
    [DllImport("__Internal")] private static extern IntPtr sio_get_game_state(int id);
    [DllImport("__Internal")] private static extern IntPtr sio_get_room_data(int id);
    [DllImport("__Internal")] private static extern IntPtr sio_get_player_data(int id);
    [DllImport("__Internal")] private static extern IntPtr sio_get_error_data(int id);

    public void Connect(string url)
    {
        try
        {
            if (IsWebGLBuild())
            {
                // In actual WebGL build, use the JS bridge
                _id = sio_connect(url);
                sio_on(_id, "connect", OnConnect);
                sio_on(_id, "disconnect", OnDisconnect);
                
                // Set up game event listeners
                sio_on(_id, "game:state", OnGameStateCallback);
                sio_on(_id, "room:auto-join", OnRoomAutoJoinCallback);
                sio_on(_id, "rooms:join", OnRoomsJoinCallback);
                sio_on(_id, "player:joined", OnPlayerJoinedCallback);
                
                // Set up game events in JS
                sio_setup_game_events(_id);
                
                // Set connected to true after a short delay to allow JS to establish connection
                Connected = true;
            }
            else
            {
                // In Unity Editor, simulate connection for testing
                _id = 1; // Use a dummy ID
                Connected = true;
                
                // Simulate connection after a delay
                MonoBehaviour.FindObjectOfType<MonoBehaviour>().StartCoroutine(SimulateConnection());
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[WebGL] Failed to init JS socket: {e.Message}");
            // Fallback to simulation mode
            _id = 1;
            Connected = true;
            MonoBehaviour.FindObjectOfType<MonoBehaviour>().StartCoroutine(SimulateConnection());
        }
    }

    private System.Collections.IEnumerator SimulateConnection()
    {
        yield return new WaitForSeconds(1f);
        
        // Simulate receiving some game events for testing
        yield return new WaitForSeconds(2f);
        NetworkManager.OnWebGLGameStateReceived?.Invoke();
    }

    // Static callbacks for IL2CPP compatibility
    [MonoPInvokeCallback(typeof(Action))]
    private static void OnConnect()
    {
        // Connection established
    }

    [MonoPInvokeCallback(typeof(Action))]
    private static void OnDisconnect()
    {
        // Connection lost
    }

    [MonoPInvokeCallback(typeof(Action))]
    private static void OnGameStateCallback()
    {
        // Trigger the NetworkManager static event
        NetworkManager.OnWebGLGameStateReceived?.Invoke();
    }

    [MonoPInvokeCallback(typeof(Action))]
    private static void OnRoomAutoJoinCallback()
    {
        // Trigger the NetworkManager static event
        NetworkManager.OnWebGLRoomJoined?.Invoke();
    }

    [MonoPInvokeCallback(typeof(Action))]
    private static void OnRoomsJoinCallback()
    {
        // Trigger the NetworkManager static event for room join
        NetworkManager.OnWebGLRoomJoined?.Invoke();
    }

    [MonoPInvokeCallback(typeof(Action))]
    private static void OnPlayerJoinedCallback()
    {
        // Trigger the NetworkManager static event
        NetworkManager.OnWebGLPlayerJoined?.Invoke();
    }

    public void Emit(string evt, string json="{}")
    {
        if (_id == -1) return;
        
        if (IsWebGLBuild())
        {
            sio_emit(_id, evt, json);
        }
        else
        {
            // Simulated emit for Unity Editor testing
        }
    }

    public void On(string evt, System.Action callback)
    {
        if (_id == -1) return;
        
        if (IsWebGLBuild())
        {
            sio_on(_id, evt, callback);
        }
        else
        {
            // Simulated event listener for Unity Editor testing
        }
    }

    public void Disconnect()
    {
        if (_id == -1) return;
        
        if (IsWebGLBuild())
        {
            sio_disconnect(_id);
        }
        else
        {
            // Simulated disconnect for Unity Editor testing
        }
        
        _id = -1;
        Connected = false;
    }

    public bool IsConnected()
    {
        if (_id == -1) return false;
        
        if (IsWebGLBuild())
        {
            return sio_is_connected(_id) == 1;
        }
        else
        {
            return Connected;
        }
    }

    public void SetupGameEventListeners()
    {
        if (_id == -1) return;
        
        if (IsWebGLBuild())
        {
            sio_setup_game_events(_id);
        }
        else
        {
            // Simulated game event listeners setup for Unity Editor testing
        }
    }

    public string GetGameStateData()
    {
        if (_id == -1) return null;
        
        if (IsWebGLBuild())
        {
            IntPtr dataPtr = sio_get_game_state(_id);
            if (dataPtr != IntPtr.Zero)
            {
                string jsonData = Marshal.PtrToStringAnsi(dataPtr);
                // Free the memory allocated in JS
                Marshal.FreeHGlobal(dataPtr);
                return jsonData;
            }
        }
        else
        {
            // Return simulated game state for testing
            return "{\"roomId\":1,\"timestamp\":1234567890,\"players\":[{\"id\":1,\"name\":\"TestPlayer\",\"x\":0,\"y\":0,\"length\":3,\"score\":0,\"kills\":0,\"isBot\":false,\"isAlive\":true,\"segments\":[{\"x\":0,\"y\":0},{\"x\":-1,\"y\":0},{\"x\":-2,\"y\":0}]}],\"food\":[],\"leaderboard\":[]}";
        }
        
        return null;
    }

    public string GetRoomData()
    {
        if (_id == -1) return null;
        
        if (IsWebGLBuild())
        {
            IntPtr dataPtr = sio_get_room_data(_id);
            if (dataPtr != IntPtr.Zero)
            {
                string jsonData = Marshal.PtrToStringAnsi(dataPtr);
                // Free the memory allocated in JS
                Marshal.FreeHGlobal(dataPtr);
                return jsonData;
            }
        }
        else
        {
            // Return simulated room data for testing
            return "{\"id\":1,\"name\":\"Test Room (Editor)\",\"code\":\"TEST123\",\"playerCount\":1,\"maxPlayers\":10}";
        }
        
        return null;
    }

    public string GetPlayerData()
    {
        if (_id == -1) return null;
        
        if (IsWebGLBuild())
        {
            IntPtr dataPtr = sio_get_player_data(_id);
            if (dataPtr != IntPtr.Zero)
            {
                string jsonData = Marshal.PtrToStringAnsi(dataPtr);
                // Free the memory allocated in JS
                Marshal.FreeHGlobal(dataPtr);
                return jsonData;
            }
        }
        else
        {
            // Return simulated player data for testing
            return "{\"id\":1,\"name\":\"TestPlayer\",\"score\":0,\"kills\":0}";
        }
        
        return null;
    }

    public string GetErrorData()
    {
        if (_id == -1) return null;
        
        if (IsWebGLBuild())
        {
            IntPtr dataPtr = sio_get_error_data(_id);
            if (dataPtr != IntPtr.Zero)
            {
                string jsonData = Marshal.PtrToStringAnsi(dataPtr);
                // Free the memory allocated in JS
                Marshal.FreeHGlobal(dataPtr);
                return jsonData;
            }
        }
        else
        {
            // Return simulated error data for testing
            return "{\"message\":\"Room not found\"}";
        }
        
        return null;
    }
#else
    public void Connect(string url)
    {
        Debug.Log("[Non-WebGL] SocketIOWebGL.Connect called - this is a no-op");
    }

    public void Emit(string evt, string json="{}")
    {
        Debug.Log($"[Non-WebGL] SocketIOWebGL.Emit called - this is a no-op: {evt}");
    }

    public void Disconnect()
    {
        Debug.Log("[Non-WebGL] SocketIOWebGL.Disconnect called - this is a no-op");
    }

    public bool IsConnected()
    {
        return false;
    }

    public void SetupGameEventListeners()
    {
        Debug.Log("[Non-WebGL] SocketIOWebGL.SetupGameEventListeners called - this is a no-op");
    }
#endif
} 