# 02 — Distribuição do `ciir` como .NET Tool

## 1. Objetivo

Empacotar o `Ciir.Cli` como uma **.NET tool** publicada no nuget.org, de forma que qualquer pessoa
com o .NET 10 SDK possa executar:

```bash
dotnet tool install --global BlogDoFT.Ciir
ciir ./minha-solution.sln --output ./ciir-output
```

O comportamento do CLI (argumentos, formato de saída, `ciir.schema.json`) **não muda**. Este spec
cobre empacotamento, versionamento, ajustes de código necessários para rodar como tool instalada,
pipeline de release e documentação.

## 2. Escopo

**Dentro:** metadados de pacote, `PackAsTool`, versionamento com GitVersion, `generator.version` do
manifest, validação de ambiente (SDK), splash screen com alertas de pré-requisito, workflow GitHub
de release para nuget.org, smoke test do pacote, atualização de README/CLAUDE.md.

**Fora (YAGNI):** multi-targeting (`net8.0`/`net9.0`), tool específica por RID, single-file/AOT/trim
(incompatíveis com `MSBuildWorkspace`, que carrega assemblies dinamicamente), pacotes NuGet
separados para `Ciir.Core`/`Ciir.Application` etc., símbolos `.snupkg`, GitHub Release com notas
automáticas, o adapter Web API, restore automático dos projetos analisados.

## 3. Decisões

| Tema | Decisão |
|---|---|
| Destino | nuget.org |
| PackageId / comando | `BlogDoFT.Ciir` / `ciir` |
| Pacote | Um único pacote framework-dependent (`tools/net10.0/any/`); só `Ciir.Cli` é empacotável, as demais libs entram como DLLs dentro dele |
| TFM | `net10.0` + `<RollForward>Major</RollForward>` (roda também em runtime mais novo) |
| Versionamento | GitVersion 6 a partir de tags `vX.Y.Z` e Conventional Commits |
| Versão do pacote ≠ `schemaVersion` | A versão do pacote evolui livremente (começa em `0.1.0`); `schemaVersion` continua sendo o contrato CIIR (`1.0`) e não muda por causa da tool |
| `generator.name` no manifest | Continua `ciir-csharp` (identifica o gerador de linguagem, não o pacote) |
| Publicação | GitHub Actions, disparada por tag `v*`, com aprovação via GitHub Environment |
| Splash screen | Exibido em toda execução de análise (não em `--help`/`--version`/erro de parsing); ASCII puro, sem cores/ANSI; stdout; opt-out por `--no-banner` ou `CIIR_NOLOGO` |
| Link do blog | `https://www.blogdoft.com.br/` |

## 4. Empacotamento

### 4.1 `src/Ciir.Cli/Ciir.Cli.csproj`

Adicionar (o `AssemblyName=ciir` já existe):

```xml
<PackAsTool>true</PackAsTool>
<ToolCommandName>ciir</ToolCommandName>
<PackageId>BlogDoFT.Ciir</PackageId>
<IsPackable>true</IsPackable>            <!-- sobrescreve o false de Directory.Build.props -->
<RollForward>Major</RollForward>
<Description>Static analyzer that turns C# code into CIIR (Code Intelligence Intermediate Representation).</Description>
<PackageTags>csharp;roslyn;static-analysis;code-intelligence;ciir;embeddings</PackageTags>
<PackageReadmeFile>README.md</PackageReadmeFile>
```

e `<None Include="..\..\README.md" Pack="true" PackagePath="\" />`.

### 4.2 `Directory.Build.props` (metadados comuns)

`Authors`, `Copyright`, `PackageLicenseExpression` (`GPL-3.0-only`; texto integral no `LICENSE` da raiz), `RepositoryUrl`
(`https://github.com/blogdoft/code-csharp-ciir`), `PackageProjectUrl`, `RepositoryType=git`,
`PublishRepositoryUrl=true`, `EmbedUntrackedSources=true`, e:

```xml
<Version Condition="'$(Version)' == ''">0.0.0-local</Version>   <!-- fallback p/ build local; CI/GitVersion sobrescreve -->
<ContinuousIntegrationBuild Condition="'$(GITHUB_ACTIONS)' == 'true'">true</ContinuousIntegrationBuild>
```

`RepositoryUrl` é explícita porque o `origin` local aponta para o Forgejo (`ssh://…:2222`), que não
deve vazar para o pacote público.

