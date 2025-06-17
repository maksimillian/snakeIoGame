<a name="readme-top"></a>

<!-- PROJECT LOGO -->
<br />
<div align="center">
  <a href="https://raw.githubusercontent.com/othneildrew/Best-README-Template/master/images/logo.png">
    <img src="https://raw.githubusercontent.com/othneildrew/Best-README-Template/master/images/logo.png" alt="Logo" width="80" height="80">
  </a>

<h3 align="center">[WIP] Chat API</h3>

  <p align="center">
  [WIP] An awesome chat API for your project
    <br />
    <a href="https://github.com/erickmarx/chat-api"><strong>Explore the docs »</strong></a>
    <br />
    <a href="https://chat-api-qjfduxo26q-uc.a.run.app">View Demo</a>
  </p>
</div>

<!-- TABLE OF CONTENTS -->
<details>
  <summary>Table of Contents</summary>
  <ol>
    <li>
      <a href="#about-the-project">About The Project</a>
      <ul>
        <li><a href="#built-with">Built With</a></li>
      </ul>
    </li>
    <li>
      <a href="#getting-started">Getting Started</a>
      <ul>
        <li><a href="#installation">Installation</a></li>
      </ul>
    </li>
    <li><a href="#usage">Usage</a></li>
    <li><a href="#roadmap">Roadmap</a></li>
    <li><a href="#license">License</a></li>
    <li><a href="#contact">Contact</a></li>
  </ol>
</details>

<!-- ABOUT THE PROJECT -->

## About The Project

<!-- [![Product Name Screen Shot][product-screenshot]](https://example.com) -->

This project is a chat API that can be used in any project that needs a chat system. It is built with NestJS, Prisma, PostgreSQL, and SocketIO. It is hosted on Google Cloud Run and uses Google Cloud SQL as a database.

### Built With

- [![Typescript][Typescript]][Typescript-url]
- [![NestJS][NestJS]][NestJS-url]
- [![SocketIO][SocketIO]][SocketIO-url]
- [![Prisma][Prisma]][Prisma-url]
- [![PostgreSQL][PostgreSQL]][PostgreSQL-url]
- [![GCP Cloud Run][GCP Cloud Run]][GCPCloudRun-url]
- [![GCP Cloud SQL][GCP Cloud SQL]][GCP-Cloud-SQL-url]

<!-- GETTING STARTED -->

## Getting Started

### Installation

1. 1. Clone the repo
   ```sh
   git clone https://github.com/erickmarx/chat-api.git
   ```
2. Configure the environment variables copying and renaming `.env.example` to `.env`
   ```js
   PORT=3000
   POSTGRES_PORT=
   POSTGRES_USER=
   POSTGRES_PASSWORD=
   POSTGRES_URL=
   ```
3. Install PNPM packages
   ```sh
   pnpm install
   ```
4. Generate Prisma client
   ```sh
   pnpm exec prisma generate
   ```
5. Seed the database
   ```sh
   pnpm exec prisma db seed -- --environment staging
   ```

<!-- USAGE EXAMPLES -->

## Usage

1. Start the aplication

```sh
  pnpm run build && pnpm run start
```

<!-- ROADMAP -->

## Roadmap

- [ ] Documention for SocketIO events
- [x] Endpoint for create conversation
- [x] Endpoint for send a message to conversation
- [x] Endpoint for get history from a conversation
- [x] Endpoint for get all conversations from a profile
- [x] Recover unreceived messages when logging in
- [x] Endpoint for update visualization when the chat is opened
- [x] Update last seen when connected/not connected
- [x] Endpoint for delete only history, not conversation
- [x] Endpoint for delete conversation with history
- [x] Endpoint for configure chat settings
- [ ] Endpoint for block conversations
- [ ] Endpoint for see blocked conversations

<!-- LICENSE -->

## License

Distributed under the MIT License. See `LICENSE.txt` for more information.

<!-- CONTACT -->

## Contact

Erick Marx - [Linkedin](https://linkedin.com/in/erickmarx) - erickmarx5@gmail.com

Project Link: [https://github.com/erickmarx/chat-api](https://github.com/erickmarx/chat-api)

[product-screenshot]: images/screenshot.png
[Typescript]: https://shields.io/badge/TypeScript-3178C6?style=for-the-badge&logo=TypeScript&logoColor=FFF&
[Typescript-url]: https://www.typescriptlang.org/
[NestJS]: https://img.shields.io/badge/nestjs-E0234E?style=for-the-badge&logo=nestjs&logoColor=white
[NestJS-url]: https://www.nestjs.com/
[GCP Cloud Run]: https://shields.io/badge/GCP-3178C6?style=for-the-badge&logo=googlecloud&logoColor=FFF
[GCPCloudRun-url]: https://cloud.google.com/run?hl=pt-BR
[GCP Cloud SQL]: https://shields.io/badge/GCP-3178C6?style=for-the-badge&logo=googlecloud&logoColor=FFF
[GCP-Cloud-SQL-url]: https://cloud.google.com/sql?hl=pt-BR
[SocketIO]: https://img.shields.io/badge/Socket.io-010101?style=for-the-badge&&logo=Socket.io&logoColor=white
[SocketIO-url]: https://socket.io/pt-br/docs/v4/
[Prisma]: https://img.shields.io/badge/Prisma-3982CE?style=for-the-badge&logo=Prisma&logoColor=white
[Prisma-url]: https://www.prisma.io/
[PostgreSQL]: https://img.shields.io/badge/postgresql-4169e1?style=for-the-badge&logo=postgresql&logoColor=white
[PostgreSQL-url]: https://www.postgresql.org/
