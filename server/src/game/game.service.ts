import { Injectable, OnModuleInit } from '@nestjs/common';
import { PrismaService } from '../prisma/prisma.service';
import { RoomService } from './room.service';
import { PlayerService } from './player.service';

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
}

export interface Food {
  id: string;
  roomId: number;
  position: { x: number; y: number };
  value: number;
}

@Injectable()
export class GameService implements OnModuleInit {
  private snakes: Map<string, Snake> = new Map();
  private foods: Map<string, Food> = new Map();
  private playerSessions: Map<string, number> = new Map();
  private readonly GAME_LOOP_INTERVAL = 1000 / 60; // 60 FPS
  private readonly FOOD_SPAWN_INTERVAL = 5000; // 5 seconds
  private readonly FOOD_VALUE_RANGE = { min: 1, max: 3 };
  private readonly GAME_AREA = { width: 2000, height: 2000 };

  constructor(
    private prisma: PrismaService,
    private roomService: RoomService,
    private playerService: PlayerService,
  ) {}

  onModuleInit() {
    this.initializeGameLoop();
    this.initializeFoodSpawner();
  }

  private initializeGameLoop() {
    setInterval(() => {
      this.updateGameState();
    }, this.GAME_LOOP_INTERVAL);
  }

  private initializeFoodSpawner() {
    setInterval(() => {
      this.spawnFood();
    }, this.FOOD_SPAWN_INTERVAL);
  }

  private updateGameState() {
    // Update snake positions
    for (const snake of this.snakes.values()) {
      const speed = snake.isBoosting ? snake.speed * 1.5 : snake.speed;
      snake.position.x += snake.direction.x * speed;
      snake.position.y += snake.direction.y * speed;
      snake.lastUpdate = Date.now();

      // Keep snake within game area
      snake.position.x = Math.max(0, Math.min(this.GAME_AREA.width, snake.position.x));
      snake.position.y = Math.max(0, Math.min(this.GAME_AREA.height, snake.position.y));
    }

    // Check collisions
    this.checkCollisions();
  }

  private checkCollisions() {
    // Check snake-snake collisions
    for (const snake1 of this.snakes.values()) {
      for (const snake2 of this.snakes.values()) {
        if (snake1.id === snake2.id) continue;

        if (this.checkSnakeCollision(snake1, snake2)) {
          this.handleSnakeKill(snake1.id, parseInt(snake2.id));
        }
      }
    }

    // Check snake-food collisions
    for (const snake of this.snakes.values()) {
      for (const food of this.foods.values()) {
        if (this.checkFoodCollision(snake, food)) {
          this.handleSnakeEat(snake.id, food.id);
        }
      }
    }
  }

  private checkSnakeCollision(snake1: Snake, snake2: Snake): boolean {
    const dx = snake1.position.x - snake2.position.x;
    const dy = snake1.position.y - snake2.position.y;
    const distance = Math.sqrt(dx * dx + dy * dy);
    return distance < 20; // Collision radius
  }

  private checkFoodCollision(snake: Snake, food: Food): boolean {
    const dx = snake.position.x - food.position.x;
    const dy = snake.position.y - food.position.y;
    const distance = Math.sqrt(dx * dx + dy * dy);
    return distance < 15; // Food collection radius
  }

  private spawnFood() {
    const rooms = Array.from(new Set(Array.from(this.snakes.values()).map(snake => snake.roomId)));
    
    for (const roomId of rooms) {
      const food: Food = {
        id: Math.random().toString(36).substring(7),
        roomId,
        position: {
          x: Math.random() * this.GAME_AREA.width,
          y: Math.random() * this.GAME_AREA.height,
        },
        value: Math.floor(Math.random() * (this.FOOD_VALUE_RANGE.max - this.FOOD_VALUE_RANGE.min + 1)) + this.FOOD_VALUE_RANGE.min,
      };

      this.foods.set(food.id, food);
    }
  }

