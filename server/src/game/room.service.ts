import { Injectable } from '@nestjs/common';
import { PrismaService } from '../prisma/prisma.service';

@Injectable()
export class RoomService {
  constructor(private prisma: PrismaService) {}

  async getRooms() {
    return this.prisma.room.findMany({
      include: {
        server: true,
        sessions: {
          include: {
            player: true,
          },
        },
      },
    });
  }

  async getRoom(id: number) {
    return this.prisma.room.findUnique({
      where: { id },
      include: {
        server: true,
        sessions: {
          include: {
            player: true,
          },
        },
      },
    });
  }

  async createRoom(name: string, serverId: number) {
    return this.prisma.room.create({
      data: {
        name,
        serverId,
      },
      include: {
        server: true,
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