import { Injectable, OnModuleInit, OnModuleDestroy } from '@nestjs/common';
import { PrismaClient } from '@prisma/client';

@Injectable()
export class PrismaService extends PrismaClient implements OnModuleInit, OnModuleDestroy {
  constructor() {
    super({
      datasources: {
        db: {
          url: process.env.DATABASE_URL,
        },
      },
    });
  }

  async onModuleInit() {
    // Reduced prisma service init logging
    
    try {
      await this.$connect();
      // Reduced database connection logging
      
      // Test the connection by counting rooms (but don't log every detail)
      try {
        const roomCount = await this.room.count();
        // Reduced database stats logging
      } catch (modelError) {
        console.error('❌ Error accessing room model:', modelError.message);
        // Reduced available models logging
      }
    } catch (error) {
      console.error('❌ Failed to connect to database:', error.message);
      throw error;
    }
  }

  async onModuleDestroy() {
    await this.$disconnect();
  }
} 