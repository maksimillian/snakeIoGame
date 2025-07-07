import { Injectable, UnauthorizedException, ConflictException } from '@nestjs/common';
import { JwtService } from '@nestjs/jwt';
import { PrismaService } from '../prisma/prisma.service';
import * as bcrypt from 'bcrypt';

@Injectable()
export class AuthService {
  constructor(
    private prisma: PrismaService,
    private jwtService: JwtService,
  ) {}

  async register(username: string, email: string, password: string) {
    // Check if username or email already exists
    const existingUser = await this.prisma.player.findFirst({
      where: {
        OR: [
          { username },
          { email },
        ],
      },
    });

    if (existingUser) {
      throw new ConflictException('Username or email already exists');
    }

    // Hash password
    const hashedPassword = await bcrypt.hash(password, 10);

    // Create new player
    const player = await this.prisma.player.create({
      data: {
        username,
        email,
        passwordHash: hashedPassword,
      },
    });

    // Generate JWT token
    const token = this.jwtService.sign({
      sub: player.id,
      username: player.username,
    });

    return {
      token,
      player: {
        id: player.id,
        username: player.username,
        email: player.email,
      },
    };
  }

  async login(username: string, password: string) {
    // Find player by username
    const player = await this.prisma.player.findFirst({
      where: { username },
    });

    if (!player) {
      throw new UnauthorizedException('Invalid credentials');
    }

    // Verify password
    const isPasswordValid = await bcrypt.compare(password, player.passwordHash);

    if (!isPasswordValid) {
      throw new UnauthorizedException('Invalid credentials');
    }

    // Generate JWT token
    const token = this.jwtService.sign({
      sub: player.id,
      username: player.username,
    });

    return {
      token,
      player: {
        id: player.id,
        username: player.username,
        email: player.email,
      },
    };
  }

  async getCurrentPlayer(id: number) {
    const player = await this.prisma.player.findUnique({
      where: { id },
      include: {
        ownedSkins: {
          include: {
            skin: true,
          },
        },
      },
    });

    if (!player) {
      throw new UnauthorizedException('Player not found');
    }

    return {
      id: player.id,
      username: player.username,
      email: player.email,
      bestScore: player.bestScore,
      totalKills: player.totalKills,
      bestRank: player.bestRank,
      ownedSkins: player.ownedSkins,
    };
  }
} 