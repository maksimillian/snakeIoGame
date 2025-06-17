import { Controller, Get, Param, UseGuards, Request } from '@nestjs/common';
import { StatsService } from './stats.service';
import { JwtAuthGuard } from '../auth/guards/jwt-auth.guard';

@Controller('stats')
export class StatsController {
  constructor(private statsService: StatsService) {}

  @UseGuards(JwtAuthGuard)
  @Get('session/:id')
  async getSessionStats(@Param('id') id: string) {
    return this.statsService.getSessionStats(+id);
  }

  @UseGuards(JwtAuthGuard)
  @Get('player/:id')
  async getPlayerStats(@Param('id') id: string) {
    return this.statsService.getPlayerStats(+id);
  }

  @Get('leaderboard')
  async getGlobalLeaderboard() {
    return this.statsService.getGlobalLeaderboard();
  }

  @Get('leaderboard/room/:id')
  async getRoomLeaderboard(@Param('id') id: string) {
    return this.statsService.getRoomLeaderboard(+id);
  }
} 