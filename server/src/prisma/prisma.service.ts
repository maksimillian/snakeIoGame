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
    console.log('=== PRISMA SERVICE INIT ===');
    console.log('DATABASE_URL:', process.env.DATABASE_URL ? 'Set' : 'NOT SET');
    console.log('Attempting to connect to database...');
    
    try {
      await this.$connect();
      console.log('✅ Successfully connected to database');
      
      // Test the connection by counting rooms (but don't log every detail)
      try {
        const roomCount = await this.room.count();
        console.log(`📊 Found ${roomCount} rooms in database`);
        
        // Only log sample rooms if there are very few (for debugging)
        if (roomCount > 0 && roomCount <= 5) {
          const rooms = await this.room.findMany({ take: 3 });
          console.log('📋 Sample rooms:', rooms.map(r => ({ id: r.id, name: r.name })));
        }
      } catch (modelError) {
        console.error('❌ Error accessing room model:', modelError.message);
        console.log('Available models:', Object.keys(this));
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