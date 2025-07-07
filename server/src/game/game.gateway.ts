import {
  WebSocketGateway,
  WebSocketServer,
  SubscribeMessage,
  MessageBody,
  ConnectedSocket,
  OnGatewayConnection,
  OnGatewayDisconnect
} from '@nestjs/websockets';
import { Server, Socket } from 'socket.io';
import { RoomsService } from '../rooms/rooms.service';
import { GameService, Snake, Food, GameState } from './game.service';
import { PlayerService } from './player.service';
import { JwtService } from '@nestjs/jwt';
import { UseGuards, Logger, OnModuleInit } from '@nestjs/common';
import { WsJwtAuthGuard } from '../auth/guards/ws-jwt-auth.guard';
import { Room } from '@prisma/client';

interface PlayerData {
  id: number;
  username: string;
  roomId?: number;
}

@WebSocketGateway({
  cors: {
    origin: '*',
    methods: ['GET', 'POST'],
    credentials: true,
  },
  // namespace: '/game', // Disabled - using root namespace
  transports: ['websocket'],
})
// @UseGuards(WsJwtAuthGuard) // Temporarily disabled for testing
export class GameGateway implements OnGatewayConnection, OnGatewayDisconnect, OnModuleInit {
  @WebSocketServer()
  server: Server;

  private logger = new Logger(GameGateway.name);
  private connectedPlayers: Map<string, PlayerData> = new Map();

  constructor(
    private roomsService: RoomsService,
    private gameService: GameService,
    private playerService: PlayerService,
    private jwtService: JwtService,
  ) {}

  onModuleInit() {
    // Set the server reference in the game service after the gateway is fully initialized
    this.gameService.setServer(this.server);
    console.log('GameGateway initialized - server reference set in game service');
  }

  async handleConnection(client: Socket) {
    try {
      console.log('New client attempting to connect:', client.id);
      
      // For testing, accept all connections
      console.log('Accepting connection from client:', client.id);
      
      // Don't create a player immediately - wait for first room join
      // This allows the client to send their preferred name
      console.log(`Client ${client.id} connected, waiting for room join to create player`);
      
      // Set up event listeners before sending connection established
      client.on('server:list', () => {
        console.log('Received server:list request from client:', client.id);
        this.handleServerList(client);
      });

      // Add custom handler for room:list
      client.on('room:list', () => {
        console.log('Received room:list request from client:', client.id);
        this.handleRoomList(client);
      });

      // Add custom handler for room:create
      client.on('room:create', (data) => {
        console.log('Received room:create request from client:', client.id);
        this.handleRoomCreate(client, data);
      });

      // Add custom handler for ping
      client.on('ping', () => {
        console.log('Received ping request from client:', client.id);
        this.handlePing(client);
      });

      // Add custom handler for room:auto-join
      client.on('room:auto-join', (data) => {
        console.log('Received room:auto-join request from client:', client.id, 'with data:', data);
        this.handleRoomAutoJoin(client, data);
      });

      // Add custom handler for rooms:join (friend code or room ID)
      client.on('rooms:join', (data) => {
        console.log('Received rooms:join request from client:', client.id, 'with data:', data);
        this.handleRoomsJoin(data, client);
      });

      // Add custom handler for player:join (joining specific game room)
      client.on('player:join', (data) => {
        console.log('Received player:join request from client:', client.id, 'with data:', data);
        this.handlePlayerJoin(data, client);
      });

      // Add custom handler for snake:input (new server-authoritative input handling)
      client.on('snake:input', (data) => {
        this.handleSnakeInput(data, client);
      });

      // Add custom handler for test:spawn-bots (testing bot spawning)
      client.on('test:spawn-bots', (data) => {
        console.log('Received test:spawn-bots request from client:', client.id, 'with data:', data);
        this.handleTestSpawnBots(data, client);
      });

      // Send connection established event
      console.log('Sending connection:established event to client:', client.id);
      client.emit('connection:established', { 
        status: 'connected', 
        clientId: client.id,
        timestamp: new Date().toISOString()
      });

      console.log('Client connected successfully:', client.id);
    } catch (error) {
      console.error('Connection error:', error);
      client.disconnect();
    }
  }

