import { Controller, Get } from '@nestjs/common';
import { AppService } from './app.service';

@Controller()
export class AppController {
  constructor(private readonly appService: AppService) {}

  @Get()
  getHello(): string {
    return this.appService.getHello();
  }

  @Get('health')
  getHealth(): { status: string; timestamp: string } {
    return {
      status: 'ok',
      timestamp: new Date().toISOString()
    };
  }

  @Get('socket-test')
  getSocketTest(): { message: string; endpoints: string[] } {
    return {
      message: 'Socket.IO server is running',
      endpoints: [
        'ws://localhost:3000 (root namespace)',
        'ws://localhost:3000/game (game namespace)'
      ]
    };
  }
} 