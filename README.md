# StudyRats

Desafios de estudo para concurseiros, entre amigos. Inspirado no GymRats: cada grupo escolhe o que acompanhar
(questões, acertos, tempo de estudo), todo registro exige foto, e o ranking mostra quem está mais constante.

**Stack:** Vue 3 + TypeScript + Pinia + Vue Router + Axios (PWA) · .NET 10 + EF Core · PostgreSQL

## Rodando localmente

Pré-requisitos: .NET SDK 10, Node 24+, Docker.

```bash
# 1. Banco (Postgres na porta 5433)
docker compose up -d

# 2. API (aplica as migrations ao subir) em http://localhost:5080
cd api
ASPNETCORE_ENVIRONMENT=Development dotnet run --urls http://localhost:5080

# 3. Front em http://localhost:5173 (o Vite faz proxy de /api e /uploads para a API)
cd web
npm install
npm run dev
```

Se o `dotnet` reclamar de 401 ao restaurar pacotes, é algum feed NuGet privado da máquina:
o `nuget.config` deste repositório já restringe a restauração ao nuget.org.

## Compartilhar na rede local (colegas no mesmo Wi-Fi)

O computador que roda o app precisa ficar ligado. Só a porta 5173 é aberta: o front repassa `/api` e
`/uploads` para a API, que continua escutando só em `localhost`.

```bash
docker compose up -d
cd api && ASPNETCORE_ENVIRONMENT=Development dotnet run --urls http://localhost:5080   # terminal 1
cd web && npm run share                                                                 # terminal 2
```

1. Descubra o IP do computador (`ipconfig`, linha "Endereço IPv4" do Wi-Fi). Os colegas abrem `http://<IP>:5173`.
2. O Firewall do Windows bloqueia conexões de entrada. Num PowerShell **como administrador**, libere a porta
   só para a sub-rede local:

   ```powershell
   New-NetFirewallRule -DisplayName "StudyRats 5173" -Direction Inbound -Protocol TCP -LocalPort 5173 -Action Allow -RemoteAddress LocalSubnet
   ```

   Para desfazer: `Remove-NetFirewallRule -DisplayName "StudyRats 5173"`.
3. Em HTTP e sem domínio, o navegador não oferece "instalar como app" (PWA exige HTTPS). O site funciona
   normalmente no navegador do celular.
4. Qualquer pessoa na mesma rede pode abrir o endereço e criar conta. Use só em redes que você confia.

## Regras de negócio

- **Grupo** é a turma permanente: não tem data de prova. **Desafio** é o grupo se preparando para um edital
  específico (nome do edital + início + data da prova), criado só por quem criou o grupo. Os registros de estudo
  pertencem ao grupo; o ranking do desafio é calculado com os registros entre o início e a prova. Um desafio que
  começa no passado já conta o que o grupo estudou desde então. Apagar um desafio não apaga registros.
- Ao criar o grupo: pelo menos uma métrica (questões, acertos, tempo). **Acertos exigem questões.**
- **Semana ativa**: semana (segunda a domingo) com pelo menos N dias estudados; N (1 a 7, padrão 3) é definido no
  grupo. A sequência de semanas ativas não quebra enquanto a semana em andamento ainda pode atingir o mínimo.
- **Bolão simulado** (opcional, por desafio): quem fica abaixo do mínimo de dias numa semana "paga" a multa
  definida na criação (R$ 1 a R$ 1.000 por semana), e o valor arrecadado é dividido entre 2 ou 3 colocados
  (padrão 70/30 ou 60/30/10; o que sobrar vai para o caixa do grupo). O app **só calcula e mostra: nunca
  movimenta dinheiro**. Regras:
  - Só contam semanas inteiras (segunda a domingo) dentro do desafio e depois de a pessoa entrar no bolão.
  - Cada pessoa escolhe entrar. Dá para sair só enquanto nenhuma semana sua foi contabilizada.
  - Quem ganha: ranking de dias ativos do desafio (desempate por tempo e depois por questões), só entre quem
    está no bolão. Empate total divide o prêmio das posições empatadas.
  - As regras não mudam depois que o desafio é criado. Nada é gravado como lançamento: tudo é recalculado dos
    registros de estudo (apagar um registro refaz a conta).
- O ranking mostra dias ativos (padrão, desempate por tempo e depois por questões), semanas ativas, questões,
  % de acerto, tempo e dias seguidos.
- **Perfil** (toque no nome de alguém): estatísticas e calendário com a foto de cada dia estudado, somando todos
  os grupos da pessoa. Ao ver outra pessoa, só aparecem registros de grupos que vocês têm em comum.
- As métricas não podem ser alteradas depois da criação (não existe endpoint de edição).
- **A foto é sempre obrigatória.** O servidor confere os bytes (JPEG/PNG/WebP), não o Content-Type.
- O registro só aceita campos das métricas ativas do grupo; acertos nunca passam do total de questões.
- Registros retroativos: até 3 dias, nunca no futuro.
- "Hoje" é calculado no fuso `America/Sao_Paulo` (configurável em `Clock:TimeZone`).
- Quem não é membro de um grupo recebe 404 (não descobre que ele existe).
- Só o autor apaga o próprio registro.

## Produção: antes de publicar

- Defina `Jwt__Key` (32+ caracteres aleatórios) e `ConnectionStrings__Default` por variável de ambiente.
  A chave que está em `appsettings.Development.json` é só para desenvolvimento.
- Defina `Cors__Origins__0` com o domínio do front, ou sirva front e API no mesmo domínio.
- Fotos ficam em disco (`Photos:Directory`). Em hospedagem com disco efêmero, use um volume persistente
  ou troque `LocalPhotoStorage` por um storage de objetos (a interface `IPhotoStorage` já isola isso).
- Coloque a API atrás de HTTPS (proxy reverso). O token JWT fica no `localStorage`.

## Fora do MVP (v2)

Metas diárias/semanais, edital checklist, revisão espaçada, caderno de erros, simulados.
