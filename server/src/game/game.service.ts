import { Injectable, OnModuleInit, Logger } from '@nestjs/common';
import { PrismaService } from '../prisma/prisma.service';
import { RoomsService } from '../rooms/rooms.service';
import { PlayerService } from './player.service';
import { BotsService } from '../bots/bots.service';
import { PlayerSession, Room, Player } from '@prisma/client';
import { JwtService } from '@nestjs/jwt';

interface GameConfig {
  gridSize: number;
  initialSnakeLength: number;
  foodSpawnInterval: number;
  foodValue: number;
  boostSpeed: number;
  boostDuration: number;
  boostCooldown: number;
  collisionRadius: number;
  maxPlayers: number;
  roomTimeout: number;
  gameLoopInterval: number;
  stateBroadcastInterval: number;
}

interface GameRoom {
  id: number;
  name: string;
  players: Set<string>;
  snakes: Map<string, Snake>;
  foods: Map<number, Food>;
  lastActivity: number;
  gameState: GameState;
}

export interface Snake {
  id: string;
  playerId: number;
  roomId: number;
  position: { x: number; y: number };
  direction: { x: number; y: number };
  length: number;
  speed: number;
  score: number;
  kills: number;
  isBoosting: boolean;
  lastUpdate: number;
  lastInput: { direction: { x: number; y: number }; isBoosting: boolean; timestamp: number };
  segments: { x: number; y: number }[];
  isAlive: boolean;
  deathTime?: number; // Timestamp when the snake died
  spawnTime: number; // Timestamp when the snake was spawned
  foodProgress: number; // Progress towards next segment (0-2, 3 food needed for 1 segment)
  skinId?: number; // Skin ID for the snake (optional)
}

export interface Food {
  id: string;
  roomId: number;
  position: { x: number; y: number };
  value: number;
  type: string;
  spawnTime: number; // Timestamp when the food was spawned
  isFromDeath?: boolean; // Whether this food spawned from snake death
  size?: number; // Size multiplier for the food (1.0 = normal size)
  color?: string; // Random color for the food (hex string)
}

export interface GameState {
  roomId: number;
  timestamp: number;
  players: {
    id: number;
    name: string;
    x: number;
    y: number;
    length: number;
    score: number;
    kills: number;
    isBot: boolean;
    isAlive: boolean;
    segments: { x: number; y: number }[];
    isLocalPlayer: boolean;
    foodProgress: number; // Progress towards next segment (0-2)
    segmentSize: number; // Segment size multiplier based on length
    skinId?: number; // Skin ID for the player
  }[];
  food: {
    id: string;
    x: number;
    y: number;
    type: string;
    value: number;
    size?: number; // Size multiplier for the food
    color?: string; // Random color for the food (hex string)
  }[];
  leaderboard: {
    rank: number;
    name: string;
    score: number;
    kills: number;
    isBot: boolean;
  }[];
}

@Injectable()
export class GameService implements OnModuleInit {
  private snakes: Map<string, Snake> = new Map();
  private foods: Map<string, Food> = new Map();
  private playerSessions: Map<string, number> = new Map();
  private collisionCooldowns: Map<string, number> = new Map(); // Track collision cooldowns
  private readonly GAME_LOOP_INTERVAL = 1000 / 120; // Increased from 60 FPS to 120 FPS for more responsive movement
  private readonly STATE_BROADCAST_INTERVAL = 1000 / 120; // Increased from 60 FPS to 120 FPS for smoother updates
  private readonly FOOD_SPAWN_INTERVAL = 500; // Reduced to 1 second for more food
  private readonly FOOD_VALUE_RANGE = { min: 1, max: 3 };
  private readonly GAME_AREA = { width: 200, height: 200, centerX: 0, centerY: 0 }; // Increased from 100x100 to 200x200
  private readonly rooms: Map<number, GameRoom> = new Map();
  private readonly playerRooms: Map<string, number> = new Map();
  private readonly playerSnakes: Map<string, Snake> = new Map();
  private readonly gameConfig: GameConfig;
  private readonly logger = new Logger(GameService.name);
  private nextRoomId = 1;
  private gameStates: Map<number, GameState> = new Map();
  private playerPositions: Map<number, { x: number; y: number }> = new Map();
  private gameLoopInterval: NodeJS.Timeout;
  private stateBroadcastInterval: NodeJS.Timeout;
  private foodSpawnInterval: NodeJS.Timeout;
  private server: any; // Will be set by the gateway
  private lastUpdateTime = Date.now();
  private gameLoopCounter = 0; // Debug counter
  private botIdCounter = -2; // starts at -2 and decrements for bot player IDs to avoid conflicts with local player
  private botNames = [
    'SnakeMaster', 'CoilKing', 'SerpentPro', 'ViperElite', 'PythonPilot',
    'SlitherChamp', 'ReptileRacer', 'ScaleSpeed', 'FangFlash', 'TailTitan',
    'CobraCruiser', 'AdderAce', 'MambaMover', 'RattlerRush', 'Copperhead',
    'Cottonmouth', 'Bushmaster', 'FerDeLance', 'Taipan', 'BlackMamba'
  ];

  // Bot AI properties for more human-like behavior
  private botMemories: Map<string, {
    lastSeenFood: { x: number; y: number } | null;
    lastSeenFoodTime: number;
    explorationTarget: { x: number; y: number } | null;
    explorationStartTime: number;
    lastDirectionChange: number;
    wanderDirection: { x: number; y: number };
    targetDirection: { x: number; y: number }; // Add target direction for smooth interpolation
    lastUpdateTime: number; // Track last update for interpolation
  }> = new Map();

  private readonly BOT_VISION_RANGE = 15; // How far bots can "see" food
  private readonly BOT_MEMORY_DURATION = 5000; // How long they remember food (5 seconds)
  private readonly BOT_EXPLORATION_DURATION = 3000; // How long they explore in one direction (3 seconds)
  private readonly BOT_DIRECTION_CHANGE_INTERVAL = 2000; // How often they change direction (2 seconds)
  private readonly BOT_ROTATION_SPEED = 0.1; // Smooth rotation speed (lower = smoother)
  private gameStartTime = Date.now(); // Track when the game service started
  private readonly GAME_START_PROTECTION_DURATION = 3000; // 5 seconds of protection after game start

  // Helper method to generate random colors
  private getRandomColor(): string {
    const colors = [
      '#FF6B6B', // Red
      '#4ECDC4', // Teal
      '#45B7D1', // Blue
      '#96CEB4', // Green
      '#FFEAA7', // Yellow
      '#DDA0DD', // Plum
      '#98D8C8', // Mint
      '#F7DC6F', // Gold
      '#BB8FCE', // Purple
      '#85C1E9', // Light Blue
      '#F8C471', // Orange
      '#82E0AA', // Light Green
      '#F1948A', // Pink
      '#85C1E9', // Sky Blue
      '#FAD7A0'  // Peach
    ];
    return colors[Math.floor(Math.random() * colors.length)];
  }

  constructor(
    private prisma: PrismaService,
    private roomsService: RoomsService,
    private playerService: PlayerService,
    private jwtService: JwtService,
    private botsService: BotsService,
  ) {
    this.gameConfig = {
      gridSize: 100,
      initialSnakeLength: 5,
      foodSpawnInterval: 5000,
      foodValue: 1,
      boostSpeed: 2,
      boostDuration: 1000,
      boostCooldown: 5000,
      collisionRadius: 1.0,
      maxPlayers: 10,
      roomTimeout: 300000, // 5 minutes
      gameLoopInterval: 1000 / 120, // 120 FPS
      stateBroadcastInterval: 1000 / 120, // 120 FPS
    };
  }

  // Method to set the server instance from the gateway
  setServer(server: any) {
    this.server = server;
  }

  onModuleInit() {
    this.initializeGameLoop();
    this.initializeStateBroadcast();
    this.initializeFoodSpawner();
  }

  private initializeGameLoop() {
    this.gameLoopInterval = setInterval(async () => {
      await this.updateGameState();
    }, this.GAME_LOOP_INTERVAL);
  }

  private initializeStateBroadcast() {
    this.stateBroadcastInterval = setInterval(() => {
      this.broadcastGameStates();
    }, this.STATE_BROADCAST_INTERVAL);
  }

  private initializeFoodSpawner() {
    this.foodSpawnInterval = setInterval(() => {
      this.spawnFood();
    }, this.FOOD_SPAWN_INTERVAL);
  }