### 4.3 `.gitignore`

Adicionar `artifacts/` (saída de `dotnet pack`).

### 4.4 Conteúdo obrigatório/proibido do `.nupkg`

- **Deve** conter `tools/net10.0/any/DotnetToolSettings.xml`, `ciir.dll`, `ciir.schema.json`
  embutido em `Ciir.Serialization.dll`, e o diretório `BuildHost-netcore/` do
  `Microsoft.CodeAnalysis.Workspaces.MSBuild` (usado pelo `MSBuildWorkspace` out-of-process).
- **Não deve** conter `Microsoft.Build.dll`, `Microsoft.Build.Framework.dll` nem
  `Microsoft.Build.Utilities.Core.dll`: o `MSBuildLocator` carrega o MSBuild do SDK instalado, e uma
  cópia empacotada gera conflito de versão. (`Microsoft.Build.Framework` já usa
  `ExcludeAssets="runtime"` em `Ciir.CSharp.csproj`.)
- `dotnet pack` deve terminar com **zero warnings** (regra do projeto; `TreatWarningsAsErrors` está
  ligado).

## 5. Versionamento (GitVersion)

- `GitVersion.yml` na raiz: prefixo de tag `v`, workflow `GitHubFlow/v1`, branch `main` com
  `label: ci`. **O GitVersion 6 não usa Conventional Commits por padrão** (o padrão são diretivas
  `+semver:`), então o arquivo define explicitamente os regex de bump: `feat` → minor,
  `fix`/`perf` → patch, `!:`/`BREAKING CHANGE:` → major. Não há `next-version`: a fonte é a última
  tag (o repositório já tem `v0.1.0`).
- A versão é calculada por `dotnet dotnet-gitversion` (tool já em `.config/dotnet-tools.json`) e
  passada ao build com `-p:Version=<SemVer>`. **Não** usar `GitVersion.MsBuild`: rodaria em todo
  build/teste local e falharia em clones rasos.
- **Invariantes (validados em clone descartável):** (a) em commit com tag estável `vX.Y.Z`, o SemVer
  é exatamente `X.Y.Z`; (b) em commit sem tag, o SemVer é pré-release (`X.Y.Z-ci.N`) e portanto nunca
  colide com uma versão publicada. Ex.: `v0.1.0` → `fix` → `0.1.1-ci.1` → `feat` → `0.2.0-ci.2`
  → tag `v0.2.0` → `0.2.0` → commit `!` → `1.0.0-ci.1`.
- **Em tag, a tag é a fonte da versão** (o workflow usa o nome da tag sem o `v`, validado como
  SemVer); o GitVersion é usado nos demais builds. Isso evita que o `label: ci` da `main` mascare
  tags pré-release.
- Tags pré-release (`v0.2.0-beta.1`) publicam pacote pré-release.

## 6. Mudanças de código

### 6.1 `generator.version` reflete a versão do pacote

Hoje: `typeof(AnalyzeInputHandler).Assembly.GetName().Version` (`AnalyzeInputHandler.cs:91`), que
perde o sufixo de pré-release. Passar a ler `AssemblyInformationalVersionAttribute` (todos os
assemblies recebem o mesmo `Version` via `Directory.Build.props`), removendo o sufixo `+<sha>` de
build metadata; fallback `0.0.0`. Extrair para um helper pequeno em `Ciir.Application` (ex.:
`GeneratorVersion.Current`) para ser testável. `ciir --version` (nativo do System.CommandLine)
passa a exibir a versão completa.

### 6.2 Validação de ambiente (novo exit code `4`)

Problema: sem SDK, `MsBuildEnvironment.EnsureRegistered()` lança `InvalidOperationException`; o
handler captura isso **por projeto** como falha `project_load` e sai com exit `0` (sem
`--fail-on-error`) — a tool "tem sucesso" sem analisar nada. Solução, mantendo a arquitetura
hexagonal:

- Nova porta `IEnvironmentValidator` em `Ciir.Application/Ports`, retornando `Result`
  (`BlogDoFT.Libs.ResultPattern`, mesmo padrão de `IInputResolver`).
- Implementação `MsBuildEnvironmentValidator` em `Ciir.CSharp/Bootstrap`, encapsulando
  `MsBuildEnvironment.EnsureRegistered()` e traduzindo `InvalidOperationException` em falha com
  mensagem acionável ("No .NET SDK found. Install the .NET 10 SDK: https://dot.net").