  async handleDisconnect(client: Socket) {
    try {
      console.log('Client disconnected:', client.id);
      
      // Get player data before removing it
      const playerData = this.connectedPlayers.get(client.id);
      
      if (playerData && playerData.roomId) {
        console.log(`Player ${playerData.username} disconnected from room ${playerData.roomId}`);
        
        // Leave the socket room
        client.leave(`room_${playerData.roomId}`);
        
        // Update room player count in database
        try {
          const room = await this.roomsService.findOne(playerData.roomId);
          if (room) {
            const newPlayerCount = Math.max(0, (room as any).currentPlayers - 1);
            await this.roomsService.updateRoomPlayerCount(playerData.roomId, newPlayerCount);
            console.log(`Updated room ${playerData.roomId} player count to ${newPlayerCount}`);
          }
        } catch (error) {
          console.error('Error updating room player count:', error);
        }
        
        // Notify other players in the room about the disconnect
        this.server.to(`room_${playerData.roomId}`).emit('player:left', {
          playerId: playerData.id,
          username: playerData.username,
          roomId: playerData.roomId
        });
        
        // Clear room from player data
        delete playerData.roomId;
      }
      
      // Remove player from connected players
      this.connectedPlayers.delete(client.id);
      
      // Handle game service cleanup
      await this.gameService.handlePlayerDisconnect(client.id);
      
      console.log(`Client ${client.id} cleanup completed`);
    } catch (error) {
      console.error('Disconnect error:', error);
    }
  }

  async handlePing(client: Socket) {
    console.log('=== PING HANDLER CALLED ===');
    console.log('Received ping from client:', client.id);
    console.log('Sending pong response...');
    client.emit('pong', { timestamp: new Date().toISOString() });
    console.log('Pong response sent');
  }

  @SubscribeMessage('server:list')
  async handleServerList(client: Socket) {
    try {
      console.log('Processing server:list request from client:', client.id);
      const servers = await this.gameService.getServers();
      console.log('Sending server list to client:', client.id, servers);
      
      // Convert servers to plain objects to ensure proper serialization
      const serverList = servers.map(server => ({
        id: server.id,
        name: server.name,
        status: server.status,
        playerCount: server.playerCount
      }));
      
      console.log('Serialized server list:', JSON.stringify(serverList));
      
      // Send the response as raw object, not JSON string
      client.emit('server:list', serverList);
      console.log('Server list sent successfully to client:', client.id);
    } catch (error) {
      console.error('Error in handleServerList:', error);
      client.emit('error', { message: error.message });
    }
  }

  @SubscribeMessage('room:list')
  async handleRoomList(client: Socket) {
    console.log('handleRoomList called for client:', client.id);
    try {
      console.log('Processing room:list request from client:', client.id);
      const rooms = await this.gameService.getRooms();
      const roomList = rooms.map(room => ({
        id: room.id,
        name: room.name,
        players: room.players,
        maxPlayers: room.maxPlayers
      }));
      
      // Create the response object
      const response = {
        rooms: roomList
      };
      
      console.log('Sending room list response:', JSON.stringify(response));
      
      // Send the response
      client.emit('room:list', response);
      console.log('Room list sent successfully to client:', client.id);
    } catch (error) {
      console.error('Error in handleRoomList:', error);
      client.emit('error', { message: error.message });
    }
  }

  @SubscribeMessage('room:create')
  async handleRoomCreate(client: Socket, data: { name: string }) {
    try {
      console.log('Received room:create request from client:', client.id, 'with name:', data.name);
      const room = await this.gameService.createRoom(data.name);
      console.log('Created room:', room);
      client.emit('room:create', room);
    } catch (error) {
      console.error('Error in handleRoomCreate:', error);
      client.emit('error', { message: error.message });
    }
  }

