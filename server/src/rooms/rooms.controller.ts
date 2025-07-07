import { Controller, Get, Post, Body, Param, UseGuards } from '@nestjs/common';
import { RoomsService } from './rooms.service';
import { JwtAuthGuard } from '../auth/guards/jwt-auth.guard';
import { Room, PlayerSession } from '@prisma/client';

@Controller('rooms')
@UseGuards(JwtAuthGuard)
export class RoomsController {
  constructor(private readonly roomsService: RoomsService) {}

  @Get()
  async findAll(): Promise<Room[]> {
    return this.roomsService.findAll();
  }

  @Get(':id')
  async findOne(@Param('id') id: string): Promise<Room | null> {
    return this.roomsService.findOne(+id);
  }

  @Post()
  async createRoom(
    @Body('name') name: string,
    @Body('serverId') serverId: number,
  ): Promise<Room> {
    return this.roomsService.createRoom(name, serverId);
  }

  @Post('play')
  async autoJoinRoom(@Body('serverId') serverId?: number): Promise<Room | null> {
    return this.roomsService.autoJoinRoom(serverId);
  }

  @Post('join')
  async joinByFriendCode(@Body('friendCode') friendCode: string): Promise<Room | null> {
    return this.roomsService.findRoomByFriendCode(friendCode);
  }

  @Get(':id/sessions')
  async getRoomSessions(@Param('id') id: string): Promise<PlayerSession[]> {
    return this.roomsService.getRoomSessions(+id);
  }

  @Get(':id/leaderboard')
  async getRoomLeaderboard(@Param('id') id: string) {
    return this.roomsService.getRoomLeaderboard(+id);
  }

  @Get(':id/players')
  async getRoomPlayers(@Param('id') id: string) {
    const room = await this.roomsService.findOne(+id);
    if (!room) return null;

    const sessions = await this.roomsService.getRoomSessions(+id);
    const bots = await this.roomsService.getRoomBots(+id);

    return {
      players: sessions.map(session => ({
        id: session.playerId,
        name: session.player.username,
        score: session.score,
        kills: session.kills,
        length: session.length,
        isBot: false,
      })),
      bots: bots.map(bot => ({
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
} 