  private async updateGameState() {
    const now = Date.now();
    const deltaTime = (now - this.lastUpdateTime) / 1000; // Convert to seconds
    this.lastUpdateTime = now;
    this.gameLoopCounter++;
    
    // Clean up dead snakes that have been dead for more than 5 seconds
    this.cleanupDeadSnakes(now);
    
    // Clean up excess food to prevent performance issues
    this.cleanupExcessFood();
    
    // Clean up food spawned from snake death after 10 seconds
    this.cleanupDeathFood(now);
    
    // Ensure bot population is maintained in all rooms
    const roomIds = new Set(Array.from(this.snakes.values()).map(snake => snake.roomId));
    for (const roomId of roomIds) {
      await this.ensureBotPopulation(roomId, 18); // Maintain 18 bots per room
    }
    
    // Update bot AI before moving snakes
    this.updateBotAI();
    
    // Log statistics occasionally to monitor performance
    if (this.gameLoopCounter % 1200 == 0) { // Log every 1200 frames (about every 10 seconds)
      const stats = this.getGameStatistics();
      console.log(`[GAME STATS] Total: ${stats.totalSnakes}, Alive: ${stats.aliveSnakes}, Dead: ${stats.deadSnakes}, Bots: ${stats.aliveBots}/${stats.totalBots}, Food: ${stats.totalFood}, Rooms: ${stats.rooms}`);
    }
    
    // Update all snakes
    for (const snake of this.snakes.values()) {
      if (!snake.isAlive) {
        // Skip dead snakes - they should not move
        continue;
      }
      
      const direction = snake.direction;
      
      // Use consistent speed - all snakes move at the same speed regardless of length
      const baseSpeed = 4; // Base speed for all snakes
      const speed = snake.isBoosting ? baseSpeed * 1.5 : baseSpeed;
      
      // Ensure minimum movement by normalizing direction and applying minimum speed
      const directionMagnitude = Math.sqrt(direction.x * direction.x + direction.y * direction.y);
      let normalizedDirection = direction;
      
      if (directionMagnitude > 0.001) {
        // Normalize the direction vector
        normalizedDirection = {
          x: direction.x / directionMagnitude,
          y: direction.y / directionMagnitude
        };
      } else {
        // If direction is too small, use the last valid direction or a default
        if (snake.lastInput && snake.lastInput.direction) {
          const lastMagnitude = Math.sqrt(snake.lastInput.direction.x * snake.lastInput.direction.x + snake.lastInput.direction.y * snake.lastInput.direction.y);
          if (lastMagnitude > 0.001) {
            normalizedDirection = {
              x: snake.lastInput.direction.x / lastMagnitude,
              y: snake.lastInput.direction.y / lastMagnitude
            };
          } else {
            // Fallback to a default direction (up)
            normalizedDirection = { x: 0, y: 1 };
          }
        } else {
          // Fallback to a default direction (up)
          normalizedDirection = { x: 0, y: 1 };
        }
      }
      
      const moveX = normalizedDirection.x * speed * deltaTime;
      const moveY = normalizedDirection.y * speed * deltaTime;
      
      // ALWAYS move the snake - GUARANTEE constant movement
      snake.position.x += moveX;
      snake.position.y += moveY;
      snake.lastUpdate = now;

      // Keep snake within game area
      const halfWidth = this.GAME_AREA.width / 2;
      const halfHeight = this.GAME_AREA.height / 2;

      // Check for wall collision instead of wrapping
      if (snake.position.x <= -halfWidth || snake.position.x >= halfWidth ||
          snake.position.y <= -halfHeight || snake.position.y >= halfHeight) {
        // Snake hit wall - kill it
        this.handleSnakeDeath(snake, 'wall');
        continue; // Skip further processing for this snake
      }

      // Update segments
      this.updateSnakeSegments(snake);
    }

    // Check collisions
    this.checkCollisions();
  }

  private updateSnakeSegments(snake: Snake) {
    // Calculate segment spacing based on snake length - longer snakes have bigger segments
    let segmentDistance = 0.4; // Base spacing for normal snakes
    
    // If snake has more than 10 segments, increase segment spacing progressively
    if (snake.length > 10) {
      const lengthBonus = Math.min((snake.length - 10) * 0.05, 0.3); // Max 30% size increase
      segmentDistance = segmentDistance * (1 + lengthBonus);
    }
    
    if (snake.segments.length === 0) {
      // Add first segment
    snake.segments.unshift({ x: snake.position.x, y: snake.position.y });
    } else {
      // Check distance from last segment
      const lastSegment = snake.segments[0];
      const dx = snake.position.x - lastSegment.x;
      const dy = snake.position.y - lastSegment.y;
      const distance = Math.sqrt(dx * dx + dy * dy);
      
      if (distance >= segmentDistance) {
        snake.segments.unshift({ x: snake.position.x, y: snake.position.y });
      }
    }
    
    // Keep only the segments needed for the snake's length
    while (snake.segments.length > snake.length) {
      snake.segments.pop();
    }
  }

  private checkCollisions() {
    const snakes = Array.from(this.snakes.values());
    const now = Date.now();
    const spawnProtectionTime = 2000; // 2 seconds of spawn protection
    const timeSinceGameStart = now - this.gameStartTime;
    
    // Skip collision detection entirely during game start protection period
    if (timeSinceGameStart < this.GAME_START_PROTECTION_DURATION) {
      return;
    }
    
    // Only check collisions every 10 frames to reduce spam and improve performance
    if (this.gameLoopCounter % 10 !== 0) {
      return;
    }
    
    // Check snake-snake collisions
    for (let i = 0; i < snakes.length; i++) {
      const snake1 = snakes[i];
      if (!snake1.isAlive) continue;
      
      // Skip collision detection for recently spawned snakes
      if (now - snake1.spawnTime < spawnProtectionTime) continue;
      
      for (let j = i + 1; j < snakes.length; j++) {
        const snake2 = snakes[j];
        if (!snake2.isAlive) continue;
        
        // Skip collision detection for recently spawned snakes
        if (now - snake2.spawnTime < spawnProtectionTime) continue;

        if (this.checkSnakeCollision(snake1, snake2)) {
          // Check collision cooldown to prevent repeated detections
          const collisionKey = `${snake1.id}-${snake2.id}`;
          const reverseCollisionKey = `${snake2.id}-${snake1.id}`;
          
          if (this.collisionCooldowns.has(collisionKey) || this.collisionCooldowns.has(reverseCollisionKey)) {
            continue; // Skip if collision is on cooldown
          }
          
          // Set collision cooldown (1 second)
          this.collisionCooldowns.set(collisionKey, now + 1000);
          
          // Only log occasionally to reduce spam
          if (this.gameLoopCounter % 60 === 0) { // Log every 60 frames (about every 0.5 seconds)
          console.log(`Snake collision detected between ${snake1.id} (${snake1.playerId}) and ${snake2.id} (${snake2.playerId})`);
          }
          
          // Determine which snake dies based on collision type
          let victim: Snake;
          let killer: Snake;
          
          // Check if it's a head-to-head collision
          const dx = snake1.position.x - snake2.position.x;
          const dy = snake1.position.y - snake2.position.y;
          const headDistance = Math.sqrt(dx * dx + dy * dy);
          
          if (headDistance < this.gameConfig.collisionRadius) {
            // Head-to-head collision - smaller snake dies
            const snake1Dies = snake1.length <= snake2.length;
            victim = snake1Dies ? snake1 : snake2;
            killer = snake1Dies ? snake2 : snake1;
            console.log(`Head-to-head collision: ${victim.playerId} (length ${victim.length}) dies to ${killer.playerId} (length ${killer.length})`);
          } else {
            // Head-to-body collision - the snake whose head hit the body dies
            // Check which snake's head hit the other's body
            let snake1HitBody = false;
            let snake2HitBody = false;
            
            // Check if snake1's head hit snake2's body
            for (const segment of snake2.segments) {
              const dx = snake1.position.x - segment.x;
              const dy = snake1.position.y - segment.y;
              const distance = Math.sqrt(dx * dx + dy * dy);
              if (distance < this.gameConfig.collisionRadius) {
                snake1HitBody = true;
                break;
              }
            }
            
            // Check if snake2's head hit snake1's body
            for (const segment of snake1.segments) {
              const dx = snake2.position.x - segment.x;
              const dy = snake2.position.y - segment.y;
              const distance = Math.sqrt(dx * dx + dy * dy);
              if (distance < this.gameConfig.collisionRadius) {
                snake2HitBody = true;
                break;
              }
            }
            
            if (snake1HitBody && !snake2HitBody) {
              // Snake1's head hit snake2's body - snake1 dies
              victim = snake1;
              killer = snake2;
              console.log(`Head-to-body collision: ${victim.playerId} hit ${killer.playerId}'s body - ${victim.playerId} dies`);
            } else if (snake2HitBody && !snake1HitBody) {
              // Snake2's head hit snake1's body - snake2 dies
              victim = snake2;
              killer = snake1;
              console.log(`Head-to-body collision: ${victim.playerId} hit ${killer.playerId}'s body - ${victim.playerId} dies`);
            } else {
              // Both hit each other's bodies or unclear - smaller snake dies
              const snake1Dies = snake1.length <= snake2.length;
              victim = snake1Dies ? snake1 : snake2;
              killer = snake1Dies ? snake2 : snake1;
              console.log(`Complex collision: ${victim.playerId} (length ${victim.length}) dies to ${killer.playerId} (length ${killer.length})`);
            }
          }
          
          // Handle the kill and update stats
          this.handleSnakeKill(killer.id, victim.playerId);
          
          // Break out of the inner loop to prevent multiple collision detections
          break;
        }
      }
    }

    // Check snake-food collisions
    for (const snake of snakes) {
      if (!snake.isAlive) continue;
      
      for (const food of this.foods.values()) {
        if (this.checkFoodCollision(snake, food)) {
          //console.log(`Food collision detected for snake ${snake.id} with food ${food.id}`);
          this.handleSnakeEat(snake.id, food.id);
        }
      }
    }

    // Check wall collisions
    for (const snake of snakes) {
      if (!snake.isAlive) continue;
      
      // Wall collision detection enabled - snakes die when hitting walls
      if (this.checkWallCollision(snake)) {
        console.log(`Wall collision detected for snake ${snake.id} at position (${snake.position.x.toFixed(1)}, ${snake.position.y.toFixed(1)})`);
        this.handleSnakeDeath(snake, 'wall');
      }
    }
    
    // Clean up expired collision cooldowns
    for (const [key, expiryTime] of this.collisionCooldowns.entries()) {
      if (now > expiryTime) {
        this.collisionCooldowns.delete(key);
      }
    }
  }