  async handleRoomAutoJoin(client: Socket, data?: any) {
    try {
      console.log('Received room:auto-join request from client:', client.id);
      
      // Set up timeout for the entire auto-join process
      const timeoutPromise = new Promise((_, reject) => {
        setTimeout(() => {
          console.log('Auto-join timeout triggered after 5 seconds');
          reject(new Error('Auto-join request timed out after 5 seconds'));
        }, 5000);
      });

      console.log('Starting auto-join process...');
      const autoJoinPromise = this.performAutoJoin(client, data);
      
      // Race between the auto-join process and timeout
      console.log('Racing auto-join against timeout...');
      await Promise.race([autoJoinPromise, timeoutPromise]);
      console.log('Auto-join completed successfully');
    } catch (error) {
      console.error('Error in handleRoomAutoJoin:', error);
      client.emit('error', { message: error.message });
    }
  }

  private async performAutoJoin(client: Socket, data?: any) {
    console.log('performAutoJoin: Starting...');
    
    // Get or create player data
    let playerData = this.connectedPlayers.get(client.id);
      if (!playerData) {
      console.log('performAutoJoin: Creating new player...');
      // Create a new player with the provided name or default name
      const playerName = (data && data.playerName && typeof data.playerName === 'string') 
        ? data.playerName.trim() 
        : `Player_${client.id.substring(0, 8)}`;
      
      const newPlayer = await this.playerService.createGuestPlayer(playerName);
      playerData = {
        id: newPlayer.id,
        username: newPlayer.username,
        roomId: undefined
      };
      this.connectedPlayers.set(client.id, playerData);
      console.log(`performAutoJoin: Created new player: ${playerData.username} (ID: ${playerData.id})`);
    } else {
      console.log('performAutoJoin: Using existing player...');
      // Update existing player name if provided in the request
      if (data && data.playerName && typeof data.playerName === 'string') {
        const newName = data.playerName as string;
        if (newName.trim() !== '') {
          // Update the player name in the database
          await this.playerService.updatePlayerName(playerData.id, newName.trim());
          playerData.username = newName.trim();
          this.connectedPlayers.set(client.id, playerData);
          console.log(`performAutoJoin: Updated player ${playerData.id} name to: ${newName.trim()}`);
        }
      }
      }

      // Check if player is already in a room
      if (playerData.roomId) {
      console.log(`performAutoJoin: Player ${playerData.username} is already in room ${playerData.roomId}, leaving first...`);
        await this.handlePlayerLeave(client);
      }

    console.log('performAutoJoin: Finding available room...');
      // Auto-join best available room
      let room = await this.roomsService.autoJoinRoom();
      
      // If no room is found, create a new one and join it
      if (!room) {
      console.log('performAutoJoin: No room found, creating new room...');
        // Use the new getOrCreateServerId method
        const serverId = await this.gameService.getOrCreateServerId();
      const roomNumber = Math.floor(Math.random() * 1000) + 1;
      room = await this.roomsService.createRoom(`Game Room ${roomNumber}`, serverId);
      }
      
    console.log(`performAutoJoin: Joining room ${room.id}...`);
      // Join the socket room
      client.join(`room_${room.id}`);
      
      // Update player data
      playerData.roomId = room.id;
      this.connectedPlayers.set(client.id, playerData);

    console.log('performAutoJoin: Creating player session...');
      // Create player session
      const session = await this.gameService.createPlayerSession(playerData.id, room.id);
      
    console.log('performAutoJoin: Spawning snake...');
      // Automatically spawn a snake for the player
      const snake = await this.gameService.spawnSnake(client.id, room.id, playerData.id);
    console.log(`performAutoJoin: Snake spawned for player ${playerData.username} in room ${room.id}`);
      
    console.log(`performAutoJoin: Player ${playerData.username} auto-joined room ${room.name} (ID: ${room.id})`);
      
      // Send room data back to client
      client.emit('room:auto-join', {
        id: room.id,
        name: room.name,
        friendCode: (room as any).friendCode,
        currentPlayers: (room as any).currentPlayers,
        maxPlayers: (room as any).maxPlayers,
        botCount: (room as any).botCount
      });

      // Also send player:joined event for consistency
      client.emit('player:joined', {
        sessionId: session.id,
        roomId: room.id,
        player: {
          id: playerData.id,
          username: playerData.username,
        },
        localPlayerId: playerData.id,
      });
    
    console.log('performAutoJoin: Completed successfully');
  }

