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

  @Get(':id/sessions')
  async getRoomSessions(@Param('id') id: string): Promise<PlayerSession[]> {
    return this.roomsService.getRoomSessions(+id);
  }
} 