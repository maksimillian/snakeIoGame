import { PrismaClient, Region, SkinUnlockType } from '@prisma/client';

const prisma = new PrismaClient();

async function main() {
  // Create default server
  const server = await prisma.server.create({
    data: {
      region: Region.USEast,
      capacity: 5000,
    },
  });

  // Create default room
  const room = await prisma.room.create({
    data: {
      serverId: server.id,
      name: 'Main Room',
    },
  });

  // Create default skins
  const defaultSkin = await prisma.skin.create({
    data: {
      name: 'Default Snake',
      rarity: 1,
      unlockType: SkinUnlockType.FREE,
      assetUrl: '/skins/default.png',
    },
  });

  const goldenSkin = await prisma.skin.create({
    data: {
      name: 'Golden Snake',
      rarity: 3,
      unlockType: SkinUnlockType.SCORE_GATE,
      scoreRequired: 1000,
      assetUrl: '/skins/golden.png',
    },
  });

  console.log('Seed data created successfully');
}

main()
  .catch((e) => {
    console.error(e);
    process.exit(1);
  })
  .finally(async () => {
    await prisma.$disconnect();
  }); 