import { NestFactory } from '@nestjs/core';
import { AppModule } from './app.module';
import * as dotenv from 'dotenv';

dotenv.config();

async function bootstrap() {
  const app = await NestFactory.create(AppModule, {
    cors: {
      origin: true, // Allow all origins
      methods: ['GET', 'POST', 'PUT', 'DELETE', 'OPTIONS'],
      credentials: true,
      allowedHeaders: ['Content-Type', 'Authorization', 'Accept'],
    },
  });

  // Enable Socket.IO with explicit configuration
  app.enableShutdownHooks();
  
  const port = process.env.PORT || 3000;
  console.log(`Starting server on port ${port}...`);
  console.log('CORS enabled for all origins');
  console.log('WebSocket namespace: /game');
  
  await app.listen(port);
  console.log(`Server is running on port ${port}`);
  console.log(`HTTP: http://localhost:${port}`);
  console.log(`WebSocket: ws://localhost:${port}/game`);
}
bootstrap();
