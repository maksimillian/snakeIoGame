## Socket.IO

- Implementar WebSocketGateway com namespace `chat` chamado ChatGateway
- Implementar logica de conexão e desconexão de perfis
- Gravar o `profileId` no client na conexão
- Gravar o `profileId` e o `socketId` em um `Map`
- Remover o `profileId` e o `socketId` do `Map` na desconexão
- Toda conexão deve vir acompanhada da query `profile` com o valor do seu `profileId` (ex: `?profile=0e396dc6-525a-4cc2-a3ad-0d0bdbac3f90`)
- Futuramente será implementado JWT para autenticação e identificação do perfil

## Criar conversa
- A conversa é ponto inicial que faz a conexão entre os perfis para enviar mensagem
- A conversa é criada com o perfil que iniciou a conversa e o perfil destinario

- Deve ser criado um metodo no ChatGateway com o socket event `conversation:create` que identifica o profileId pelo socket e recebe com o `profileId` do destinatario como `participantId`
- Deve ser verificado se o profileId e o participantId são diferentes e existem
- Verificar se já existe uma conversa entre os dois perfis, se existir, retornar `{ conversationId: string, lastMessage: string, lastMessageAt: string }`
- Caso não exista, criar uma conversa e relacionar historicos individual.
- Deve retornar `{ conversationId: string }`

## Enviar mensagem

## Recuperar conversas

## Recuperar historico de uma conversa com paginação

## Recuperar mensagens não recebidas quando logar

## Atualizar visualização enquanto chat aberto

## Atualizar ultima vez visto

## Deletar historico mas manter conversa

## Deletar conversa e historico

## Silenciar conversa?

## Bloquear conversa?

## Ver conversas bloqueadas

## Configurações?