- `AnalyzeInputHandler` chama o validador **depois** de resolver a entrada (argumento inválido
  continua sendo exit `2` sem exigir SDK) e **antes** da descoberta de projetos (que já usa MSBuild
  para listar `.sln`). Falha → `AnalysisExitCode.EnvironmentError = 4`, sem analisar nada.
- Registrar a implementação em `Ciir.Cli/Composition/ServiceCollectionExtensions.cs`.
- Mudança **aditiva** (não é breaking); o `01-spec-inicial.md §60` permite códigos extras
  documentados — documentar no README.

### 6.3 Splash screen

Ordem obrigatória da saída em uma execução de análise:

1. **Banner** — nome da ferramenta (`CIIR`) e `BlogDoFT` em ASCII art, com a versão da tool;
2. **Link do blog**;
3. **Alertas de pré-requisito** (os itens do §7);
4. **Saída da ferramenta** (progresso, erros etc.), incluindo a mensagem do exit code `4`, que vem
   depois do splash.

Leiaute (a arte abaixo é o ponto de partida; ASCII puro, ≤ 60 colunas, sem ANSI):

```text
  ____  ___  ___  ____
 / ___||_ _||_ _||  _ \
| |     | |  | | | |_) |
| |___  | |  | | |  _ <
 \____||___||___||_| \_\
  Code Intelligence IR - v0.1.0

 ____   _                 ____          _____  _____
| __ ) | |  ___    __ _  |  _ \   ___  |  ___||_   _|
|  _ \ | | / _ \  / _` | | | | | / _ \ | |_     | |
| |_) || || (_) || (_| | | |_| || (_) ||  _|    | |
|____/ |_| \___/  \__, | |____/  \___/ |_|      |_|
                  |___/

  https://www.blogdoft.com.br/

  Before you start:
   - Requires the .NET 10 SDK (the runtime alone is not enough).
   - Analyzed projects must be restored first (dotnet restore); ciir will not.
   - A global.json in the analyzed repository may select a different SDK.

Discovering projects...
Found 3 project(s).
```

Implementação:

- Nova classe `SplashScreen` em `Ciir.Cli/Presentation` (camada de apresentação: a arquitetura
  mantém escrita em console no CLI; atualizar o comentário de `ConsoleProgressReporter`, que hoje
  diz ser o "único" componente que escreve no `Console`). Duas partes: `Render(version)` — função
  pura que devolve o texto (testável) — e a escrita em `Console.Out`.
- Os textos dos alertas ficam **só** ali; o §7 e o README referenciam o mesmo conteúdo.
- `Program.cs` chama o splash na action do comando, **depois** do parsing e **antes** de
  `AnalyzeInputHandler.ExecuteAsync`. Assim `--help`, `--version` e erros de parsing nunca exibem
  o banner (a saída de `--version` continua parseável por scripts). Um caminho inexistente (exit
  `2`) exibe o banner e depois o erro, na mesma ordem das demais execuções.
- Versão exibida: a mesma do §6.1 (`GeneratorVersion.Current`).
- **Opt-out** (adição deste spec, para não poluir logs de CI/pipelines): opção `--no-banner` e
  variável de ambiente `CIIR_NOLOGO` (`1`/`true`, case-insensitive), no espírito do
  `DOTNET_NOLOGO`. Suprime banner, link **e** alertas; é independente de `--no-progress`.
- Sem cores/ANSI e só caracteres ASCII, para funcionar em consoles Windows e em logs de CI.

### 6.4 O que não muda

`Program.cs` (além da chamada ao splash e da nova opção), demais opções do CLI, diretório de saída
padrão `./ciir-output` (relativo ao cwd de quem invoca a tool), saída de progresso no console,
demais exit codes (0/1/2/3).

## 7. Pré-requisitos de runtime e limitações (documentar e exibir no splash — §6.3)

1. **.NET 10 SDK** instalado (o runtime sozinho não basta: o `MSBuildWorkspace` usa o MSBuild do SDK).
2. Os projetos analisados devem estar **restaurados** (`dotnet restore`); sem `obj/project.assets.json`
   as referências NuGet não resolvem e há relações `unresolved` em excesso. A tool não faz restore.
3. **`global.json` do repositório analisado**: o `MSBuildLocator` pode selecionar um SDK diferente
   do 10. Ver Spike S1 (§11) — o resultado define a versão mínima de SDK suportada e se o validador
   também deve checar a major do MSBuild registrado.

## 8. Pipeline de release — `.github/workflows/release.yml`

Roda **apenas no GitHub** (o Forgejo prioriza `.forgejo/workflows` e só usa `.github/workflows` como
fallback; como o diretório existe, o arquivo é ignorado lá — confirmar).

**Gatilhos:** `push` de tags `v*`; `workflow_dispatch` (dry-run: build+test+pack+smoke, **sem** push).

**Job `build`** (`ubuntu-latest`, `permissions: contents: read`):

1. `actions/checkout` com `fetch-depth: 0` (GitVersion precisa do histórico + tags).
2. `actions/setup-dotnet` com `10.0.x`.
3. Versão: se o ref é tag `vX.Y.Z[-pre]`, usa o nome da tag sem o `v` (falha se não for SemVer
   válido); caso contrário, `dotnet tool restore` + `dotnet dotnet-gitversion /output json` → `SemVer`.
5. `dotnet build -c Release -p:Version=$SemVer` e `dotnet test -c Release --no-build` (warnings = erro).
6. `dotnet pack src/Ciir.Cli -c Release --no-build -p:Version=$SemVer -o artifacts`.
7. Validar conteúdo do `.nupkg` (§4.4) com `unzip -l`.
8. **Smoke test do pacote** (§10.3) e `upload-artifact` do `.nupkg`.

**Job `publish`** (`needs: build`, `if: startsWith(github.ref, 'refs/tags/v')`,
`environment: nuget` com revisor obrigatório):

- Permissões do job: `contents: read` e `id-token: write` (só aqui; o job `build` fica somente com
  `contents: read`).
- `download-artifact`; `NuGet/login@v1` (troca o token OIDC do GitHub por uma API key temporária,
  de 1 hora); e
  `dotnet nuget push artifacts/*.nupkg --source https://api.nuget.org/v3/index.json --api-key "$NUGET_API_KEY" --skip-duplicate`,
  com a chave recebida via variável de ambiente (nunca inline no comando).
- Publicar no nuget.org é irreversível (só dá para *unlist*), por isso o environment com aprovação.

**Autenticação — Trusted Publishing (OIDC):** não há API key de longa duração. Pré-requisitos:

- política de *trusted publishing* no nuget.org para o repositório `blogdoft/code-csharp-ciir`,
  workflow `release.yml` e environment `nuget`;
- secret `NUGET_USER` (o *username* do perfil no nuget.org, não o e-mail) — pode ser secret ou
  variável do repositório.

**Fluxo de release:** criar a tag `vX.Y.Z` no Forgejo → o espelho a leva ao GitHub → o workflow
dispara. O espelho roda uma vez por dia (23:00 UTC, 20h em São Paulo) e também pode ser disparado à
mão (`workflow_dispatch` em `.forgejo/workflows/mirror-to-github.yml`) para levar a tag ao GitHub sem
esperar. Em dias úteis (segunda a sexta) ele só espelha entre 20h e 7h (America/Sao_Paulo), inclusive
nas execuções manuais; aos sábados e domingos essa janela não é verificada.

## 9. Documentação

- `README.md`: substituir "Installation / running the CLI" por instalação como tool
  (`dotnet tool install -g BlogDoFT.Ciir`, `update`, `uninstall`, tool local com
  `dotnet new tool-manifest` + `dotnet tool run ciir`, e `dnx BlogDoFT.Ciir` no .NET 10+ como
  execução avulsa); manter a seção "rodar do código-fonte"; adicionar §7 (pré-requisitos), o exit
  code `4` à tabela de exit codes, a opção `--no-banner` e a variável `CIIR_NOLOGO`, e um exemplo
  da saída com o splash.
- `CLAUDE.md` → "Development commands": comandos de `pack`/instalação local e menção ao workflow de
  release.
- Este spec (`02`) é a fonte para o processo de release (tag → mirror → GitHub → nuget.org).

Comandos de verificação local (a documentar):

```bash
dotnet pack src/Ciir.Cli -c Release -o artifacts
dotnet tool install --tool-path ./artifacts/tool --add-source ./artifacts BlogDoFT.Ciir --prerelease
./artifacts/tool/ciir --version
```

## 10. Testes e critérios de aceite

1. **Unitários (`Ciir.Application.Tests`):** `GeneratorVersion` (remove `+sha`, preserva
   pré-release, fallback `0.0.0`); `AnalyzeInputHandler` com `IEnvironmentValidator` (NSubstitute)
   falhando → exit `4` e `ICodeAnalyzer` nunca chamado; entrada inválida continua exit `2` mesmo com
   validador falhando.
2. **`Ciir.Cli.Tests`** (subprocess, padrão de `CliExitCodeTests`): `--version` → exit `0` e saída
   semver, **sem** banner; `--help` → exit `0` listando as opções (incluindo `--no-banner`), sem
   banner; caminho inexistente → exit `2`, com o banner no stdout e a mensagem de erro no stderr;
   `--no-banner` e `CIIR_NOLOGO=1` → stdout sem banner/link/alertas.
   **`SplashScreen.Render`** (teste unitário, sem subprocess): contém `BlogDoFT` em ASCII, a versão e
   a URL `https://www.blogdoft.com.br/`; os três alertas aparecem **depois** do link, e o link
   **depois** do banner (verificar por índices das ocorrências); só caracteres ASCII.
3. **Smoke test do pacote (CI):** instalar o `.nupkg` local em `--tool-path` temporário; `ciir
   --version` == versão do pacote; `dotnet restore fixtures/BasicSolution` e rodar `ciir
   fixtures/BasicSolution` → exit `0`, quatro artefatos gerados, `manifest.json` com
   `generator.version` == versão do pacote.
4. Conteúdo do pacote conforme §4.4; `dotnet build`/`pack` sem warnings.
5. `dotnet test` da solution continua verde.

## 11. Questões em aberto

- ~~**Licença**~~ — decidida: **GPLv3** (`GPL-3.0-only`, texto integral em `LICENSE`). Se a intenção
  for permitir versões futuras da GPL, trocar para `GPL-3.0-or-later`.
- **Prefixo `BlogDoFT.`:** confirmar que a conta do nuget.org que vai publicar é dona/reservou o
  prefixo (as `BlogDoFT.Libs.*` já existem); caso contrário o primeiro push falha.
- **Spike S1 — SDK/`global.json`:** rodar a tool (com Roslyn 5.9) contra um repo cujo `global.json`
  fixa SDK 8, numa máquina com vários SDKs instalados, para ver qual MSBuild o
  `MSBuildLocator.RegisterDefaults()` escolhe e se a análise funciona. Define a versão mínima de SDK
  e se o validador (§6.2) deve exigir MSBuild ≥ 10.
  **Resultado:** um projeto `net8.0` com `global.json` fixando o SDK `8.0.423` (SDKs 8, 9 e 10
  instalados) foi analisado com sucesso pela tool (exit `0`, 6 entidades, sem erros). Portanto o
  validador **não** exige MSBuild ≥ 10; o alerta do splash continua apenas informativo.
- **Exit code `4`:** verificado com um `DOTNET_ROOT` que só tem o runtime (sem SDK): a tool sai com
  `4`, sem criar o diretório de saída, com a mensagem acionável.
- ~~**Trusted Publishing** vs. API key~~ — decidido: Trusted Publishing (§8). Confirmar no primeiro
  release que o nuget.org aceita criar o ID `BlogDoFT.Ciir` (ainda inexistente) por esse caminho; se
  não aceitar, publicar a primeira versão manualmente e usar o workflow a partir da segunda.

## 12. Ordem de implementação (Conventional Commits)

1. `build(cli): package Ciir.Cli as the BlogDoFT.Ciir .NET tool` (§4, `.gitignore`)
2. `fix(manifest): report the package version as generator.version` (§6.1 + testes)
3. `feat(app): fail fast with exit code 4 when no MSBuild SDK is available` (§6.2 + testes)
4. `feat(cli): show a BlogDoFT splash screen with prerequisite notices` (§6.3 + testes; `--no-banner`/`CIIR_NOLOGO`)
5. `build: derive package version with GitVersion` (§5)
6. `ci: add GitHub release workflow publishing to nuget.org` (§8, incl. `workflow_dispatch` no mirror)
7. `docs: document .NET tool installation and prerequisites` (§9)
