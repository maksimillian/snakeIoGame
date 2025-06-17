import { Injectable } from '@nestjs/common';
import { PrismaService } from '../prisma/prisma.service';

@Injectable()
export class StatsService {
  constructor(private prisma: PrismaService) {}

  async getSessionStats(sessionId: number) {
    const session = await this.prisma.playerSession.findUnique({
      where: { id: sessionId },
      include: {
        player: true,
        room: true,
      },
    });

    if (!session) {
      return null;
    }

    return {
      id: session.id,
      player: session.player.username,
      room: session.room.name,
      score: session.score,
      kills: session.kills,
      length: session.length,
      topPosition: session.topPosition,
      spawnedAt: session.spawnedAt,
      updatedAt: session.updatedAt,
    };
  }

  async getPlayerStats(playerId: number) {
    const player = await this.prisma.player.findUnique({
      where: { id: playerId },
      include: {
        sessions: {
          orderBy: { score: 'desc' },
          take: 1,
        },
      },
    });

    if (!player) {
      return null;
    }

    return {
      id: player.id,
      username: player.username,
      bestScore: player.bestScore,
      totalKills: player.totalKills,
      bestRank: player.bestRank,
      lastSession: player.sessions[0],
    };
  }

  async getGlobalLeaderboard() {
    const players = await this.prisma.player.findMany({
      orderBy: { bestScore: 'desc' },
      take: 100,
    });

    return players.map((player, index) => ({
      rank: index + 1,
      username: player.username,
      bestScore: player.bestScore,
      totalKills: player.totalKills,
    }));
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