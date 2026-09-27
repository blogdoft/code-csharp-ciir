# 03 — Envio do `ciir.jsonl` para o code-ciir-indexer (`--send`)

## 1. Objetivo

Permitir que uma única invocação do `ciir` analise o código **e** envie o `ciir.jsonl` gerado ao
`code-ciir-indexer`, pensado para uso em pipeline (CI):

```bash
ciir ./minha-solution.sln --send https://blogdoft.home.arpa/code-brain \
     --projectId 3f2b1c0e-... --clientId ciir-pipeline --clientSecret "$CIIR_SECRET"
```

Sem `--send`, o comportamento do CLI **não muda**.

## 2. Contrato do indexer (verificado em `code-ciir-indexer`, spec 04/05)

| Item | Valor |
|---|---|
| Endpoint de upload | `POST {baseUrl}/api/indexer/ciir-uploads` |
| Corpo | `multipart/form-data`: campo texto `projectId` **antes** do arquivo `ciirFile` (a ordem é obrigatória: o `projectId` é validado antes de gravar qualquer byte) |
| Nome do arquivo | precisa terminar em `.jsonl` (`ciir.jsonl`) |
| Sucesso | `202 Accepted` + `{ "uploadId": "<guid>", "status": "pending" }` |
| Erros | `400`/`413` (`application/problem+json`), `401` (token ausente/ inválido, quando o Keycloak está ligado), `404` (projeto desconhecido, sem corpo), `429` |
| Autenticação | Opcional (`Keycloak:Enabled`). Ligada: `Authorization: Bearer <jwt>` do realm |
| Token (gateway) | `POST {baseUrl}/api/indexer/auth/token` (anônimo), corpo JSON `{ "clientId", "clientSecret" }` → `200 { "accessToken", "tokenType", "expiresIn" }`. O indexer conversa com o Keycloak; o `ciir` não conhece o Keycloak. Erros: `400`, `401` (credenciais recusadas), `404` (autenticação desligada no indexer), `502` (Keycloak indisponível). Contrato definido em `code-ciir-indexer/.specs/06-token-gateway.md` |

`{baseUrl}` pode ter prefixo de caminho (ingress do cluster: `https://blogdoft.home.arpa/code-brain`).

## 3. Interface de linha de comando

| Opção | Alias | Significado |
|---|---|---|
| `--send [<baseUrl>]` | `-s` | Envia o `ciir.jsonl` ao indexer ao final da análise. O valor é opcional |
| `--projectId <guid>` | `-pi` | Id do projeto **já registrado** no indexer. Obrigatório com `--send` |
| `--clientId <id>` | `-ci` | `client_id` no Keycloak (client credentials) |
| `--clientSecret <secret>` | `-cs` | Secret do client |
| `--token <jwt>` | `-t` | Access token usado como Bearer |
| `--insecure` | — | Não valida o certificado TLS/SSL do indexer (autoassinado, CA privada, hostname divergente). Vale para o pedido de token e para o upload. Imprime um aviso em stderr. Uso restrito a redes confiáveis; sem a opção, o certificado é sempre validado |

> **Decisão registrada:** o pedido original não citava `--projectId`, mas o indexer rejeita o upload
> sem ele; por isso a opção foi acrescentada (nome em camelCase, igual às opções de conexão pedidas).

### 3.1 Resolução da `baseUrl`

1. Valor de `--send`, se informado e não vazio;
2. senão, a variável de ambiente `CIIR_BASE_URL`;
3. senão, erro: a variável não está definida e por isso a `<base-url>` precisa ser fornecida.

O valor precisa ser uma URL absoluta `http`/`https`; caso contrário, erro informando a URL inválida.
Como `--send` aceita valor opcional, o `<path>` deve vir **antes** de `-s` (`ciir ./src -s`); em
`ciir -s ./src` o `./src` seria lido como a base URL.

### 3.2 Autenticação

Precedência (a primeira que se aplica vence):

1. `--token` informado → `Authorization: Bearer <token>`;
2. `--clientId` **e** `--clientSecret` informados → o `ciir` obtém um token via
   `client_credentials` (§4.2) e o usa como Bearer;
3. caso contrário → envio **sem** autenticação.

Só um de `--clientId`/`--clientSecret` (sem `--token`): é ignorado, com um aviso em stderr
(“ambos são necessários”), e o envio segue sem autenticação.

