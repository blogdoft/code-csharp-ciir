# Captura de `appsettings*.json` e arquivos YAML no CIIR

## 1. Objetivo

Incluir, na construção do CIIR, dois tipos de artefato que hoje não são analisados: arquivos
`appsettings*.json` e arquivos `*.yaml`/`*.yml` existentes no repositório analisado. Este documento
formaliza as decisões tomadas para essa feature, complementando (e não substituindo) o kind set já
reservado em [`01-spec-inicial.md`, seção 16](./01-spec-inicial.md).

O kind set original já reservava `file`, `configuration` e `configuration_key` para uso futuro,
justamente para este tipo de artefato — esta feature implementa esse ponto de extensão, não cria um
novo.

## 2. Escopo

- **`appsettings*.json`**: qualquer arquivo cujo nome comece com `appsettings` (case-insensitive) e
  tenha extensão `.json`, em qualquer diretório sob a raiz analisada.
- **`*.yaml`/`*.yml`**: qualquer arquivo com essas extensões, em qualquer diretório sob a raiz
  analisada — sem filtro por nome/propósito (workflows de CI, docker-compose, manifests Kubernetes,
  etc. entram todos igualmente).
- Ambos são descobertos **independentemente da estrutura de projetos/solução** — um arquivo na raiz
  do repositório é capturado mesmo que não pertença a nenhum `.csproj`.
- São ignorados os mesmos diretórios já ignorados na descoberta de projetos: `bin`, `obj`, `.git`,
  `.vs`, e adicionalmente `node_modules`.

## 3. Profundidade de captura

| Artefato | Profundidade | Kind(s) emitido(s) |
|---|---|---|
| `appsettings*.json` | Estrutural — cada chave (folha ou container) vira um documento | `configuration` (o arquivo) + `configuration_key` (cada chave) |
| `*.yaml` / `*.yml` | Apenas metadados do arquivo — sem parsing estrutural | `file` |

**Decisão**: YAML não é parseado estruturalmente porque seu propósito varia demais entre
repositórios (CI, docker-compose, Kubernetes, ...) para caber em um único schema de chaves — ao
contrário de `appsettings.json`, cujo formato (chave:valor, aninhável) é uniforme e cujo consumo
real (`Microsoft.Extensions.Configuration`) já define uma convenção de achatamento bem conhecida.

## 4. Achatamento de chaves (`appsettings*.json`)

Chaves são achatadas com `:` como separador, seguindo a mesma convenção do
`Microsoft.Extensions.Configuration`:

```text
ConnectionStrings:Default
Feature:Nested:Deep
AllowedHosts:0
AllowedHosts:1
```

Um documento `configuration_key` é emitido para **todo nó da árvore**, folha ou container
(objeto/array) — não apenas para folhas. Isso garante que os `valueType`s `object` e `array` também
apareçam no CIIR (contêineres não são "achatados para dentro" de suas folhas).

Para determinismo, as propriedades de um objeto JSON são ordenadas alfabeticamente
(`StringComparer.Ordinal`) antes de recursão, independente da ordem em que aparecem no texto fonte
— seguindo o mesmo princípio de determinismo já descrito em `docs/ciir-specification.md`.

## 5. Valores nunca são capturados

**Regra inegociável**: `configurationKey.valueType` registra apenas o tipo JSON do valor
(`string`, `number`, `boolean`, `array`, `object`, `null`) — nunca o valor em si.

Motivo: `appsettings*.json` frequentemente contém segredos (connection strings, chaves de API,
senhas), e o CIIR gerado pode ser indexado ou usado para embeddings por ferramentas downstream. Não
há heurística de redaction por nome de chave — a regra é absoluta e se aplica a toda chave,
independente do nome.

## 6. Identidade

- `configuration` / `file`: `canonicalName` = caminho relativo à raiz de análise, normalizado com
  `/` (mesma convenção de `source.path`).
- `configuration_key`: `canonicalName` = `{caminhoRelativo}#{caminhoDaChave}` — o `#` evita
  ambiguidade com o `:` já usado dentro do caminho da chave, e garante que a mesma chave em arquivos
  irmãos (`appsettings.json` vs. `appsettings.Development.json`) produza IDs diferentes.
- Containment (`configuration_key` → `configuration` pai) é expresso via `symbol.container`, do
  mesmo jeito que tipo/membro em C# — nunca por uma relação `contains` (que o gerador C# também não
  emite, por ser redundante com `symbol.container`).
- `language`: `"json"` para `configuration`/`configuration_key`, `"yaml"` para `file`.
- `project`: nome lógico fixo `"Configuration"` — nenhum arquivo descoberto está necessariamente
  ligado a um único `.csproj`.

## 7. Arquitetura

Implementado como um novo adapter, `src/Ciir.Configuration`, implementando `ICodeAnalyzer` — o
mesmo padrão descrito no `CLAUDE.md` para um novo gerador de linguagem/fonte, análogo a
`Ciir.CSharp`. Não depende de Roslyn.

Para permitir múltiplos `ICodeAnalyzer` coexistindo, `AnalyzeInputHandler` passou a aceitar
`IEnumerable<ICodeAnalyzer>` e a despachar cada arquivo descoberto para o primeiro analisador cujo
`CanAnalyze` o reivindica — mecanismo que já existia na porta (`ICodeAnalyzer.CanAnalyze`) mas não
era usado antes desta feature.

Nenhuma dependência NuGet nova foi necessária: `System.Text.Json` (já presente no SDK) é suficiente
para o parsing de `appsettings*.json`; YAML não precisa de parser algum, já que seu conteúdo nunca é
interpretado.

## 8. Não-objetivos

- Parsing estrutural de YAML (ver seção 3).
- Captura do valor de qualquer chave de configuração (ver seção 5).
- Redaction seletiva por nome de chave — a regra de "nunca capturar valor" é absoluta, não uma
  heurística.
- Vínculo de `appsettings*.json`/YAML a um `.csproj` específico — são tratados como artefatos do
  repositório, não do projeto.