  async handlePlayerJoin(socketId: string, roomId: number, playerId: number) {
    const room = await this.roomService.getRoom(roomId);
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

    return {
      room,
      session,
    };
  }

  async handlePlayerDisconnect(socketId: string) {
    await this.handlePlayerLeave(socketId);
  }

  async handlePlayerLeave(socketId: string) {
    const sessionId = this.playerSessions.get(socketId);
    if (sessionId) {
      const session = await this.prisma.playerSession.findUnique({
        where: { id: sessionId },
        include: { player: true },
      });

      if (session) {
        await this.prisma.player.update({
          where: { id: session.playerId },
          data: {
            bestScore: Math.max(session.score, session.player.bestScore),
            totalKills: session.player.totalKills + session.kills,
          },
        });
      }

      this.playerSessions.delete(socketId);
    }

    const snake = this.snakes.get(socketId);
    if (snake) {
      this.snakes.delete(socketId);
    }
  }

  async spawnSnake(socketId: string, roomId: number, playerId: number): Promise<Snake> {
    const snake: Snake = {
      id: socketId,
      playerId,
      roomId,
      position: { x: Math.random() * this.GAME_AREA.width, y: Math.random() * this.GAME_AREA.height },
      direction: { x: 1, y: 0 },
      length: 1,
      speed: 5,
      score: 0,
      kills: 0,
      isBoosting: false,
      lastUpdate: Date.now(),
    };

    this.snakes.set(socketId, snake);
    return snake;
  }

  async updateSnakeDirection(socketId: string, direction: { x: number; y: number }) {
    const snake = this.snakes.get(socketId);
    if (snake) {
      snake.direction = direction;
      snake.lastUpdate = Date.now();
    }
    return snake;
  }

  async updateSnakeBoost(socketId: string, isBoosting: boolean) {
    const snake = this.snakes.get(socketId);
    if (snake) {
      snake.isBoosting = isBoosting;
      snake.speed = isBoosting ? 8 : 5;
      snake.lastUpdate = Date.now();
    }
    return snake;
  }

  async handleSnakeDeath(socketId: string) {
    const snake = this.snakes.get(socketId);
    if (snake) {
      const sessionId = this.playerSessions.get(socketId);
      if (sessionId) {
        await this.prisma.playerSession.update({
          where: { id: sessionId },
          data: {
            score: snake.score,
            kills: snake.kills,
            length: snake.length,
          },
        });
      }
      this.snakes.delete(socketId);
      return snake;
    }
    return null;
  }

  async handleSnakeEat(socketId: string, foodId: string) {
    const snake = this.snakes.get(socketId);
    const food = this.foods.get(foodId);

    if (snake && food && snake.roomId === food.roomId) {
      snake.length += food.value;
      snake.score += food.value * 10;
      this.foods.delete(foodId);

      const sessionId = this.playerSessions.get(socketId);
      if (sessionId) {
        await this.prisma.playerSession.update({
          where: { id: sessionId },
          data: {
            score: snake.score,
            length: snake.length,
          },
        });
      }

      return { snake, food };
    }
  }

  async handleSnakeKill(socketId: string, killedSnakeId: number) {
    const killer = this.snakes.get(socketId);
    const killed = this.snakes.get(killedSnakeId.toString());

    if (killer && killed && killer.roomId === killed.roomId) {
      killer.score += killed.score;
      killer.kills += 1;
      this.snakes.delete(killedSnakeId.toString());

      const sessionId = this.playerSessions.get(socketId);
      if (sessionId) {
        await this.prisma.playerSession.update({
          where: { id: sessionId },
          data: {
            score: killer.score,
            kills: killer.kills,
          },
        });
      }

      return { killer, killed };
    }
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
    return Array.from(this.snakes.values()).filter(snake => snake.roomId === roomId);
  }

  getFoodsInRoom(roomId: number): Food[] {
    return Array.from(this.foods.values()).filter(food => food.roomId === roomId);
  }
} 