  async handleRoomsJoin(
    data: { friendCode?: string; roomId?: string | number; playerName?: string },
    client: Socket,
  ) {
    try {
      console.log('Received rooms:join request from client:', client.id, 'with data:', data);
      
      // Set up timeout for the entire join process
      const timeoutPromise = new Promise((_, reject) => {
        setTimeout(() => {
          console.log('Room join timeout triggered after 5 seconds');
          reject(new Error('Room join request timed out after 5 seconds'));
        }, 5000);
      });

      console.log('Starting room join process...');
      const joinPromise = this.performRoomJoin(data, client);
      
      // Race between the join process and timeout
      console.log('Racing room join against timeout...');
      await Promise.race([joinPromise, timeoutPromise]);
      console.log('Room join completed successfully');
    } catch (error) {
      console.error('Error in handleRoomsJoin:', error);
      client.emit('error', { message: error.message });
    }
  }

  private async performRoomJoin(
    data: { friendCode?: string; roomId?: string | number; playerName?: string },
    client: Socket,
  ) {
    console.log('performRoomJoin: Starting...');
      
    // Get or create player data
    let playerData = this.connectedPlayers.get(client.id);
      if (!playerData) {
      console.log('performRoomJoin: Creating new player...');
      // Create a new player with the provided name or default name
      const playerName = (data.playerName && typeof data.playerName === 'string') 
        ? data.playerName.trim() 
        : `Player_${client.id.substring(0, 8)}`;
      
      const newPlayer = await this.playerService.createGuestPlayer(playerName);
      playerData = {
        id: newPlayer.id,
        username: newPlayer.username,
        roomId: undefined
      };
      this.connectedPlayers.set(client.id, playerData);
      console.log(`performRoomJoin: Created new player: ${playerData.username} (ID: ${playerData.id})`);
    } else {
      console.log('performRoomJoin: Using existing player...');
      // Update existing player name if provided in the request
      if (data.playerName && typeof data.playerName === 'string') {
        const newName = data.playerName as string;
        if (newName.trim() !== '') {
          // Update the player name in the database
          await this.playerService.updatePlayerName(playerData.id, newName.trim());
          playerData.username = newName.trim();
          this.connectedPlayers.set(client.id, playerData);
          console.log(`performRoomJoin: Updated player ${playerData.id} name to: ${newName.trim()}`);
        }
      }
      }

      // Check if player is already in a room
      if (playerData.roomId) {
      console.log(`performRoomJoin: Player ${playerData.username} is already in room ${playerData.roomId}, leaving first...`);
        await this.handlePlayerLeave(client);
      }

    let room;
    
    // Handle friend code join
    if (data.friendCode) {
      console.log(`performRoomJoin: Joining room by friend code: ${data.friendCode}`);
      room = await this.roomsService.findRoomByFriendCode(data.friendCode);
    }
    // Handle room ID join
    else if (data.roomId) {
      console.log(`performRoomJoin: Joining room by ID: ${data.roomId}`);
      
      // Check if roomId is a valid number
      const roomId = typeof data.roomId === 'string' ? parseInt(data.roomId) : data.roomId;
      
      if (isNaN(roomId)) {
        console.log(`performRoomJoin: Invalid room ID: ${data.roomId} - not a valid number`);
        client.emit('error', { message: 'Invalid room ID - must be a number' });
        return;
      }
      
      room = await this.roomsService.findOne(roomId);
    }
    else {
      console.log('performRoomJoin: No friend code or room ID provided');
      client.emit('error', { message: 'No friend code or room ID provided' });
      return;
    }
    
    if (!room) {
      console.log('performRoomJoin: Room not found');
      client.emit('error', { message: 'Room not found' });
      throw new Error('Room not found');
    }
    
    if ((room as any).currentPlayers >= (room as any).maxPlayers) {
      console.log('performRoomJoin: Room is full');
      client.emit('error', { message: 'Room is full' });
      throw new Error('Room is full');
    }
    
    console.log(`performRoomJoin: Joining room ${room.id}...`);
        // Join the socket room
        client.join(`room_${room.id}`);
        
        // Update player data
        playerData.roomId = room.id;
        this.connectedPlayers.set(client.id, playerData);

    console.log('performRoomJoin: Creating player session...');
        // Create player session
        const session = await this.gameService.createPlayerSession(playerData.id, room.id);
        
    console.log('performRoomJoin: Spawning snake...');
        // Automatically spawn a snake for the player
        const snake = await this.gameService.spawnSnake(client.id, room.id, playerData.id);
    console.log(`performRoomJoin: Snake spawned for player ${playerData.username} in room ${room.id}`);
        
    console.log(`performRoomJoin: Player ${playerData.username} joined room ${room.name} (ID: ${room.id})`);
        
        // Send room data back to client
    client.emit('rooms:join', {
          id: room.id,
          name: room.name,
          friendCode: (room as any).friendCode,
          currentPlayers: (room as any).currentPlayers,
          maxPlayers: (room as any).maxPlayers,
          botCount: (room as any).botCount
        });

        // Also send player:joined event for consistency
        client.emit('player:joined', {
          sessionId: session.id,
          roomId: room.id,
          player: {
            id: playerData.id,
            username: playerData.username,
          },
          localPlayerId: playerData.id,
        });
    
    console.log('performRoomJoin: Completed successfully');
  }