  private checkSnakeCollision(snake1: Snake, snake2: Snake): boolean {
    // Don't check collision if it's the same snake
    if (snake1.id === snake2.id) {
      return false;
    }
    
    // Check head-to-head collision with larger radius
    const dx = snake1.position.x - snake2.position.x;
    const dy = snake1.position.y - snake2.position.y;
    const distance = Math.sqrt(dx * dx + dy * dy);
    
    if (distance < this.gameConfig.collisionRadius * 1.5) { // Increased head collision radius
      return true;
    }

    // Check head-to-body collision with more segments
    for (const segment of snake2.segments) {
      const dx = snake1.position.x - segment.x;
      const dy = snake1.position.y - segment.y;
      const distance = Math.sqrt(dx * dx + dy * dy);
      
      if (distance < this.gameConfig.collisionRadius) {
        return true;
      }
    }

    // Also check snake2's head against snake1's body
    for (const segment of snake1.segments) {
      const dx = snake2.position.x - segment.x;
      const dy = snake2.position.y - segment.y;
      const distance = Math.sqrt(dx * dx + dy * dy);
      
      if (distance < this.gameConfig.collisionRadius) {
        return true;
      }
    }

    return false;
  }

  private checkFoodCollision(snake: Snake, food: Food): boolean {
    const dx = snake.position.x - food.position.x;
    const dy = snake.position.y - food.position.y;
    const distance = Math.sqrt(dx * dx + dy * dy);
    return distance < this.gameConfig.collisionRadius; // Use configurable collision radius
  }

  private checkWallCollision(snake: Snake): boolean {
    const halfWidth = this.GAME_AREA.width / 2;
    const halfHeight = this.GAME_AREA.height / 2;
    
    // Immediate death on wall contact - no buffer zone
    return snake.position.x <= -halfWidth || snake.position.x >= halfWidth ||
           snake.position.y <= -halfHeight || snake.position.y >= halfHeight;
  }

  private spawnFood() {
    const rooms = Array.from(new Set(Array.from(this.snakes.values()).map(snake => snake.roomId)));
    
    for (const roomId of rooms) {
      // Only spawn food if there are active snakes in the room
      const activeSnakes = Array.from(this.snakes.values()).filter(snake => 
        snake.roomId === roomId && snake.isAlive
      );
      
      if (activeSnakes.length === 0) continue;

      // Check if there's already too much food in this room
      const roomFood = Array.from(this.foods.values()).filter(food => food.roomId === roomId);
      if (roomFood.length >= 1000) { // Increased limit to 1000 food items per room (5x more)
        continue;
      }

      // Spawn 5 food items at once (5x more food)
      for (let i = 0; i < 5; i++) {
      const food: Food = {
        id: Math.random().toString(36).substring(7),
        roomId,
        position: {
            x: (Math.random() - 0.5) * this.GAME_AREA.width, // Range from -100 to +100 (properly bounded)
            y: (Math.random() - 0.5) * this.GAME_AREA.height, // Range from -100 to +100 (properly bounded)
        },
        value: Math.floor(Math.random() * (this.FOOD_VALUE_RANGE.max - this.FOOD_VALUE_RANGE.min + 1)) + this.FOOD_VALUE_RANGE.min,
        type: 'regular',
        spawnTime: Date.now(),
        isFromDeath: false,
        size: 0.85, // Normal size for regular food
        color: this.getRandomColor() // Random color for food
      };

      this.foods.set(food.id, food);
      }
    }
  }

  private cleanupExcessFood() {
    // Clean up excess food to prevent performance issues
    const rooms = Array.from(new Set(Array.from(this.snakes.values()).map(snake => snake.roomId)));
    
    for (const roomId of rooms) {
      const roomFood = Array.from(this.foods.values()).filter(food => food.roomId === roomId);
      
      if (roomFood.length > 1200) { // If more than 1200 food items in a room (5x more)
        // Remove oldest food items (keep newest 1000)
        const sortedFood = roomFood.sort((a, b) => {
          // Extract timestamp from food ID for sorting
          const aTime = parseInt(a.id.split('_').pop() || '0');
          const bTime = parseInt(b.id.split('_').pop() || '0');
          return bTime - aTime; // Newest first
        });
        
        // Remove excess food
        const foodToRemove = sortedFood.slice(1000);
        for (const food of foodToRemove) {
          this.foods.delete(food.id);
        }
      }
    }
  }

  private cleanupDeathFood(now: number) {
    // Clean up food spawned from snake death after 10 seconds
    const deathFoodLifetime = 10000; // 10 seconds in milliseconds
    
    for (const [foodId, food] of this.foods.entries()) {
      if (food.isFromDeath && (now - food.spawnTime) > deathFoodLifetime) {
        this.foods.delete(foodId);
        this.logger.log(`Removed death food ${foodId} after ${deathFoodLifetime}ms`);
      }
    }
  }

  private async broadcastGameStates() {
    if (!this.server) {
      // Only log occasionally to reduce performance impact
      if (this.gameLoopCounter % 12000 == 0) { // Log every 12000 frames (about every 100 seconds)
      console.log('WARNING: Server instance not set in game service - cannot broadcast game state');
      }
      return;
    }

    // Group snakes by room - EXCLUDE DEAD SNAKES
    const roomSnakes = new Map<number, Snake[]>();
    for (const snake of this.snakes.values()) {
      // Skip dead snakes to prevent performance issues
      if (!snake.isAlive) {
        continue;
      }
      
      if (!roomSnakes.has(snake.roomId)) {
        roomSnakes.set(snake.roomId, []);
      }
      roomSnakes.get(snake.roomId)!.push(snake);
    }

    // Broadcast game state for each room
    for (const [roomId, snakes] of roomSnakes) {
      const gameState = await this.buildGameState(roomId, snakes);
      this.server.to(`room_${roomId}`).emit('game:state', gameState);
    }
  }

