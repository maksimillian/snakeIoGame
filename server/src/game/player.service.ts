import { Injectable } from '@nestjs/common';
import { PrismaService } from '../prisma/prisma.service';

@Injectable()
export class PlayerService {
  constructor(private prisma: PrismaService) {}

  async getPlayer(id: number) {
    return this.prisma.player.findUnique({
      where: { id },
      include: {
        ownedSkins: {
          include: {
            skin: true,
          },
        },
      },
    });
  }

  async getPlayerStats(id: number) {
    const player = await this.prisma.player.findUnique({
      where: { id },
      include: {
        sessions: {
          orderBy: { score: 'desc' },
          take: 1,
        },
      },
    });

    if (!player) {
      throw new Error('Player not found');
    }

    return {
      bestScore: player.bestScore,
      totalKills: player.totalKills,
      bestRank: player.bestRank,
      lastSession: player.sessions[0],
    };
  }

  async getPlayerSkins(id: number) {
    return this.prisma.playerSkin.findMany({
      where: { playerId: id },
      include: {
        skin: true,
      },
    });
  }

  async unlockSkin(playerId: number, skinId: number) {
    const skin = await this.prisma.skin.findUnique({
      where: { id: skinId },
    });

    if (!skin) {
      throw new Error('Skin not found');
    }

    const player = await this.prisma.player.findUnique({
      where: { id: playerId },
    });

    if (!player) {
      throw new Error('Player not found');
    }

    // Check if player meets requirements
    if (skin.unlockType === 'SCORE_GATE' && player.bestScore < (skin.scoreRequired || 0)) {
      throw new Error('Score requirement not met');
    }

    // Create player skin
    return this.prisma.playerSkin.create({
      data: {
        playerId,
        skinId,
        isEquipped: false,
      },
      include: {
        skin: true,
      },
    });
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
} 