  async handlePlayerJoin(
    data: { roomId: number | string },
    client: Socket,
  ) {
    try {
      const playerData = this.connectedPlayers.get(client.id);
      if (!playerData) {
        client.emit('error', { message: 'Player not authenticated' });
        return;
      }

      // Convert roomId to number if it's a string
      const roomId = typeof data.roomId === 'string' ? parseInt(data.roomId, 10) : data.roomId;

      // Check if player is already in a room
      if (playerData.roomId) {
        console.log(`Player ${playerData.username} is already in room ${playerData.roomId}, leaving first...`);
        
        // Leave current room first
        await this.handlePlayerLeave(client);
        
        // Get updated player data after leaving
        const updatedPlayerData = this.connectedPlayers.get(client.id);
        if (!updatedPlayerData) {
          client.emit('error', { message: 'Player data lost after leaving room' });
          return;
        }
      }

      // Join the new room
      client.join(`room_${roomId}`);
      
      // Update player data
      playerData.roomId = roomId;
      this.connectedPlayers.set(client.id, playerData);

      console.log(`Player ${playerData.username} joined room ${roomId}`);

      // Create or get player session
      const session = await this.gameService.createPlayerSession(playerData.id, roomId);
      console.log(`Created session ${session.id} for player ${playerData.id}`);
      
      // Automatically spawn a snake for the player
      const snake = await this.gameService.spawnSnake(client.id, roomId, playerData.id);
      console.log(`Snake spawned for player ${playerData.username} (ID: ${playerData.id}) in room ${roomId} - Snake ID: ${snake?.id}`);
      
      // Get room state
      const roomState = await this.gameService.getGameState(roomId);
      console.log(`Room state has ${roomState.players?.length || 0} players`);
      
      // Emit room state to all players in the room
      this.server.to(`room_${roomId}`).emit('room:state', {
        roomId: roomId,
        players: roomState.players,
        food: roomState.food,
        leaderboard: roomState.leaderboard,
      });

      console.log(`Sending player:joined event to client ${client.id} with localPlayerId: ${playerData.id}`);
      client.emit('player:joined', {
        sessionId: session.id,
        roomId: roomId,
        player: {
          id: playerData.id,
          username: playerData.username,
        },
        localPlayerId: playerData.id,
      });

    } catch (error) {
      console.error('Player join error:', error);
      client.emit('error', { message: 'Failed to join room' });
    }
  }

