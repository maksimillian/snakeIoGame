import { Injectable } from '@nestjs/common';
import { PrismaService } from '../prisma/prisma.service';
import { Room, PlayerSession } from '@prisma/client';

@Injectable()
export class RoomsService {
  constructor(private readonly prisma: PrismaService) {}

  async findAll(): Promise<Room[]> {
    return this.prisma.room.findMany();
  }

  async findOne(id: number): Promise<Room | null> {
    return this.prisma.room.findUnique({
      where: { id },
    });
  }

  async createRoom(name: string, serverId: number): Promise<Room> {
    return this.prisma.room.create({
      data: {
        name,
        serverId,
      },
    });
  }

  async getRoomSessions(roomId: number): Promise<PlayerSession[]> {
    return this.prisma.playerSession.findMany({
      where: { roomId },
      include: {
        player: true,
      },
    });
  }

  async getRoomLeaderboard(roomId: number) {
    const sessions = await this.prisma.playerSession.findMany({
      where: { roomId },
      orderBy: { score: 'desc' },
      take: 10,
      include: {
        player: true,
      },
    });

    return sessions.map((session, index) => ({
      rank: index + 1,
      player: session.player.username,
      score: session.score,
      kills: session.kills,
    }));
  }
} 