### 3.3 Validação e códigos de saída

Toda a validação de `--send` (baseUrl, `--projectId`) ocorre **antes** da análise, para falhar
rápido em pipeline, com exit code `2` (`InvalidInput`). Novo código:

| Código | Significado |
|---|---|
| `5` | `UploadFailure`: a análise terminou, mas o envio ao indexer falhou (rede, token, resposta ≠ 2xx) |

O envio só ocorre se a análise terminou com sucesso (não há envio com `--fail-on-error` violado nem
com falha ao gravar a saída). Em sucesso, o `ciir` imprime em stdout o `uploadId` e o `status`
devolvidos; o `ciir` **não** aguarda o processamento (não faz polling de
`GET /api/indexer/ciir-uploads/{id}`).

## 4. Arquitetura (hexagonal)

| Onde | O quê |
|---|---|
| `Ciir.Application/Model` | `SendOptions` (`BaseUrl`, `ProjectId`, `Credentials`), `IndexerCredentials` (`BearerTokenCredentials`, `ClientCredentials`), `CiirUploadReceipt`; `AnalysisOptions.Send`, `AnalysisResult.Upload`; `AnalysisExitCode.UploadFailure = 5` |
| `Ciir.Application/Ports` | `ICiirUploader` (porta dirigida) |
| `Ciir.Application/UseCases` | `AnalyzeInputHandler` envia depois de gravar os artefatos, se `Options.Send` não for nulo e o resultado for sucesso |
| `src/Ciir.Indexer.Client` (novo) | Adapter HTTP: `HttpCiirUploader : ICiirUploader`. Sem Roslyn. Depende só de `Ciir.Application` |
| `Ciir.Cli` | Opções, resolução/validação (`SendOptionsFactory`), registro no composition root, saída do recibo |

### 4.1 Upload

`POST` multipart (`projectId`, depois `ciirFile` = `ciir.jsonl`, transmitido por stream — o arquivo
pode ter centenas de MB), com o Bearer quando houver. `HttpClient.Timeout` de 10 minutos. `2xx` →
recibo; demais → falha com o status e, se houver, o `detail` do problem+json. Mensagens nunca
incluem o token nem a secret.

### 4.2 Token via client credentials (gateway do indexer)

`POST {baseUrl}/api/indexer/auth/token` com `{ "clientId", "clientSecret" }` → `accessToken`, usado
como Bearer no upload. O `ciir` **não** sabe onde nem como o token é emitido (sem descoberta de
OpenAPI, sem URL de Keycloak). Falhas: `401` → credenciais recusadas; `404` → o indexer não tem
token endpoint (autenticação desligada: omitir as credenciais ou usar `--token`); `502` → indexer
sem acesso ao provedor de identidade; resposta sem `accessToken` ou rede → falha. Mensagens nunca
incluem a secret.

Pré-requisito do lado do Keycloak: o client informado precisa ser **confidencial**, com *Service
accounts enabled* (o indexer só faz o grant `client_credentials`).

## 5. Fora de escopo (YAGNI)

Polling do status do upload; `POST /ciir-uploads/register` (arquivos enormes via Garage); cache/refresh
de token; variáveis de ambiente para `clientId`/`clientSecret`/`token`/
`projectId` (só `CIIR_BASE_URL`, como pedido); criar o projeto no indexer.

> **Revisão:** a primeira versão descobria o token endpoint do Keycloak pelo documento OpenAPI do
> indexer. Substituída pelo gateway `POST /api/indexer/auth/token` (spec 06 do indexer), para o
> `ciir` não depender de detalhes de emissão do token.

## 6. Plano de execução (concluído)

1. [x] Spec.
2. `Ciir.Application`: modelos, porta, exit code `5`, integração no `AnalyzeInputHandler` + testes.
3. `Ciir.Indexer.Client` + `Ciir.Indexer.Client.Tests` (handler HTTP falso), incluídos na `.slnx`.
4. `Ciir.Cli`: opções, `SendOptionsFactory`, DI, saída; testes unitários e de subprocesso.
5. Documentação: README (uso, opções, exit code, arquitetura/testes), `CLAUDE.md` (tabela de projetos).
6. `dotnet format` → `dotnet build` (zero warnings) → `dotnet test`; smoke test do CLI contra um
   servidor local falso do indexer.