  @SubscribeMessage('player:leave')
  async handlePlayerLeave(@ConnectedSocket() client: Socket) {
    try {
      const playerData = this.connectedPlayers.get(client.id);
      if (!playerData?.roomId) {
        console.log(`Player ${client.id} tried to leave but is not in a room`);
        return;
      }

      const roomId = playerData.roomId;
      console.log(`Player ${playerData.username} leaving room ${roomId}`);

      // Handle snake death before leaving the room
      const snake = this.gameService.getSnakeBySocketId(client.id);
      if (snake && snake.isAlive) {
        console.log(`Player ${playerData.username} leaving room - marking snake as dead and spawning food`);
        
        // Mark the snake as dead
        snake.isAlive = false;
        snake.deathTime = Date.now();
        
        // Spawn food at the snake's position and body segments
        await this.gameService.spawnFoodAtSnakeDeath(snake);
        
        // Immediately broadcast updated game state
        const roomSnakes = this.gameService.getSnakesInRoom(roomId);
        const gameState = await this.gameService.buildGameState(roomId, roomSnakes);
        this.server.to(`room_${roomId}`).emit('game:state', gameState);
        console.log(`Game state broadcast sent for room ${roomId} after player leave`);
      }

      // Leave the socket room
      client.leave(`room_${roomId}`);
      
      // Update game state - mark session as inactive
      try {
        await this.gameService.leaveRoom(playerData.id);
      } catch (error) {
        console.error('Error updating game state on leave:', error);
      }
      
      // Clear room from player data
      delete playerData.roomId;
      this.connectedPlayers.set(client.id, playerData);

      // Notify other players in the room
      this.server.to(`room_${roomId}`).emit('player:left', {
        playerId: playerData.id,
        username: playerData.username,
        roomId: roomId
      });

      client.emit('player:left', { roomId: roomId });

      console.log(`Player ${playerData.username} successfully left room ${roomId}`);

    } catch (error) {
      console.error('Player leave error:', error);
    }
  }

  @SubscribeMessage('snake:spawn')
  async handleSnakeSpawn(
    client: Socket,
    data: { roomId: number; playerId: number },
  ): Promise<Snake> {
    try {
      const snake = await this.gameService.spawnSnake(client.id, data.roomId, data.playerId);
      this.broadcastGameState(data.roomId);
      return snake;
    } catch (error) {
      client.emit('error', { message: error.message });
      return null;
    }
  }

  @SubscribeMessage('snake:input')
  async handleSnakeInput(
    data: { direction: { x: number; y: number }; isBoosting: boolean },
    client: Socket,
  ) {
    try {
      const playerData = this.connectedPlayers.get(client.id);
      if (!playerData || !playerData.roomId) {
        // Don't emit error for input when not in room - just ignore it
        return;
      }

      // Check if player has a snake spawned
      const snake = this.gameService.getSnakeBySocketId(client.id);
      if (!snake) {
        // Don't emit error for input when snake not spawned - just ignore it
        return;
      }

      // Store the input in the game service - the server will process it in the game loop
      await this.gameService.handlePlayerInput(client.id, data.direction, data.isBoosting);
    } catch (error) {
      console.error('Error in handleSnakeInput:', error);
      // Don't emit error to client for input processing errors
    }
  }

  @SubscribeMessage('snake:move')
  async handleSnakeMove(
    @MessageBody() data: { x: number; y: number; r?: number; rotation?: number; b?: boolean; isBoosting?: boolean; t?: number; timestamp?: number },
    @ConnectedSocket() client: Socket,
  ) {
    try {
      const playerData = this.connectedPlayers.get(client.id);
      if (!playerData || !playerData.roomId) {
        client.emit('error', { message: 'Player not in a room' });
        return;
      }

      // Convert position to direction (normalized vector from current position to target)
      const snake = this.gameService.getSnakeBySocketId(client.id);
      if (!snake) {
        client.emit('error', { message: 'Snake not found' });
        return;
      }

      const dx = data.x - snake.position.x;
      const dy = data.y - snake.position.y;
      const length = Math.sqrt(dx * dx + dy * dy);
      
      const direction = length > 0 ? { x: dx / length, y: dy / length } : { x: 0, y: 0 };
      const isBoosting = data.b || data.isBoosting || false;

      // Store the input in the game service - the server will process it in the game loop
      await this.gameService.handlePlayerInput(client.id, direction, isBoosting);
    } catch (error) {
      console.error('Error in handleSnakeMove:', error);
      client.emit('error', { message: error.message });
    }
  }

