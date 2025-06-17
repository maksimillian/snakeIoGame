import {
  WebSocketGateway,
  WebSocketServer,
  SubscribeMessage,
  OnGatewayConnection,
  OnGatewayDisconnect,
} from '@nestjs/websockets';
import { Server, Socket } from 'socket.io';
import { GameService, Snake, Food } from './game.service';
import { RoomService } from './room.service';
import { PlayerService } from './player.service';
import { UseGuards } from '@nestjs/common';
import { WsJwtAuthGuard } from '../auth/guards/ws-jwt-auth.guard';

interface GameState {
  snakes: Snake[];
  foods: Food[];
}

@WebSocketGateway({
  cors: {
    origin: '*',
    methods: ['GET', 'POST'],
    credentials: true,
  },
  namespace: 'game',
  transports: ['websocket'],
})
@UseGuards(WsJwtAuthGuard)
export class GameGateway implements OnGatewayConnection, OnGatewayDisconnect {
  @WebSocketServer()
  server: Server;

  constructor(
    private gameService: GameService,
    private roomService: RoomService,
    private playerService: PlayerService,
  ) {}

  async handleConnection(client: Socket) {
    try {
      console.log(`Client connected: ${client.id}`);
    } catch (error) {
      console.error('Connection error:', error);
      client.disconnect();
    }
  }

  async handleDisconnect(client: Socket) {
    try {
      console.log(`Client disconnected: ${client.id}`);
      await this.gameService.handlePlayerDisconnect(client.id);
    } catch (error) {
      console.error('Disconnect error:', error);
    }
  }

  @SubscribeMessage('player:join')
  async handlePlayerJoin(client: Socket, data: { roomId: number; playerId: number }) {
    try {
      const result = await this.gameService.handlePlayerJoin(client.id, data.roomId, data.playerId);
      client.join(`room:${data.roomId}`);
      return result;
    } catch (error) {
      client.emit('error', { message: error.message });
      return null;
    }
  }

  @SubscribeMessage('player:leave')
  async handlePlayerLeave(client: Socket, data: { roomId: number }) {
    try {
      const result = await this.gameService.handlePlayerLeave(client.id);
      client.leave(`room:${data.roomId}`);
      return result;
    } catch (error) {
      client.emit('error', { message: error.message });
      return null;
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

  @SubscribeMessage('snake:move')
  async handleSnakeMove(
    client: Socket,
    data: { direction: { x: number; y: number } },
  ) {
    try {
      const snake = await this.gameService.updateSnakeDirection(client.id, data.direction);
      if (snake) {
        this.broadcastGameState(snake.roomId);
      }
      return snake;
    } catch (error) {
      client.emit('error', { message: error.message });
      return null;
    }
  }

  @SubscribeMessage('snake:boost')
  async handleSnakeBoost(client: Socket, data: { isBoosting: boolean }) {
    try {
      const snake = await this.gameService.updateSnakeBoost(client.id, data.isBoosting);
      if (snake) {
        this.broadcastGameState(snake.roomId);
      }
      return snake;
    } catch (error) {
      client.emit('error', { message: error.message });
      return null;
    }
  }

  @SubscribeMessage('snake:die')
  async handleSnakeDeath(client: Socket) {
    try {
      const snake = await this.gameService.handleSnakeDeath(client.id);
      if (snake) {
        this.broadcastGameState(snake.roomId);
      }
      return snake;
    } catch (error) {
      client.emit('error', { message: error.message });
      return null;
    }
  }

  @SubscribeMessage('snake:eat')
  async handleSnakeEat(client: Socket, data: { foodId: string }) {
    try {
      const result = await this.gameService.handleSnakeEat(client.id, data.foodId);
      if (result?.snake) {
        this.broadcastGameState(result.snake.roomId);
      }
      return result;
    } catch (error) {
      client.emit('error', { message: error.message });
      return null;
    }
  }

  @SubscribeMessage('snake:kill')
  async handleSnakeKill(client: Socket, data: { killedSnakeId: number }) {
    try {
      const result = await this.gameService.handleSnakeKill(client.id, data.killedSnakeId);
      if (result?.killer) {
        this.broadcastGameState(result.killer.roomId);
      }
      return result;
    } catch (error) {
      client.emit('error', { message: error.message });
      return null;
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

  private broadcastGameState(roomId: number) {
    const gameState: GameState = {
      snakes: this.gameService.getSnakesInRoom(roomId),
      foods: this.gameService.getFoodsInRoom(roomId),
    };

    this.server.to(`room:${roomId}`).emit('game:state', gameState);
  }
} 