  public async buildGameState(roomId: number, snakes: Snake[]): Promise<GameState> {
    const now = Date.now();
    const timeSinceGameStart = now - this.gameStartTime;
    
    // Get all foods in the room
    const roomFoods = this.getFoodsInRoom(roomId);
    
    // Build players array with local player identification
    const players = await Promise.all(snakes.map(async snake => {
      // Determine if this snake belongs to the current client
      const isLocalPlayer = snake.playerId > 0; // Only real players (not bots) can be local
      
      // Calculate segment size multiplier based on snake length
      let segmentSize = 1.0; // Base size for normal snakes
      if (snake.length > 10) {
        const lengthBonus = Math.min((snake.length - 10) * 0.05, 0.3); // Max 30% size increase
        segmentSize = 1.0 + lengthBonus;
      }
      
      // Get player name - use database for real players, bot names for bots
      let playerName: string;
      if (snake.playerId > 0) {
        // Real player - get name from database
        try {
          const player = await this.prisma.player.findUnique({
            where: { id: snake.playerId }
          });
          playerName = player?.username || 'Unknown Player';
        } catch (error) {
          console.error(`Error fetching player name for ID ${snake.playerId}:`, error);
          playerName = 'Unknown Player';
        }
      } else {
        // Bot - use bot name
        playerName = this.getBotName(snake.playerId);
      }
      
      // During game start protection period, force all snakes to be alive
      const isAlive = timeSinceGameStart < this.GAME_START_PROTECTION_DURATION ? true : snake.isAlive;
      
      return {
        id: snake.playerId,
        name: playerName,
        x: snake.position.x,
        y: snake.position.y,
        length: snake.length,
        score: snake.score,
        kills: snake.kills,
        isBot: snake.playerId < 0,
        isAlive: isAlive,
        segments: snake.segments,
        isLocalPlayer: isLocalPlayer, // Add local player flag
        foodProgress: snake.foodProgress, // Add food progress
        segmentSize: segmentSize, // Add segment size multiplier
        skinId: snake.skinId // Add skin ID
      };
    }));
    
    // Build food array
    const food = roomFoods.map(food => ({
      id: food.id,
      x: food.position.x,
      y: food.position.y,
      type: food.type,
      value: food.value,
      size: food.size,
      color: food.color
    }));
    
    // Build leaderboard
    const leaderboard = players
      .filter(player => player.isAlive)
      .sort((a, b) => b.score - a.score)
      .map((player, index) => ({
        rank: index + 1,
        name: player.name,
        score: player.score,
        kills: player.kills,
        isBot: player.isBot
      }));
    
    return {
      roomId,
      timestamp: now,
      players,
      food,
      leaderboard
    };
  }

  // Method to handle player input (direction and boost)
  async handlePlayerInput(socketId: string, direction: { x: number; y: number }, isBoosting: boolean) {
    const snake = this.snakes.get(socketId);
    if (!snake || !snake.isAlive) return;

    const len = Math.hypot(direction.x, direction.y);
    
    // Only update direction if input is meaningful (not zero)
    // This creates a dead zone when cursor is very close to snake head
    if (len > 0.1) {
      const targetDirection = { x: direction.x / len, y: direction.y / len };
      
      // Smooth rotation interpolation
      const rotationSpeed = 0.1; // Reduced from 0.2 to 0.1 for more gradual turning
      
      // Interpolate between current direction and target direction
      // This prevents instant 180-degree turns and creates smooth rotation
      snake.direction.x = snake.direction.x + (targetDirection.x - snake.direction.x) * rotationSpeed;
      snake.direction.y = snake.direction.y + (targetDirection.y - snake.direction.y) * rotationSpeed;
      
      // Normalize the interpolated direction to maintain unit length
      const newLen = Math.hypot(snake.direction.x, snake.direction.y);
      if (newLen > 0) {
        snake.direction.x /= newLen;
        snake.direction.y /= newLen;
      }
    }
    // If input is zero or very small, keep current direction (snake won't change direction)
    
    snake.isBoosting = isBoosting;
    snake.lastInput = { direction: snake.direction, isBoosting, timestamp: Date.now() };
    snake.lastUpdate = Date.now();
  }

  // Method to get snake by socket ID
  getSnakeBySocketId(socketId: string): Snake | undefined {
    return this.snakes.get(socketId);
  }

  async handlePlayerJoin(socketId: string, roomId: number, playerId: number) {
    const room = await this.roomsService.findOne(roomId);
    if (!room) {
      throw new Error('Room not found');
    }

    const session = await this.prisma.playerSession.create({
      data: {
        playerId,
        roomId,
        score: 0,
        kills: 0,
        topPosition: 0,
        length: 1,
      },
      include: {
        player: true,
      },
    });

    this.playerSessions.set(socketId, session.id);

    // Add player to room
    const roomWithFields = room as any;
    roomWithFields.currentPlayers = roomWithFields.currentPlayers || 0;
    roomWithFields.currentPlayers++;

    // Ensure bot population is maintained
    await this.ensureBotPopulation(roomId, 18);

    this.logger.log(`Player ${playerId} joined room ${roomId} with socket ${socketId}`);

    return {
      room,
      session,
    };
  }

  async handlePlayerDisconnect(socketId: string) {
    // Only log occasionally to reduce performance impact
    if (this.gameLoopCounter % 6000 == 0) { // Log every 6000 frames (about every 50 seconds)
    console.log(`Player disconnected: ${socketId}`);
    }
    
    const snake = this.snakes.get(socketId);
    if (snake) {
      // Only log occasionally to reduce performance impact
      if (this.gameLoopCounter % 6000 == 0) { // Log every 6000 frames (about every 50 seconds)
      console.log(`Handling disconnect for snake ${snake.id} (player ${snake.playerId})`);
      }
      
      // Mark snake as dead and handle death
      this.handleSnakeDeath(snake, 'disconnect');
      
      // Remove from collections
      this.snakes.delete(socketId);
      this.playerSnakes.delete(socketId);
      
      // Remove from room
      const room = this.rooms.get(snake.roomId);
      if (room) {
        room.players.delete(socketId);
        room.snakes.delete(socketId);
        
        // If room is empty, clean it up
        if (room.players.size === 0) {
          this.rooms.delete(snake.roomId);
          // Only log occasionally to reduce performance impact
          if (this.gameLoopCounter % 6000 == 0) { // Log every 6000 frames (about every 50 seconds)
          console.log(`Room ${snake.roomId} deleted due to being empty`);
          }
        }
      }
      
      // Remove from player rooms
      this.playerRooms.delete(socketId);
      
      // Clean up session
      const sessionId = this.playerSessions.get(socketId);
      if (sessionId) {
        try {
          await this.prisma.playerSession.update({
            where: { id: sessionId },
            data: { isAlive: false },
          });
          this.playerSessions.delete(socketId);
        } catch (error) {
          console.error('Error updating session on disconnect:', error);
        }
      }
      
      // Only log occasionally to reduce performance impact
      if (this.gameLoopCounter % 6000 == 0) { // Log every 6000 frames (about every 50 seconds)
      console.log(`Disconnect handling completed for snake ${snake.id}`);
      }
    }
  }
  
  public async spawnFoodAtSnakeDeath(snake: Snake) {
    const now = Date.now();
    
    // Helper function to clamp position within game boundaries
    const clampPosition = (pos: { x: number; y: number }) => {
      const halfWidth = this.GAME_AREA.width / 2;
      const halfHeight = this.GAME_AREA.height / 2;
      return {
        x: Math.max(-halfWidth + 5, Math.min(halfWidth - 5, pos.x)), // Keep 5 units away from walls
        y: Math.max(-halfHeight + 5, Math.min(halfHeight - 5, pos.y))
      };
    };
    
    // Spawn food at the snake's head position (higher value)
    const headPosition = clampPosition(snake.position);
    const headFood: Food = {
      id: `food_death_head_${snake.id}_${now}`,
      roomId: snake.roomId,
      position: headPosition,
      value: 5, // Fixed high value for head food from killed snakes
      type: 'regular',
      spawnTime: now,
      isFromDeath: true,
      size: 0.95, // 50% bigger for death food
      color: this.getRandomColor() // Random color for food
    };
    this.foods.set(headFood.id, headFood);
    
    // Spawn food every 3rd segment instead of every segment
    const segmentStep = 3; // Spawn food every 3rd segment
    
    for (let i = 0; i < snake.segments.length; i += segmentStep) {
      const segment = snake.segments[i];
      const segmentPosition = clampPosition(segment);
      const segmentFood: Food = {
        id: `food_death_segment_${snake.id}_${i}_${now}`,
        roomId: snake.roomId,
        position: segmentPosition,
        value: 5, // Fixed high value for segment food from killed snakes
        type: 'regular',
        spawnTime: now,
        isFromDeath: true,
        size: 0.75,
        color: this.getRandomColor() // Random color for food
      };
      this.foods.set(segmentFood.id, segmentFood);
    }
  }