  @SubscribeMessage('snake:boost')
  async handleSnakeBoost(
    @MessageBody() data: { isBoosting: boolean; sessionId: number },
    @ConnectedSocket() client: Socket,
  ) {
    try {
      const playerData = this.connectedPlayers.get(client.id);
      if (!playerData?.roomId) {
        return;
      }

      // Handle boost logic here
      // For now, just broadcast the boost state
      this.server.to(`room_${playerData.roomId}`).emit('snake:boost', {
        sessionId: data.sessionId,
        isBoosting: data.isBoosting,
      });

    } catch (error) {
      console.error('Snake boost error:', error);
    }
  }

  @SubscribeMessage('snake:die')
  async handleSnakeDie(
    @MessageBody() data: { sessionId: number },
    @ConnectedSocket() client: Socket,
  ) {
    try {
      const playerData = this.connectedPlayers.get(client.id);
      if (!playerData?.roomId) {
        return;
      }

      // Get the snake and mark it as dead
      const snake = this.gameService.getSnakeBySocketId(client.id);
      if (snake && snake.isAlive) {
        console.log(`Snake ${snake.id} (player ${snake.playerId}) died from client death event`);
        
        // Mark the snake as dead
        this.gameService.handleSnakeDeath(snake, 'collision');
        
        // Spawn food at the snake's position and body segments
        await this.gameService.spawnFoodAtSnakeDeath(snake);
      }
      
      // Get updated game state
      const roomState = await this.gameService.getGameState(playerData.roomId);
      
      // Broadcast to all players in the room
      this.server.to(`room_${playerData.roomId}`).emit('game:state', roomState);
      
      // Emit specific death event
      this.server.to(`room_${playerData.roomId}`).emit('snake:die', {
        sessionId: data.sessionId,
      });

    } catch (error) {
      console.error('Snake die error:', error);
    }
  }

  @SubscribeMessage('snake:eat')
  async handleSnakeEat(
    @MessageBody() data: { sessionId: number; foodId: number },
    @ConnectedSocket() client: Socket,
  ) {
    try {
      const playerData = this.connectedPlayers.get(client.id);
      if (!playerData?.roomId) {
        return;
      }

      // Update player stats
      const updatedSession = await this.gameService.eatFood(data.sessionId, data.foodId);
      
      // Get updated game state
      const roomState = await this.gameService.getGameState(playerData.roomId);
      
      // Broadcast to all players in the room
      this.server.to(`room_${playerData.roomId}`).emit('game:state', roomState);
      
      // Emit specific eat event
      this.server.to(`room_${playerData.roomId}`).emit('snake:eat', {
        sessionId: data.sessionId,
        foodId: data.foodId,
        newScore: updatedSession.score,
        newLength: updatedSession.length,
      });

    } catch (error) {
      console.error('Snake eat error:', error);
    }
  }

  @SubscribeMessage('snake:kill')
  async handleSnakeKill(
    @MessageBody() data: { killerSessionId: number; victimSessionId: number },
    @ConnectedSocket() client: Socket,
  ) {
    try {
      const playerData = this.connectedPlayers.get(client.id);
      if (!playerData?.roomId) {
        return;
      }

      // Find the killer and victim snakes
      const killer = this.gameService.getSnakeBySocketId(client.id);
      const victim = this.gameService.getAllSnakes().find(snake => 
        snake.playerId === data.victimSessionId && snake.roomId === playerData.roomId
      );
      
      if (killer && victim && killer.isAlive && victim.isAlive) {
        console.log(`Client reported kill: ${killer.playerId} killed ${victim.playerId}`);
        
        // Update killer stats
        killer.score += victim.score;
        killer.kills += 1;
        
        // Mark victim as dead
        this.gameService.handleSnakeDeath(victim, 'collision');
        
        // Spawn food at victim's position
        await this.gameService.spawnFoodAtSnakeDeath(victim);
      }
      
      // Get updated game state
      const roomState = await this.gameService.getGameState(playerData.roomId);
      
      // Broadcast to all players in the room
      this.server.to(`room_${playerData.roomId}`).emit('game:state', roomState);
      
      // Emit specific kill event
      this.server.to(`room_${playerData.roomId}`).emit('snake:kill', {
        killerSessionId: data.killerSessionId,
        victimSessionId: data.victimSessionId,
      });

    } catch (error) {
      console.error('Snake kill error:', error);
    }
  }

