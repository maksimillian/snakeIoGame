<<<<<<< HEAD
# snakeIoGame
=======
# Snake.io Multiplayer Game

A multiplayer Snake.io game built with Unity WebGL, NestJS, Socket.IO, and React.

## Architecture

```
Unity (WebGL)     ⇄   Socket.IO    ⇄   NestJS API   ⇄   PostgreSQL via Prisma
     ▲                                            │
     └──── React "portal" website (leaderboard, landing page, etc.)
```

## Quick Start

1. Clone this repository:
   ```bash
   git clone https://github.com/yourusername/snakeio.git
   cd snakeio
   ```

2. Fork/clone the template repositories into their respective folders:
   - `client-unity/`: [SocketIOUnity](https://github.com/itisnajim/SocketIOUnity)
   - `server/`: [chat-api](https://github.com/erickmarx/chat-api)
   - `db/`: [prisma-docker](https://github.com/grdnmsz/prisma-docker)
   - `frontend/`: [docker-react](https://github.com/sublime-railway/docker-react)
   - `infra/`: [datadog-agent-railway-starter](https://github.com/railwayapp-templates/datadog-agent-railway-starter)

3. Start the development environment:
   ```bash
   docker compose up
   ```

4. Access the services:
   - Game: http://localhost:8080
   - Web Portal: http://localhost:3001
   - API: http://localhost:3000
   - Database: localhost:5432

## Development

Each module has its own MCP context for Cursor IDE integration:

- `game-client.mcp.json`: Unity WebGL client
- `server-api.mcp.json`: NestJS + Socket.IO server
- `database.mcp.json`: PostgreSQL + Prisma
- `web-frontend.mcp.json`: React portal
- `infra.mcp.json`: Docker + Railway config

Open the project in Cursor IDE and use ⌥+Enter for AI assistance that understands both local and global context.

## License

MIT 
>>>>>>> 597e1e4 (Save work before rebase)