  async handlePlayerLeave(socketId: string) {
    console.log(`Player leaving: ${socketId}`);
    
    const snake = this.snakes.get(socketId);
    if (snake) {
      console.log(`Handling leave for snake ${snake.id} (player ${snake.playerId})`);
      
      // Mark snake as dead and handle death
      this.handleSnakeDeath(snake, 'disconnect');
      
      // Remove from collections
      this.snakes.delete(socketId);
      this.playerSnakes.delete(socketId);
      
      // Remove from room
      const room = this.rooms.get(snake.roomId);
      if (room) {
        room.players.delete(socketId);
        room.snakes.delete(socketId);
      }
      
      // Remove from player rooms
      this.playerRooms.delete(socketId);
      
      // Clean up session
      const sessionId = this.playerSessions.get(socketId);
      if (sessionId) {
        try {
          await this.prisma.playerSession.update({
            where: { id: sessionId },
            data: { isAlive: false },
          });
          this.playerSessions.delete(socketId);
        } catch (error) {
          console.error('Error updating session on leave:', error);
        }
      }
      
      console.log(`Leave handling completed for snake ${snake.id}`);
    }
  }

  async spawnSnake(socketId: string, roomId: number, playerId: number): Promise<Snake> {
    // Find a safe spawn position that doesn't collide with existing snakes
    const spawnPosition = this.findSafeSpawnPosition(roomId);

    const snake: Snake = {
      id: socketId,
      playerId,
      roomId,
      position: spawnPosition,
      direction: { x: 1, y: 0 }, // GUARANTEED valid direction - snake will NEVER stop
      length: 5, // Initial length of 5
      speed: 8, // Default speed (not used - calculated in game loop)
      score: 0,
      kills: 0,
      isBoosting: false,
      lastUpdate: Date.now(),
      lastInput: { direction: { x: 1, y: 0 }, isBoosting: false, timestamp: Date.now() },
      segments: [],
      isAlive: true,
      spawnTime: Date.now(),
      foodProgress: 0, // Start with 0 food progress
    };

    // Add snake to room
    const room = this.rooms.get(roomId);
    if (room) {
      room.snakes.set(socketId, snake);
    }

    // Add snake to main snakes collection so it can be found by getSnakesInRoom
    this.snakes.set(socketId, snake);
    this.playerSnakes.set(socketId, snake);
    this.playerRooms.set(socketId, roomId);

    console.log(`[SNAKE_SPAWN] Real player snake ${playerId} added to main collection. Total snakes: ${this.snakes.size}`);

    // Ensure bot population is maintained
    await this.ensureBotPopulation(roomId, 18);

    this.logger.log(`Snake spawned for player ${playerId} in room ${roomId}`);
    return snake;
  }

  private findSafeSpawnPosition(roomId: number): { x: number; y: number } {
    const centerX = this.GAME_AREA.centerX;
    const centerY = this.GAME_AREA.centerY;
    const maxSpawnRadius = 80; // Increased spawn radius to find more space
    const minSpawnRadius = 20; // Minimum distance from center
    const collisionRadius = this.gameConfig.collisionRadius * 2; // Safe distance from other snakes
    const wallBuffer = 15; // Safe distance from walls to prevent immediate death
    
    // Get all existing snakes in the room
    const existingSnakes = Array.from(this.snakes.values()).filter(snake => 
      snake.roomId === roomId && snake.isAlive
    );
    
    // Try to find a safe position
    for (let attempt = 0; attempt < 50; attempt++) {
      const angle = Math.random() * 2 * Math.PI;
      const distance = minSpawnRadius + Math.random() * (maxSpawnRadius - minSpawnRadius);
      
      const candidateX = centerX + Math.cos(angle) * distance;
      const candidateY = centerY + Math.sin(angle) * distance;
      
      // Check if this position is safe (not too close to any existing snake)
      let isSafe = true;
      for (const existingSnake of existingSnakes) {
        const dx = candidateX - existingSnake.position.x;
        const dy = candidateY - existingSnake.position.y;
        const distanceToSnake = Math.sqrt(dx * dx + dy * dy);
        
        if (distanceToSnake < collisionRadius) {
          isSafe = false;
          break;
        }
      }
      
      // Also check if position is too close to walls
      const halfWidth = this.GAME_AREA.width / 2;
      const halfHeight = this.GAME_AREA.height / 2;
      if (Math.abs(candidateX) >= halfWidth - wallBuffer || Math.abs(candidateY) >= halfHeight - wallBuffer) {
        isSafe = false;
      }
      
      if (isSafe) {
        return { x: candidateX, y: candidateY };
      }
    }
    
    // If we can't find a safe position, spawn in the center with a small random offset
    const centerOffset = 10; // Small random offset from center
    return {
      x: centerX + (Math.random() - 0.5) * centerOffset,
      y: centerY + (Math.random() - 0.5) * centerOffset
    };
  }

  async updateSnakeDirection(socketId: string, direction: { x: number; y: number }) {
    const snake = this.snakes.get(socketId);
    if (snake) {
      // ALWAYS normalize the direction to length 1
      const directionMagnitude = Math.sqrt(direction.x * direction.x + direction.y * direction.y);
      if (directionMagnitude > 0) {
        snake.direction = {
          x: direction.x / directionMagnitude,
          y: direction.y / directionMagnitude
        };
      }
      snake.lastUpdate = Date.now();
    }
    return snake;
  }

  async updateSnakePosition(socketId: string, position: { x: number; y: number }, rotation: number, isBoosting: boolean) {
    // Only allow client position updates if explicitly enabled (for debugging)
    if (process.env.ALLOW_CLIENT_POS !== 'true') {
      console.log(`Client position update blocked for ${socketId} - server is authoritative`);
      return;
    }

    const snake = this.snakes.get(socketId);
    if (snake) {
      snake.position = position;
      snake.isBoosting = isBoosting;
      snake.lastUpdate = Date.now();
      
      // Convert rotation to direction and normalize to length 1
      const angle = (rotation * Math.PI) / 180;
      const dirX = Math.cos(angle);
      const dirY = Math.sin(angle);
      const directionMagnitude = Math.sqrt(dirX * dirX + dirY * dirY);
      
      if (directionMagnitude > 0) {
      snake.direction = {
          x: dirX / directionMagnitude,
          y: dirY / directionMagnitude
      };
      }
    }
    return snake;
  }

  async updateSnakeBoost(socketId: string, isBoosting: boolean) {
    const snake = this.snakes.get(socketId);
    if (snake) {
      snake.isBoosting = isBoosting;
      snake.lastUpdate = Date.now();
    }
    return snake;
  }

  public handleSnakeDeath(snake: Snake, reason: 'wall' | 'collision' | 'disconnect') {
    const now = Date.now();
    const timeSinceGameStart = now - this.gameStartTime;
    
    // Ignore deaths during the game start protection period (except for disconnects)
    if (timeSinceGameStart < this.GAME_START_PROTECTION_DURATION && reason !== 'disconnect') {
      console.log(`Ignoring ${reason} death for snake ${snake.id} during game start protection period`);
      return;
    }
    
    const isBot = snake.playerId < 0;
    
    snake.isAlive = false;
    snake.deathTime = Date.now();
    
    // Clean up bot memory if it's a bot
    if (isBot) {
      this.botMemories.delete(snake.id);
    }
    
    // Spawn food at the snake's death location (head and limited segments)
    this.spawnFoodAtSnakeDeath(snake);
    
    // Broadcast updated game state
    this.broadcastGameStates();
  }

  async handleSnakeEat(socketId: string, foodId: string) {
    const snake = this.snakes.get(socketId);
    if (!snake || !snake.isAlive) return;

    const food = this.foods.get(foodId);
    if (!food || food.roomId !== snake.roomId) return;

    // Remove food
    this.foods.delete(foodId);

    // Update snake stats
    snake.score += food.value;

    // Update food progress (0-2, 3 food needed for 1 segment)
    snake.foodProgress += 1;
    
    // Check if we have enough food to grow a segment
    if (snake.foodProgress >= 3) {
      snake.length += 1;
      snake.foodProgress = 0; // Reset progress
      
      // Only log occasionally to reduce performance impact
      if (this.gameLoopCounter % 300 == 0) { // Log every 300 frames (about every 2.5 seconds)
    if (snake.playerId < 0) {
      this.logger.log(`Bot ${snake.id} ate food, new length: ${snake.length}, score: ${snake.score}`);
        } else {
    this.logger.log(`Snake ${snake.id} ate food ${foodId}, new length: ${snake.length}, score: ${snake.score}`);
        }
      }
    } else {
      // Only log occasionally to reduce performance impact
      if (this.gameLoopCounter % 300 == 0) { // Log every 300 frames (about every 2.5 seconds)
        if (snake.playerId < 0) {
          this.logger.log(`Bot ${snake.id} ate food, progress: ${snake.foodProgress}/3, score: ${snake.score}`);
        } else {
          this.logger.log(`Snake ${snake.id} ate food ${foodId}, progress: ${snake.foodProgress}/3, score: ${snake.score}`);
        }
      }
    }
  }

