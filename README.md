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

## Regras de negócio

- Ao criar o grupo: pelo menos uma métrica (questões, acertos, tempo). **Acertos exigem questões.**
- As métricas não podem ser alteradas depois da criação (não existe endpoint de edição).
- **A foto é sempre obrigatória.** O servidor confere os bytes (JPEG/PNG/WebP), não o Content-Type.
- O registro só aceita campos das métricas ativas do grupo; acertos nunca passam do total de questões.
- Registros retroativos: até 3 dias, dentro do período do desafio, nunca no futuro.
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
