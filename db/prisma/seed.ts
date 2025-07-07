import { PrismaClient } from '@prisma/client';

const prisma = new PrismaClient();

function generateRandomFriendCode(): string {
  const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789';
  let result = '';
  for (let i = 0; i < 6; i++) {
    result += chars.charAt(Math.floor(Math.random() * chars.length));
  }
  
  // Avoid common words that might be confusing
  const commonWords = ['WELCOME', 'HELLO', 'TEST', 'DEMO', 'GAME', 'PLAY', 'SNAKE', 'JOIN'];
  if (commonWords.includes(result)) {
    // Generate a different code if it matches a common word
    return generateRandomFriendCode();
  }
  
  return result;
}

async function main() {
  console.log('🌱 Starting database seed...');

  // Create default server
  const server = await prisma.server.upsert({
    where: { id: 1 },
    update: {},
    create: {
      id: 1,
      region: 'USEast',
      capacity: 5000,
      currentPlayers: 0,
      isActive: true,
    },
  });

  console.log('✅ Created default server:', server);

  // Create default skins
  const skins = await Promise.all([
    prisma.skin.upsert({
      where: { id: 1 },
      update: {},
      create: {
        id: 1,
        name: 'Classic Green',
        rarity: 1,
        unlockType: 'FREE',
        assetUrl: '/skins/classic-green.png',
      },
    }),
    prisma.skin.upsert({
      where: { id: 2 },
      update: {},
      create: {
        id: 2,
        name: 'Golden Serpent',
        rarity: 3,
        unlockType: 'SCORE_GATE',
        scoreRequired: 1000,
        assetUrl: '/skins/golden-serpent.png',
      },
    }),
    prisma.skin.upsert({
      where: { id: 3 },
      update: {},
      create: {
        id: 3,
        name: 'Neon Blue',
        rarity: 2,
        unlockType: 'SCORE_GATE',
        scoreRequired: 500,
        assetUrl: '/skins/neon-blue.png',
      },
    }),
    prisma.skin.upsert({
      where: { id: 4 },
      update: {},
      create: {
        id: 4,
        name: 'Fire Dragon',
        rarity: 4,
        unlockType: 'SCORE_GATE',
        scoreRequired: 2000,
        assetUrl: '/skins/fire-dragon.png',
      },
    }),
    prisma.skin.upsert({
      where: { id: 5 },
      update: {},
      create: {
        id: 5,
        name: 'Rainbow Snake',
        rarity: 5,
        unlockType: 'EVENT',
        assetUrl: '/skins/rainbow-snake.png',
      },
    }),
  ]);

  console.log('✅ Created skins:', skins.length);

  // Create a random seed room for production
  const randomRoomNames = [
    'Serpent Sanctuary', 'Viper Valley', 'Python Palace', 'Coil Castle', 
    'Slither Station', 'Reptile Realm', 'Snake Summit', 'Scale Sanctuary',
    'Fang Fortress', 'Venom Village', 'Cobra Corner', 'Rattler Retreat'
  ];
  
  const randomRoomName = randomRoomNames[Math.floor(Math.random() * randomRoomNames.length)];
  const randomFriendCode = generateRandomFriendCode();
  
  const room = await prisma.room.upsert({
    where: { id: 1 },
    update: {},
    create: {
      id: 1,
      serverId: 1,
      name: randomRoomName,
      friendCode: randomFriendCode,
      maxPlayers: 10,
      currentPlayers: 0,
      botCount: 7,
      isActive: true,
    },
  });

  console.log(`✅ Created seed room: "${randomRoomName}" with friend code: ${randomFriendCode}`);

  // Create some random bots for the room
  const botNames = [
    'ShadowStrike', 'ThunderCoil', 'FrostFang', 'BlazeBite', 'StormScale',
    'VoidViper', 'CrystalCobra', 'MysticMamba', 'PhantomPython', 'EchoElapidae'
  ];

  const bots = await Promise.all(
    botNames.map((name, index) =>
      prisma.bot.upsert({
        where: { id: index + 1 },
        update: {},
        create: {
          id: index + 1,
          roomId: 1,
          name,
          score: Math.floor(Math.random() * 100),
          kills: Math.floor(Math.random() * 5),
          length: Math.floor(Math.random() * 10) + 1,
          isAlive: true,
        },
      })
    )
  );

  console.log('✅ Created bots:', bots.length);

  console.log('🎉 Database seed completed successfully!');
}

main()
  .catch((e) => {
    console.error('❌ Error during seed:', e);
    process.exit(1);
  })
  .finally(async () => {
    await prisma.$disconnect();
  }); 