  public handleSnakeKill(killerSocketId: string, victimPlayerId: number) {
    const killer = this.snakes.get(killerSocketId);
    const victim = Array.from(this.snakes.values()).find(snake => snake.playerId === victimPlayerId);
    
    if (killer && victim && killer.roomId === victim.roomId) {
      console.log(`Snake ${killer.id} (player ${killer.playerId}) killed snake ${victim.id} (player ${victim.playerId})`);
      
      // Update killer stats
      killer.score += victim.score;
      killer.kills += 1;
      
      // Mark the victim as dead
      this.handleSnakeDeath(victim, 'collision');
      
      // Broadcast updated game state
      this.broadcastGameStates();
      
      return { killer, victim };
    }
    
    return null;
  }

  async handleSkinEquip(socketId: string, skinId: number) {
    const sessionId = this.playerSessions.get(socketId);
    if (!sessionId) {
      throw new Error('Player session not found');
    }

    const session = await this.prisma.playerSession.findUnique({
      where: { id: sessionId },
      include: { player: true },
    });

    if (!session) {
      throw new Error('Session not found');
    }

    const playerSkin = await this.prisma.playerSkin.findFirst({
      where: {
        playerId: session.playerId,
        skinId,
      },
    });

    if (!playerSkin) {
      throw new Error('Skin not owned by player');
    }

    await this.prisma.playerSkin.update({
      where: {
        id: playerSkin.id,
      },
      data: {
        isEquipped: true,
      },
    });

    return { success: true };
  }

  getSnakesInRoom(roomId: number): Snake[] {
    const snakes = Array.from(this.snakes.values()).filter(snake => 
      snake.roomId === roomId && snake.isAlive // Only include alive snakes
    );
    // Only log occasionally to reduce performance impact
    if (this.gameLoopCounter % 600 == 0) { // Log every 600 frames (about every 5 seconds)
      console.log(`[GET_SNAKES] Room ${roomId}: Found ${snakes.length} alive snakes`);
    }
    return snakes;
  }

  getAllSnakes(): Snake[] {
    return Array.from(this.snakes.values());
  }

  getAllSnakesInRoom(roomId: number): Snake[] {
    return Array.from(this.snakes.values()).filter(snake => snake.roomId === roomId);
  }

  getDeadSnakesInRoom(roomId: number): Snake[] {
    return Array.from(this.snakes.values()).filter(snake => 
      snake.roomId === roomId && !snake.isAlive
    );
  }

  async getServers() {
    // For now, return a single server
    return [{
      id: "1",
      name: "Main Server",
      status: "online",
      playerCount: this.getTotalPlayers()
    }];
  }

  async getRooms() {
    // Get rooms from database instead of in-memory Map
    const dbRooms = await this.roomsService.findAll();
    return dbRooms.map(room => ({
      id: room.id,
      name: room.name,
      players: room.currentPlayers,
      maxPlayers: room.maxPlayers
    }));
  }

  async createRoom(name: string) {
    // Create room in database using RoomsService
    // We need a server ID, so let's get the first available server or create one
    const servers = await this.prisma.server.findMany();
    let serverId: number;
    
    if (servers.length > 0) {
      serverId = servers[0].id;
    } else {
      // Create a default server if none exists
      const defaultServer = await this.prisma.server.create({
        data: {
          region: 'USEast',
          capacity: 5000,
          currentPlayers: 0,
        },
      });
      serverId = defaultServer.id;
    }
    
    // Create room in database
    const room = await this.roomsService.createRoom(name, serverId);
    
    // Use type assertion to access the fields that should exist
    const roomWithFields = room as any;
    
    return {
      id: room.id,
      name: room.name,
      players: roomWithFields.currentPlayers || 0,
      maxPlayers: roomWithFields.maxPlayers || 10
    };
  }

  private getTotalPlayers(): number {
    return this.playerRooms.size;
  }

  async createPlayerSession(playerId: number, roomId: number): Promise<PlayerSession> {
    // Check if player already has an active session in this room
    const existingSession = await this.prisma.playerSession.findFirst({
      where: {
        playerId,
        roomId,
        isAlive: true,
      },
    });

    if (existingSession) {
      // Reset the existing session score to 0 for a fresh start
      const resetSession = await this.prisma.playerSession.update({
        where: { id: existingSession.id },
        data: {
          score: 0,
          kills: 0,
          length: 1,
          isAlive: true,
          updatedAt: new Date(),
        },
        include: {
          player: true,
        },
      });
      
      return resetSession;
    }

    // Create new session
    const session = await this.prisma.playerSession.create({
      data: {
        playerId,
        roomId,
        score: 0,
        kills: 0,
        length: 1,
        isAlive: true,
      },
      include: {
        player: true,
      },
    });

    // Update room player count
    const room = await this.roomsService.findOne(roomId);
    if (room) {
      await this.roomsService.updateRoomPlayerCount(roomId, room.currentPlayers + 1);
    }

    // Initialize player position
    this.playerPositions.set(session.id, {
      x: Math.random() * 800,
      y: Math.random() * 600,
    });

    return session;
  }

  async getPlayerSession(sessionId: number): Promise<PlayerSession | null> {
    return this.prisma.playerSession.findUnique({
      where: { id: sessionId },
      include: {
        player: true,
      },
    });
  }

  async updatePlayerPosition(sessionId: number, x: number, y: number): Promise<void> {
    this.playerPositions.set(sessionId, { x, y });
  }

  async getPlayerPosition(sessionId: number): Promise<{ x: number; y: number } | null> {
    return this.playerPositions.get(sessionId) || null;
  }

  async eatFood(sessionId: number, foodId: number): Promise<PlayerSession> {
    const session = await this.prisma.playerSession.findUnique({
      where: { id: sessionId },
      include: {
        player: true,
      },
    });

    if (!session) {
      throw new Error('Session not found');
    }

    // Calculate new score and length
    const scoreIncrease = 10;
    const newScore = session.score + scoreIncrease;
    const newLength = session.length + 1;

    // Update session
    const updatedSession = await this.prisma.playerSession.update({
      where: { id: sessionId },
      data: {
        score: newScore,
        length: newLength,
        updatedAt: new Date(),
      },
      include: {
        player: true,
      },
    });

    // Update player's best score if needed
    if (newScore > session.player.bestScore) {
      await this.prisma.player.update({
        where: { id: session.playerId },
        data: { bestScore: newScore },
      });
    }

    return updatedSession;
  }

  async killSnake(killerSessionId: number, victimSessionId: number): Promise<void> {
    const [killerSession, victimSession] = await Promise.all([
      this.prisma.playerSession.findUnique({
        where: { id: killerSessionId },
        include: { player: true },
      }),
      this.prisma.playerSession.findUnique({
        where: { id: victimSessionId },
        include: { player: true },
      }),
    ]);

    if (!killerSession || !victimSession) {
      throw new Error('Session not found');
    }

    // Update killer stats
    const killScore = 50;
    await this.prisma.playerSession.update({
      where: { id: killerSessionId },
      data: {
        kills: killerSession.kills + 1,
        score: killerSession.score + killScore,
        updatedAt: new Date(),
      },
    });

    // Update killer's total kills
    await this.prisma.player.update({
      where: { id: killerSession.playerId },
      data: { totalKills: killerSession.player.totalKills + 1 },
    });

    // Mark victim as dead
    await this.prisma.playerSession.update({
      where: { id: victimSessionId },
      data: {
        isAlive: false,
        updatedAt: new Date(),
      },
    });

    // Update victim's stats
    await this.prisma.player.update({
      where: { id: victimSession.playerId },
      data: {
        totalGames: victimSession.player.totalGames + 1,
      },
    });
  }

