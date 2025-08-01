import { Injectable } from '@nestjs/common';
import { PrismaService } from '../prisma/prisma.service';
import { Room, PlayerSession, Player } from '@prisma/client';

@Injectable()
export class RoomsService {
  constructor(private readonly prisma: PrismaService) {}

  async findAll(): Promise<Room[]> {
    const rooms = await this.prisma.room.findMany({
      include: {
        server: true,
        sessions: {
          include: {
            player: true,
          },
        },
        bots: true,
      },
    });
    
    // Reduced room listing logs
    
    return rooms;
  }

  async findOne(id: number): Promise<Room | null> {
    return this.prisma.room.findUnique({
      where: { id },
      include: {
        server: true,
        sessions: {
          include: {
            player: true,
          },
        },
        bots: true,
      },
    });
  }

  async findRoomByFriendCode(friendCode: string): Promise<Room | null> {
    // Reduced friend code lookup logging
    
    // Convert to uppercase for case-insensitive matching
    const normalizedFriendCode = friendCode.toUpperCase();
    
    const room = await this.prisma.room.findFirst({
      where: { 
        friendCode: {
          equals: normalizedFriendCode,
          mode: 'insensitive'
        }
      },
      include: {
        server: true,
        sessions: {
          include: {
            player: true,
          },
        },
        bots: true,
      },
    });
    
    if (room) {
      // Reduced room found logging
    } else {
      // Reduced room not found logging
    }
    
    return room;
  }

  async createRoom(name: string, serverId: number): Promise<Room> {
    const friendCode = this.generateFriendCode();
    
    // Check if this friend code already exists
    const existingRoom = await this.prisma.room.findUnique({
      where: { friendCode },
    });
    
    if (existingRoom) {
      // Reduced friend code generation logging
      return this.createRoom(name, serverId); // Recursively try again
    }
    
    // Reduced room creation logging
    
    return this.prisma.room.create({
      data: {
        name,
        serverId,
        friendCode,
        maxPlayers: 10,
        currentPlayers: 0,
        botCount: 7,
        isActive: true,
      },
      include: {
        server: true,
      },
    });
  }

  async autoJoinRoom(serverId?: number): Promise<Room | null> {
    // Only log occasionally to reduce performance impact
    if (Math.random() < 0.01) { // Log only 1% of the time
    // Reduced auto join logging
    }
    
    if (!this.prisma) {
      console.error('Prisma service is undefined!');
      throw new Error('Prisma service not injected');
    }
    
    const whereClause: any = {
      isActive: true,
      currentPlayers: { lt: 10 }, // Less than max players
    };

    if (serverId) {
      whereClause.serverId = serverId;
    }

    // Only log occasionally to reduce performance impact
    if (Math.random() < 0.01) { // Log only 1% of the time
    // Reduced room search logging
    }
    
    return this.prisma.room.findFirst({
      where: whereClause,
      include: {
        server: true,
        sessions: {
          include: {
            player: true,
          },
        },
        bots: true,
      },
      orderBy: { currentPlayers: 'asc' }, // Prefer rooms with fewer players
    });
  }

  async updateRoomPlayerCount(roomId: number, playerCount: number): Promise<Room> {
    return this.prisma.room.update({
      where: { id: roomId },
      data: { currentPlayers: playerCount },
    });
  }

  async getRoomSessions(roomId: number): Promise<(PlayerSession & { player: Player })[]> {
    return this.prisma.playerSession.findMany({
      where: { roomId, isAlive: true },
      include: {
        player: true,
      },
    });
  }

  async getRoomBots(roomId: number) {
    return this.prisma.bot.findMany({
      where: { roomId, isAlive: true },
    });
  }

  async getRoomLeaderboard(roomId: number) {
    const sessions = await this.prisma.playerSession.findMany({
      where: { roomId, isAlive: true },
      orderBy: { score: 'desc' },
      take: 10,
      include: {
        player: true,
      },
    });

    const bots = await this.prisma.bot.findMany({
      where: { roomId, isAlive: true },
      orderBy: { score: 'desc' },
      take: 10,
    });

    // Combine players and bots for leaderboard
    const allEntries = [
      ...sessions.map((session) => ({
        name: session.player.username,
        score: session.score,
        kills: session.kills,
        isBot: false,
      })),
      ...bots.map((bot) => ({
        name: bot.name,
        score: bot.score,
        kills: bot.kills,
        isBot: true,
      })),
    ].sort((a, b) => b.score - a.score);

    return allEntries.slice(0, 10).map((entry, index) => ({
      ...entry,
      rank: index + 1,
    }));
  }

  private generateFriendCode(): string {
    const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789';
    let result = '';
    for (let i = 0; i < 6; i++) {
      result += chars.charAt(Math.floor(Math.random() * chars.length));
    }
    
    // Avoid common words that might be confusing
    const commonWords = ['WELCOME', 'HELLO', 'TEST', 'DEMO', 'GAME', 'PLAY', 'SNAKE', 'JOIN'];
    if (commonWords.includes(result)) {
      // Generate a different code if it matches a common word
      return this.generateFriendCode();
    }
    
    // Reduced friend code generation logging
    return result;
  }
} 