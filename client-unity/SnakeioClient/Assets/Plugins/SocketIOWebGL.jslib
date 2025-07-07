mergeInto(LibraryManager.library, {
  sio_connect: function(urlPtr) {
    const url = UTF8ToString(urlPtr);
    if (!Module.socketio_nextId) Module.socketio_nextId = 1;
    const id = Module.socketio_nextId++;
    if (!Module.sio_sockets) Module.sio_sockets = {};
    if (!Module.sio_connected) Module.sio_connected = {};
    if (!Module.sio_callbacks) Module.sio_callbacks = {};
    
    Module.sio_sockets[id] = io(url, { transports: ["websocket", "polling"] });
    Module.sio_connected[id] = false;
    
    // Set up connection event handlers
    Module.sio_sockets[id].on('connect', function() {
      Module.sio_connected[id] = true;
      
      // Call Unity method when connected
      if (Module.sio_callbacks[id] && Module.sio_callbacks[id]['connect']) {
        try {
          dynCall_v(Module.sio_callbacks[id]['connect']);
        } catch (e) {
          console.error('[WebGL] Error calling connect callback:', e);
        }
      }
    });
    
    Module.sio_sockets[id].on('disconnect', function() {
      Module.sio_connected[id] = false;
      
      // Call Unity method when disconnected
      if (Module.sio_callbacks[id] && Module.sio_callbacks[id]['disconnect']) {
        try {
          dynCall_v(Module.sio_callbacks[id]['disconnect']);
        } catch (e) {
          console.error('[WebGL] Error calling disconnect callback:', e);
        }
      }
    });
    
    return id;
  },
  sio_emit: function(id, evtPtr, dataPtr) {
    const s = Module.sio_sockets[id];
    if (!s) {
      console.warn('[WebGL] Socket not found for ID:', id);
      return;
    }
    const evt  = UTF8ToString(evtPtr);
    const data = UTF8ToString(dataPtr);
    try { 
      s.emit(evt, data && data.length ? JSON.parse(data) : {}); 
    }
    catch(e) { 
      s.emit(evt, data); 
    }
  },
  sio_on: function(id, evtPtr, cbPtr) {
    const s = Module.sio_sockets[id];
    if (!s) {
      console.warn('[WebGL] Socket not found for ID:', id);
      return;
    }
    const evt = UTF8ToString(evtPtr);
    
    // Store the callback pointer for this event
    if (!Module.sio_callbacks[id]) Module.sio_callbacks[id] = {};
    Module.sio_callbacks[id][evt] = cbPtr;
    
    s.on(evt, function(data) { 
      try {
        // Call Unity method with event data
        if (cbPtr && typeof cbPtr === 'number') {
          // For now, just call the callback without parameters
          // We can extend this later to pass data to Unity
          dynCall_v(cbPtr);
        } else {
          console.warn('[WebGL] Invalid callback pointer for event:', evt);
        }
      } catch (e) {
        console.error('[WebGL] Error calling callback for event:', evt, 'error:', e);
      }
    });
  },
  sio_disconnect: function(id) {
    const s = Module.sio_sockets[id];
    if (!s) {
      console.warn('[WebGL] Socket not found for ID:', id);
      return;
    }
    s.disconnect();
    delete Module.sio_sockets[id];
    delete Module.sio_connected[id];
    delete Module.sio_callbacks[id];
  },
  sio_is_connected: function(id) {
    return Module.sio_connected[id] ? 1 : 0;
  },
  sio_setup_game_events: function(id) {
    const s = Module.sio_sockets[id];
    if (!s) {
      console.warn('[WebGL] Socket not found for ID:', id);
      return;
    }
    
    // Set up game state event listener
    s.on('game:state', function(data) {
      // Store the game state data globally so Unity can access it
      if (!Module.gameStateData) Module.gameStateData = {};
      Module.gameStateData[id] = data;
      
      // Call Unity method to handle game state
      if (Module.sio_callbacks[id] && Module.sio_callbacks[id]['game:state']) {
        try {
          dynCall_v(Module.sio_callbacks[id]['game:state']);
        } catch (e) {
          console.error('[WebGL] Error calling game:state callback:', e);
        }
      }
    });
    
    // Set up room auto-join response listener
    s.on('room:auto-join', function(data) {
      // Store the room data globally
      if (!Module.roomData) Module.roomData = {};
      Module.roomData[id] = data;
      
      // Call Unity method to handle room join
      if (Module.sio_callbacks[id] && Module.sio_callbacks[id]['room:auto-join']) {
        try {
          dynCall_v(Module.sio_callbacks[id]['room:auto-join']);
        } catch (e) {
          console.error('[WebGL] Error calling room:auto-join callback:', e);
        }
      }
    });
    
    // Set up rooms:join response listener
    s.on('rooms:join', function(data) {
      // Store the room data globally
      if (!Module.roomData) Module.roomData = {};
      Module.roomData[id] = data;
      
      // Call Unity method to handle room join
      if (Module.sio_callbacks[id] && Module.sio_callbacks[id]['rooms:join']) {
        try {
          dynCall_v(Module.sio_callbacks[id]['rooms:join']);
        } catch (e) {
          console.error('[WebGL] Error calling rooms:join callback:', e);
        }
      }
    });
    
    // Set up player joined event listener
    s.on('player:joined', function(data) {
      // Store the player data globally
      if (!Module.playerData) Module.playerData = {};
      Module.playerData[id] = data;
      
      // Call Unity method to handle player join
      if (Module.sio_callbacks[id] && Module.sio_callbacks[id]['player:joined']) {
        try {
          dynCall_v(Module.sio_callbacks[id]['player:joined']);
        } catch (e) {
          console.error('[WebGL] Error calling player:joined callback:', e);
        }
      }
    });
    
    // Set up error event listener
    s.on('error', function(data) {
      // Store the error data globally
      if (!Module.errorData) Module.errorData = {};
      Module.errorData[id] = data;
      
      // Call Unity method to handle error
      if (Module.sio_callbacks[id] && Module.sio_callbacks[id]['error']) {
        try {
          dynCall_v(Module.sio_callbacks[id]['error']);
        } catch (e) {
          console.error('[WebGL] Error calling error callback:', e);
        }
      }
    });
  },
  
  // New function to get game state data from JS - fixed to avoid stringToUTF8
  sio_get_game_state: function(id) {
    if (Module.gameStateData && Module.gameStateData[id]) {
      const data = Module.gameStateData[id];
      const jsonString = JSON.stringify(data);
      
      // Allocate memory for the string
      const buffer = Module._malloc(jsonString.length + 1);
      
      // Copy string to buffer using a simple loop
      for (let i = 0; i < jsonString.length; i++) {
        Module.HEAPU8[buffer + i] = jsonString.charCodeAt(i);
      }
      Module.HEAPU8[buffer + jsonString.length] = 0; // null terminator
      
      return buffer;
    }
    return 0;
  },
  
  // New function to get room data from JS - fixed to avoid stringToUTF8
  sio_get_room_data: function(id) {
    if (Module.roomData && Module.roomData[id]) {
      const data = Module.roomData[id];
      const jsonString = JSON.stringify(data);
      
      // Allocate memory for the string
      const buffer = Module._malloc(jsonString.length + 1);
      
      // Copy string to buffer using a simple loop
      for (let i = 0; i < jsonString.length; i++) {
        Module.HEAPU8[buffer + i] = jsonString.charCodeAt(i);
      }
      Module.HEAPU8[buffer + jsonString.length] = 0; // null terminator
      
      return buffer;
    }
    return 0;
  },
  
  // New function to get player data from JS - fixed to avoid stringToUTF8
  sio_get_player_data: function(id) {
    if (Module.playerData && Module.playerData[id]) {
      const data = Module.playerData[id];
      const jsonString = JSON.stringify(data);
      
      // Allocate memory for the string
      const buffer = Module._malloc(jsonString.length + 1);
      
      // Copy string to buffer using a simple loop
      for (let i = 0; i < jsonString.length; i++) {
        Module.HEAPU8[buffer + i] = jsonString.charCodeAt(i);
      }
      Module.HEAPU8[buffer + jsonString.length] = 0; // null terminator
      
      return buffer;
    }
    return 0;
  },
  
  // New function to get error data from JS - fixed to avoid stringToUTF8
  sio_get_error_data: function(id) {
    if (Module.errorData && Module.errorData[id]) {
      const data = Module.errorData[id];
      const jsonString = JSON.stringify(data);
      
      // Allocate memory for the string
      const buffer = Module._malloc(jsonString.length + 1);
      
      // Copy string to buffer using a simple loop
      for (let i = 0; i < jsonString.length; i++) {
        Module.HEAPU8[buffer + i] = jsonString.charCodeAt(i);
      }
      Module.HEAPU8[buffer + jsonString.length] = 0; // null terminator
      
      return buffer;
    }
    return 0;
  }
}); 