  async getGameState(roomId: number): Promise<GameState> {
    const room = await this.roomsService.findOne(roomId);
    if (!room) {
      throw new Error('Room not found');
    }

    // Get all active players
    const sessions = await this.roomsService.getRoomSessions(roomId);
    const bots = await this.roomsService.getRoomBots(roomId);

    // Build player list
    const players = [
      ...sessions.map((session) => {
        const position = this.playerPositions.get(session.id) || { x: 0, y: 0 };
        
        // Calculate segment size multiplier based on snake length
        let segmentSize = 1.0; // Base size for normal snakes
        if (session.length > 10) {
          const lengthBonus = Math.min((session.length - 10) * 0.05, 0.3); // Max 30% size increase
          segmentSize = 1.0 + lengthBonus;
        }
        
        return {
          id: session.id,
          name: session.player.username,
          x: position.x,
          y: position.y,
          length: session.length,
          score: session.score,
          kills: session.kills,
          isBot: false,
          isAlive: session.isAlive,
          segments: [],
          isLocalPlayer: session.id > 0, // Real players can be local
          foodProgress: 0, // Default food progress for database sessions
          segmentSize: segmentSize // Add segment size multiplier
        };
      }),
      ...bots.map((bot) => {
        // Calculate segment size multiplier based on snake length
        let segmentSize = 1.0; // Base size for normal snakes
        if (bot.length > 10) {
          const lengthBonus = Math.min((bot.length - 10) * 0.05, 0.3); // Max 30% size increase
          segmentSize = 1.0 + lengthBonus;
        }
        
        return {
        id: bot.id,
        name: bot.name,
        x: Math.random() * 800, // Random position for bots
        y: Math.random() * 600,
        length: bot.length,
        score: bot.score,
        kills: bot.kills,
        isBot: true,
        isAlive: bot.isAlive,
        segments: [],
          isLocalPlayer: false, // Bots are never local players
          foodProgress: 0, // Default food progress for bots
          segmentSize: segmentSize // Add segment size multiplier
        };
      }),
    ];

    // Get leaderboard
    const leaderboard = await this.roomsService.getRoomLeaderboard(roomId);

    // Generate food (simplified - you might want to store this in database)
    const food = Array.from({ length: 20 }, (_, i) => ({
      id: (i + 1).toString(),
      x: Math.random() * 800,
      y: Math.random() * 600,
      type: 'regular',
      value: Math.floor(Math.random() * 3) + 1, // Random value between 1-3
    }));

    const gameState: GameState = {
      roomId,
      timestamp: Date.now(),
      players,
      food,
      leaderboard,
    };

    this.gameStates.set(roomId, gameState);
    return gameState;
  }

  async leaveRoom(sessionId: number): Promise<void> {
    const session = await this.prisma.playerSession.findUnique({
      where: { id: sessionId },
      include: { room: true },
    });

    if (!session) {
      return;
    }

    // Mark session as inactive
    await this.prisma.playerSession.update({
      where: { id: sessionId },
      data: { isAlive: false },
    });

    // Update room player count
    const room = await this.roomsService.findOne(session.roomId);
    if (room) {
      await this.roomsService.updateRoomPlayerCount(session.roomId, Math.max(0, room.currentPlayers - 1));
    }

    // Remove player position
    this.playerPositions.delete(sessionId);
  }

  async getRoomLeaderboard(roomId: number) {
    return this.roomsService.getRoomLeaderboard(roomId);
  }

  async getRoomPlayers(roomId: number) {
    const sessions = await this.roomsService.getRoomSessions(roomId);
    const bots = await this.roomsService.getRoomBots(roomId);

    return {
      players: sessions.map((session) => ({
        id: session.playerId,
        name: session.player.username,
        score: session.score,
        kills: session.kills,
        length: session.length,
        isBot: false,
      })),
      bots: bots.map((bot) => ({
        id: bot.id,
        name: bot.name,
        score: bot.score,
        kills: bot.kills,
        length: bot.length,
        isBot: true,
      })),
      totalPlayers: sessions.length + bots.length,
    };
  }

  /**
   * Returns the first available server's id, or creates a default one if none exist.
   */
  public async getOrCreateServerId(): Promise<number> {
    const servers = await this.prisma.server.findMany({ where: { isActive: true } });
    if (servers.length > 0) {
      return servers[0].id;
    } else {
      const defaultServer = await this.prisma.server.create({
        data: {
          region: 'USEast',
          capacity: 5000,
          currentPlayers: 0,
          isActive: true,
        },
      });
      return defaultServer.id;
    }
  }

  private cleanupDeadSnakes(now: number) {
    // Only run cleanup if there are snakes in the game
    if (this.snakes.size === 0) {
      return;
    }
    
    // Don't run cleanup too early in the game session to avoid false positives
    const gameSessionDuration = now - this.lastUpdateTime;
    if (gameSessionDuration < 5000) { // Reduced from 10 seconds to 5 seconds
      return;
    }
    
    // Clean up dead snakes more aggressively
    const deadSnakes = Array.from(this.snakes.values()).filter(snake => 
      !snake.isAlive && 
      snake.deathTime && 
      (now - snake.deathTime > 3000) // Reduced from 5 seconds to 3 seconds for faster cleanup
    );
    
    for (const snake of deadSnakes) {
      // For bots, clean up immediately if they've been dead for more than 2 seconds
      const isBot = snake.playerId < 0;
      const cleanupDelay = isBot ? 2000 : 3000; // Bots get cleaned up faster
      
      if (now - snake.deathTime > cleanupDelay) {
        console.log(`Removing dead ${isBot ? 'bot' : 'snake'} ${snake.id} after cleanup delay`);
        this.snakes.delete(snake.id);
        
        // Also clean up bot memory immediately for bots
        if (isBot) {
          this.botMemories.delete(snake.id);
        }
      }
    }
  }

  private async createBotSnake(roomId: number): Promise<Snake> {
    const botName = this.getRandomBotName();
    const id = `bot:${Math.abs(this.botIdCounter)}`;
    
    // Get random skin for bot
    const randomSkin = await this.getRandomSkin();
    
    const snake: Snake = {
      id,
      playerId: this.botIdCounter--, // negative = bot
      roomId,
      position: { x: 0, y: 0 },
      direction: { x: 1, y: 0 },
      speed: 4,
      length: this.gameConfig.initialSnakeLength,
      score: 0,
      kills: 0,
      isBoosting: false,
      isAlive: true,
      lastUpdate: Date.now(),
      lastInput: { direction: { x: 1, y: 0 }, isBoosting: false, timestamp: Date.now() },
      segments: [],
      spawnTime: Date.now(),
      foodProgress: 0, // Start with 0 food progress
      skinId: randomSkin?.id || 1, // Assign random skin or default to skin 1
    };
    this.snakes.set(id, snake);
    this.logger.log(`Created bot snake ${botName} (ID: ${id}) in room ${roomId} with skin: ${randomSkin?.name || 'default'}`);
    return snake;
  }

  private async getRandomSkin() {
    try {
      // Get all available skins
      const skins = await this.prisma.skin.findMany();
      if (skins.length === 0) {
        return null;
      }
      
      // Return a random skin
      const randomIndex = Math.floor(Math.random() * skins.length);
      return skins[randomIndex];
    } catch (error) {
      this.logger.error('Error getting random skin for bot:', error);
      return null;
    }
  }

  private getRandomBotName(): string {
    const randomIndex = Math.floor(Math.random() * this.botNames.length);
    return this.botNames[randomIndex];
  }

  private getBotName(playerId: number): string {
    // Use the absolute value of the negative player ID to get a consistent name
    const index = Math.abs(playerId) % this.botNames.length;
    return this.botNames[index];
  }

  private async ensureBotPopulation(roomId: number, target = 7) {
    const aliveBots = Array.from(this.snakes.values())
                         .filter(s => s.roomId === roomId && s.playerId < 0 && s.isAlive);
    
    // Find dead bots that can be reused (only after 5 second respawn delay)
    const now = Date.now();
    const deadBots = Array.from(this.snakes.values())
                         .filter(s => s.roomId === roomId && s.playerId < 0 && !s.isAlive && 
                                    s.deathTime && (now - s.deathTime) > 5000); // 5 second respawn delay
    
    const missing = target - aliveBots.length;
    if (missing <= 0) return;

    // First, try to reuse dead bot IDs
    const botsToRespawn = Math.min(missing, deadBots.length);
    for (let i = 0; i < botsToRespawn; i++) {
      const deadBot = deadBots[i];
      // Reuse the dead bot by respawning it
      deadBot.isAlive = true;
      deadBot.deathTime = undefined;
      deadBot.score = 0;
      deadBot.kills = 0;
      deadBot.length = this.gameConfig.initialSnakeLength;
      deadBot.position = this.findSafeSpawnPosition(roomId);
      deadBot.segments = [];
      deadBot.foodProgress = 0;
      deadBot.spawnTime = Date.now();
      
      // Randomize initial direction
      const angle = Math.random() * 2 * Math.PI;
      deadBot.direction = {
        x: Math.cos(angle),
        y: Math.sin(angle),
      };
      deadBot.lastInput.direction = { ...deadBot.direction };
      
      // Clean up bot memory for the reused bot
      this.botMemories.delete(deadBot.id);
      
      console.log(`Reused dead bot ${deadBot.id} (player ${deadBot.playerId}) for room ${roomId} after 5s delay`);
    }

    // If we still need more bots, create new ones
    const remainingNeeded = missing - botsToRespawn;
    for (let i = 0; i < remainingNeeded; i++) {
      const bot = await this.createBotSnake(roomId);
      // Use safe spawn position for bots too
      bot.position = this.findSafeSpawnPosition(roomId);
      // Randomize initial direction
      const angle = Math.random() * 2 * Math.PI;
      bot.direction = {
        x: Math.cos(angle),
        y: Math.sin(angle),
      };
      bot.lastInput.direction = { ...bot.direction };
    }
  }

