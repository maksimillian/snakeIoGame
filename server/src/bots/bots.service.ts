import { Injectable } from '@nestjs/common';
import { PrismaService } from '../prisma/prisma.service';
import { Bot, Room } from '@prisma/client';

@Injectable()
export class BotsService {
  constructor(private prisma: PrismaService) {}

  private botNames = [
    'SnakeMaster', 'CoilKing', 'SerpentPro', 'ViperElite', 'PythonPilot',
    'SlitherChamp', 'ReptileRacer', 'ScaleSpeed', 'FangFlash', 'TailTitan',
    'CobraCruiser', 'AdderAce', 'MambaMover', 'RattlerRush', 'Copperhead',
    'Cottonmouth', 'Bushmaster', 'FerDeLance', 'Taipan', 'BlackMamba'
  ];

  async spawnBotsForRoom(roomId: number, count: number = 7): Promise<Bot[]> {
    const bots: Bot[] = [];
    
    for (let i = 0; i < count; i++) {
      const botName = this.getRandomBotName();
      const bot = await this.prisma.bot.create({
        data: {
          roomId,
          name: botName,
          score: 0,
          kills: 0,
          length: 1,
          isAlive: true,
        },
      });
      bots.push(bot);
    }

    // Update room bot count
    await this.prisma.room.update({
      where: { id: roomId },
      data: { botCount: count },
    });

    return bots;
  }

  async getBotsInRoom(roomId: number): Promise<Bot[]> {
    return this.prisma.bot.findMany({
      where: { roomId, isAlive: true },
    });
  }

  async updateBotPosition(botId: number, length: number): Promise<Bot> {
    return this.prisma.bot.update({
      where: { id: botId },
      data: { length, updatedAt: new Date() },
    });
  }

  async botEatFood(botId: number, scoreIncrease: number = 10): Promise<Bot> {
    const bot = await this.prisma.bot.findUnique({
      where: { id: botId },
    });

    if (!bot) throw new Error('Bot not found');

    return this.prisma.bot.update({
      where: { id: botId },
      data: {
        score: bot.score + scoreIncrease,
        length: bot.length + 1,
        updatedAt: new Date(),
      },
    });
  }

  async botKillSnake(botId: number, killScore: number = 50): Promise<Bot> {
    const bot = await this.prisma.bot.findUnique({
      where: { id: botId },
    });

    if (!bot) throw new Error('Bot not found');

    return this.prisma.bot.update({
      where: { id: botId },
      data: {
        kills: bot.kills + 1,
        score: bot.score + killScore,
        updatedAt: new Date(),
      },
    });
  }

  async botDeath(botId: number): Promise<Bot> {
    return this.prisma.bot.update({
      where: { id: botId },
      data: { isAlive: false, updatedAt: new Date() },
    });
  }

  async respawnBot(botId: number): Promise<Bot> {
    return this.prisma.bot.update({
      where: { id: botId },
      data: {
        score: 0,
        kills: 0,
        length: 1,
        isAlive: true,
        spawnedAt: new Date(),
        updatedAt: new Date(),
      },
    });
  }

  async maintainBotPopulation(roomId: number, targetCount: number = 7): Promise<void> {
    const aliveBots = await this.prisma.bot.count({
      where: { roomId, isAlive: true },
    });

    if (aliveBots < targetCount) {
      const botsToSpawn = targetCount - aliveBots;
      await this.spawnBotsForRoom(roomId, botsToSpawn);
    }
  }

  private getRandomBotName(): string {
    const randomIndex = Math.floor(Math.random() * this.botNames.length);
    return this.botNames[randomIndex];
  }
} 