  @SubscribeMessage('skin:equip')
  async handleSkinEquip(client: Socket, data: { skinId: number }) {
    try {
      return await this.gameService.handleSkinEquip(client.id, data.skinId);
    } catch (error) {
      client.emit('error', { message: error.message });
      return null;
    }
  }

  @SubscribeMessage('room:get-state')
  async handleGetRoomState(
    @MessageBody() data: { roomId: number },
    @ConnectedSocket() client: Socket,
  ) {
    try {
      const roomState = await this.gameService.getGameState(data.roomId);
      client.emit('room:state', {
        roomId: data.roomId,
        ...roomState,
      });

    } catch (error) {
      console.error('Get room state error:', error);
      client.emit('error', { message: 'Failed to get room state' });
    }
  }

  @SubscribeMessage('room:get-leaderboard')
  async handleGetLeaderboard(
    @MessageBody() data: { roomId: number },
    @ConnectedSocket() client: Socket,
  ) {
    try {
      const leaderboard = await this.gameService.getRoomLeaderboard(data.roomId);
      client.emit('room:leaderboard', {
        roomId: data.roomId,
        leaderboard,
      });

    } catch (error) {
      console.error('Get leaderboard error:', error);
      client.emit('error', { message: 'Failed to get leaderboard' });
    }
  }

  @SubscribeMessage('test:broadcast')
  async handleTestBroadcast(@ConnectedSocket() client: Socket) {
    try {
      const playerData = this.connectedPlayers.get(client.id);
      if (!playerData?.roomId) {
        client.emit('error', { message: 'Player not in a room' });
        return;
      }

      console.log(`Manual broadcast triggered for room ${playerData.roomId}`);
      
      // Get current game state
      const roomState = await this.gameService.getGameState(playerData.roomId);
      
      // Broadcast to all players in the room
      this.server.to(`room_${playerData.roomId}`).emit('game:state', roomState);
      
      console.log(`Manual broadcast sent for room ${playerData.roomId}: ${roomState.players.length} players, ${roomState.food.length} food`);
      
      client.emit('test:broadcast:success', { message: 'Broadcast sent' });
    } catch (error) {
      console.error('Test broadcast error:', error);
      client.emit('error', { message: error.message });
    }
  }

  private async broadcastGameState(roomId: number) {
    const snakes = this.gameService.getSnakesInRoom(roomId);
    const gameState = await this.gameService.buildGameState(roomId, snakes);

    this.server.to(`room_${roomId}`).emit('game:state', gameState);
  }

  // Helper method to check if player is in a room
  private isPlayerInRoom(clientId: string): boolean {
    const playerData = this.connectedPlayers.get(clientId);
    return playerData?.roomId !== undefined;
  }

  // Helper method to get player's current room
  private getPlayerRoom(clientId: string): number | null {
    const playerData = this.connectedPlayers.get(clientId);
    return playerData?.roomId || null;
  }

  // Helper method to force player to leave current room
  private async forcePlayerLeaveRoom(clientId: string): Promise<void> {
    const playerData = this.connectedPlayers.get(clientId);
    if (playerData && playerData.roomId) {
      // Leave the room
      const client = this.server.sockets.sockets.get(clientId);
      if (client) {
        client.leave(`room_${playerData.roomId}`);
      }
      
      // Update game service
      await this.gameService.handlePlayerLeave(clientId);
      
      // Clear room from player data
      delete playerData.roomId;
    }
  }

  async handleTestSpawnBots(data: { roomId: number; count?: number }, client: Socket) {
    try {
      const { roomId, count = 3 } = data;
      console.log(`Test spawning ${count} bots in room ${roomId}`);
      
      const result = await this.gameService.testSpawnBots(roomId, count);
      
      client.emit('test:spawn-bots:response', {
        success: true,
        message: result,
        roomId,
        count
      });
      
      // Broadcast updated game state to all players in the room
      await this.broadcastGameState(roomId);
      
    } catch (error) {
      console.error('Error spawning test bots:', error);
      client.emit('test:spawn-bots:response', {
        success: false,
        error: error.message
      });
    }
  }
} 