  private updateBotAI() {
    const now = Date.now();
    
    for (const snake of this.snakes.values()) {
      if (!snake.isAlive || snake.playerId >= 0) continue; // skip real players

      // Initialize bot memory if not exists
      if (!this.botMemories.has(snake.id)) {
        this.botMemories.set(snake.id, {
          lastSeenFood: null,
          lastSeenFoodTime: 0,
          explorationTarget: null,
          explorationStartTime: 0,
          lastDirectionChange: 0,
          wanderDirection: { x: Math.random() - 0.5, y: Math.random() - 0.5 },
          targetDirection: { x: 0, y: 0 },
          lastUpdateTime: 0
        });
      }
      
      const memory = this.botMemories.get(snake.id)!;

      // --- HUMAN-LIKE BOT AI LOGIC ---------------------------------
      
      // 1) Check for food within vision range (limited visibility)
      let visibleFood: { x: number; y: number } | null = null;
      let closestDistSq = this.BOT_VISION_RANGE * this.BOT_VISION_RANGE;
      
      for (const food of this.foods.values()) {
        if (food.roomId !== snake.roomId) continue;
        
        const dx = food.position.x - snake.position.x;
        const dy = food.position.y - snake.position.y;
        const distSq = dx * dx + dy * dy;
        
        if (distSq <= closestDistSq) {
          closestDistSq = distSq;
          visibleFood = food.position;
        }
      }

      // 2) Update memory based on what we can see
      if (visibleFood) {
        memory.lastSeenFood = visibleFood;
        memory.lastSeenFoodTime = now;
        memory.explorationTarget = null; // Clear exploration when we see food
      } else {
        // Check if memory is still valid
        if (now - memory.lastSeenFoodTime > this.BOT_MEMORY_DURATION) {
          memory.lastSeenFood = null;
        }
      }

      // 3) Decide movement direction based on current state
      let targetDirection: { x: number; y: number };

      if (visibleFood) {
        // Move towards visible food
        targetDirection = {
          x: visibleFood.x - snake.position.x,
          y: visibleFood.y - snake.position.y
        };
      } else if (memory.lastSeenFood && (now - memory.lastSeenFoodTime) <= this.BOT_MEMORY_DURATION) {
        // Move towards remembered food location
        targetDirection = {
          x: memory.lastSeenFood.x - snake.position.x,
          y: memory.lastSeenFood.y - snake.position.y
        };
        // Add some uncertainty to memory-based movement
        targetDirection.x += (Math.random() - 0.5) * 0.5;
        targetDirection.y += (Math.random() - 0.5) * 0.5;
      } else {
        // No food visible or remembered - explore/wander
        if (!memory.explorationTarget || (now - memory.explorationStartTime) > this.BOT_EXPLORATION_DURATION) {
          // Set new exploration target
          memory.explorationTarget = {
            x: (Math.random() - 0.5) * this.GAME_AREA.width * 0.8,
            y: (Math.random() - 0.5) * this.GAME_AREA.height * 0.8
          };
          memory.explorationStartTime = now;
        }
        
        // Move towards exploration target
        targetDirection = {
          x: memory.explorationTarget.x - snake.position.x,
          y: memory.explorationTarget.y - snake.position.y
        };
        
        // Add some randomness to exploration
        targetDirection.x += (Math.random() - 0.5) * 0.3;
        targetDirection.y += (Math.random() - 0.5) * 0.3;
      }

      // 4) Occasionally change direction randomly (like humans do)
      if (now - memory.lastDirectionChange > this.BOT_DIRECTION_CHANGE_INTERVAL) {
        if (Math.random() < 0.3) { // 30% chance to change direction
          const randomAngle = Math.random() * 2 * Math.PI;
          memory.wanderDirection = {
            x: Math.cos(randomAngle),
            y: Math.sin(randomAngle)
          };
          memory.lastDirectionChange = now;
        }
      }

      // 5) Blend target direction with current wander direction for more natural movement
      const blendFactor = visibleFood ? 0.9 : 0.7; // More direct when food is visible
      const finalDirection = {
        x: targetDirection.x * blendFactor + memory.wanderDirection.x * (1 - blendFactor),
        y: targetDirection.y * blendFactor + memory.wanderDirection.y * (1 - blendFactor)
      };

      // 6) Normalize direction
      const mag = Math.hypot(finalDirection.x, finalDirection.y) || 1;
      finalDirection.x /= mag;
      finalDirection.y /= mag;

      // 7) Apply smooth direction interpolation instead of instant changes
      const deltaTime = (now - memory.lastUpdateTime) / 1000; // Convert to seconds
      memory.lastUpdateTime = now;
      
      // Calculate interpolation factor based on time and rotation speed
      const interpolationFactor = Math.min(1.0, this.BOT_ROTATION_SPEED * deltaTime * 60); // 60 FPS base
      
      // Smoothly interpolate from current direction to target direction
      const currentDir = snake.direction;
      const targetDir = finalDirection;
      
      const interpolatedDirection = {
        x: currentDir.x + (targetDir.x - currentDir.x) * interpolationFactor,
        y: currentDir.y + (targetDir.y - currentDir.y) * interpolationFactor
      };
      
      // Normalize the interpolated direction
      const magInterpolated = Math.hypot(interpolatedDirection.x, interpolatedDirection.y) || 1;
      interpolatedDirection.x /= magInterpolated;
      interpolatedDirection.y /= magInterpolated;
      
      // Apply the interpolated direction
      if (Math.abs(interpolatedDirection.x) > 0.001 || Math.abs(interpolatedDirection.y) > 0.001) {
        snake.direction.x = interpolatedDirection.x;
        snake.direction.y = interpolatedDirection.y;
      } else {
        // Fallback to current direction or default
        if (Math.abs(snake.direction.x) < 0.001 && Math.abs(snake.direction.y) < 0.001) {
          snake.direction = { x: 1, y: 0 }; // Default direction
        }
      }

      // 8) Occasionally boost (like humans do when they see food or are in danger)
      const boostChance = visibleFood ? 0.05 : 0.01; // More likely to boost when food is visible
      snake.isBoosting = Math.random() < boostChance;
      
      // 9) Update last input for consistency
      snake.lastInput.direction = { x: snake.direction.x, y: snake.direction.y };
      snake.lastInput.isBoosting = snake.isBoosting;
      snake.lastInput.timestamp = now;
    }
  }

  private async respawnBot(roomId: number) {
    const bot = await this.createBotSnake(roomId);
    // Use safe spawn position for respawning bots
    bot.position = this.findSafeSpawnPosition(roomId);
    // Randomize initial direction
    const angle = Math.random() * 2 * Math.PI;
    bot.direction = {
      x: Math.cos(angle),
      y: Math.sin(angle),
    };
    bot.lastInput.direction = { ...bot.direction };
    
    this.logger.log(`Bot respawned in room ${roomId} with ID ${bot.id}`);
  }

  // Public method for testing bot spawning
  public async testSpawnBots(roomId: number, count: number = 3) {
    this.logger.log(`Test spawning ${count} bots in room ${roomId}`);
    await this.ensureBotPopulation(roomId, count);
    return `Spawned ${count} bots in room ${roomId}`;
  }

  getFoodsInRoom(roomId: number): Food[] {
    return Array.from(this.foods.values()).filter(food => food.roomId === roomId);
  }

  getGameStatistics(): {
    totalSnakes: number;
    aliveSnakes: number;
    deadSnakes: number;
    totalBots: number;
    aliveBots: number;
    deadBots: number;
    totalFood: number;
    rooms: number;
  } {
    const allSnakes = Array.from(this.snakes.values());
    const aliveSnakes = allSnakes.filter(s => s.isAlive);
    const deadSnakes = allSnakes.filter(s => !s.isAlive);
    const bots = allSnakes.filter(s => s.playerId < 0);
    const aliveBots = bots.filter(s => s.isAlive);
    const deadBots = bots.filter(s => !s.isAlive);
    const rooms = new Set(allSnakes.map(s => s.roomId)).size;
    
    return {
      totalSnakes: allSnakes.length,
      aliveSnakes: aliveSnakes.length,
      deadSnakes: deadSnakes.length,
      totalBots: bots.length,
      aliveBots: aliveBots.length,
      deadBots: deadBots.length,
      totalFood: this.foods.size,
      rooms
    };
  }
} 