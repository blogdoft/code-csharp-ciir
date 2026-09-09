# C# Code Intelligence IR Generator

## 1. Objetivo

Criar uma aplicação .NET capaz de analisar estaticamente código-fonte C# e gerar uma representação intermediária padronizada denominada **Code Intelligence Intermediate Representation — CIIR**.

O CIIR deverá ser independente da linguagem de programação e servirá como contrato intermediário para futuros processos, incluindo:

* geração de embeddings;
* indexação vetorial;
* construção de grafos de dependência;
* construção de call graphs;
* graph expansion;
* descoberta de fluxos de execução;
* análise arquitetural;
* análise de impacto;
* investigação de código;
* extração futura de regras de negócio;
* geração de documentação;
* construção de ferramentas de Code Intelligence.

A primeira implementação deverá analisar **C# utilizando Roslyn**, mas a arquitetura não poderá depender de C# fora do projeto responsável especificamente pela análise C#.

O CIIR é um contrato independente do analisador.

A arquitetura deverá permitir futuramente implementações como:

```text
C# / Roslyn ─────────────┐
Java / JDT ──────────────┤
TypeScript ──────────────┤
Python ──────────────────┤
SQL ─────────────────────┤
                         ▼
                       CIIR
                         │
              ┌──────────┼──────────┐
              ▼          ▼          ▼
          Embeddings    Graph      Analysis
```

---

# 2. Princípios arquiteturais

A implementação deverá seguir:

* SOLID;
* DRY;
* KISS;
* YAGNI;
* Separation of Concerns;
* Dependency Inversion;
* Composition over inheritance;
* baixo acoplamento;
* alta coesão;
* programação orientada a contratos;
* testabilidade;
* processamento incremental/streaming sempre que possível.

Evitar abstrações sem necessidade concreta.

Não criar interfaces apenas para satisfazer artificialmente padrões de arquitetura.

Interfaces deverão existir principalmente quando:

* houver uma fronteira arquitetural;
* houver necessidade real de substituição;
* houver mais de uma implementação plausível;
* forem necessárias para Dependency Inversion;
* facilitarem testes de componentes externos.

O código deverá seguir as recomendações de qualidade do Sonar, incluindo:

* nenhum código morto;
* nenhum warning relevante;
* métodos pequenos e coesos;
* complexidade cognitiva controlada;
* tratamento correto de recursos descartáveis;
* tratamento apropriado de exceções;
* ausência de duplicação significativa;
* ausência de secrets hardcoded;
* uso apropriado de `CancellationToken`;
* operações assíncronas quando existir I/O;
* argumentos públicos validados;
* nullable reference types habilitados;
* documentação XML para APIs públicas relevantes.

Não suprimir warnings ou regras do Sonar sem justificativa técnica documentada.

---

# 3. Requisito fundamental de desacoplamento

O CLI é **apenas uma forma de iniciar uma análise**.

Nenhuma regra de análise deverá existir no projeto CLI.

A aplicação deverá ser organizada para permitir no futuro:

```text
CLI ───────────┐
REST API ──────┤
Worker ────────┤
Queue Consumer ┤
Kubernetes Job ┤
Git Webhook ───┤
               ▼
       Analysis Application
               │
               ▼
           CIIR Engine
```

O componente principal deverá poder ser chamado programaticamente sem qualquer dependência do CLI.

---

# 4. Estrutura inicial da Solution

Criar uma solution semelhante a:

```text
src/
  Ciir.Core/
  Ciir.Application/
  Ciir.CSharp/
  Ciir.Serialization/
  Ciir.Cli/

tests/
  Ciir.Core.Tests/
  Ciir.Application.Tests/
  Ciir.CSharp.Tests/
  Ciir.Serialization.Tests/
  Ciir.Cli.Tests/

schemas/
```

Os nomes podem ser levemente adaptados caso exista motivo técnico relevante, mas a separação arquitetural deve ser preservada.

---

# 5. Responsabilidade dos projetos

## 5.1 `Ciir.Core`

Não deverá possuir dependência de Roslyn, CLI, filesystem concreto ou infraestrutura.

Responsável por definir o modelo CIIR.

Deverá conter:

* entidades do CIIR;
* value objects;
* enums;
* contratos principais;
* regras de identidade;
* tipos para source locations;
* relações;
* documentação;
* control flow metadata;
* condições;
* metadados;
* constantes da especificação.

Exemplos:

```text
CiirDocument
CiirSymbol
CiirSourceLocation
CiirRelation
CiirRelationTarget
CiirDocumentation
CiirParameter
CiirCondition
CiirControlFlow
CiirComment
```

---

## 5.2 `Ciir.Application`

Responsável por orchestration e casos de uso.

Exemplos:

```text
AnalyzeInputCommand
AnalyzeInputHandler
IInputResolver
ICodeAnalyzer
ICiirWriter
IAnalysisReporter
```

Esse projeto deverá decidir:

* o que deve ser analisado;
* em que ordem;
* como múltiplos projetos são processados;
* quando iniciar e finalizar writers;
* geração do relatório final;
* propagação de cancelamento.

Não deverá conhecer detalhes de Roslyn.

---

## 5.3 `Ciir.CSharp`

Responsável exclusivamente pela implementação de análise C#.

Deverá utilizar Roslyn.

Pode depender, conforme necessário, de:

```text
Microsoft.CodeAnalysis
Microsoft.CodeAnalysis.CSharp
Microsoft.CodeAnalysis.Workspaces.MSBuild
```

Responsabilidades:

* carregar solutions;
* carregar projects;
* obter compilations;
* analisar syntax trees;
* utilizar semantic models;
* resolver símbolos;
* extrair documentação;
* extrair relações;
* extrair condições;
* calcular métricas básicas;
* produzir objetos CIIR.

Somente esse projeto deverá conhecer:

```text
SyntaxNode
SyntaxTree
SemanticModel
ISymbol
IMethodSymbol
INamedTypeSymbol
Compilation
MSBuildWorkspace
```

Nenhum desses tipos poderá aparecer nas APIs públicas do Core ou Application.

---

## 5.4 `Ciir.Serialization`

Responsável por:

* serialização JSON;
* escrita JSONL;
* manifest;
* JSON Schema;
* hashing relacionado ao artifact;
* compatibilidade do formato.

Não deverá depender de Roslyn.

---

## 5.5 `Ciir.Cli`

Responsável exclusivamente pela interface de linha de comando.

Deverá:

1. interpretar argumentos;
2. validar argumentos básicos;
3. construir/configurar dependency injection;
4. chamar `Ciir.Application`;
5. apresentar progresso;
6. apresentar erros;
7. retornar exit codes adequados.

Não deverá:

* abrir `.csproj`;
* utilizar Roslyn;
* percorrer syntax tree;
* gerar diretamente CIIR;
* implementar regras semânticas;
* montar `embeddingText`.

---

# 6. Uso do CLI

A interface principal deverá ser:

```bash
ciir <path>
```

Exemplos:

```bash
ciir ./MyApplication.sln
```

```bash
ciir ./src/MyProject/MyProject.csproj
```

```bash
ciir ./src
```

Opções mínimas:

```text
--output <path>
--verbose
--no-progress
--include-source
--fail-on-error
```

Exemplo:

```bash
ciir ./src \
  --output ./artifacts/ciir \
  --verbose
```

O nome final do executável poderá ser configurado pelo projeto, mas deverá ser curto e adequado a automação.

---

# 7. Tipos de entrada

O argumento `<path>` poderá apontar para:

### Solution

```text
*.sln
*.slnx
```

Nesse caso devem ser analisados todos os projetos C# pertencentes à solution.

---

### Project

```text
*.csproj
```

Nesse caso deverá ser analisado o projeto informado.

---

### Diretório

Exemplo:

```bash
ciir /repositories/legacy-system
```

O sistema deverá percorrer recursivamente o diretório procurando projetos C#.

Arquivos encontrados em:

```text
bin/
obj/
.git/
.vs/
```

não deverão ser usados para descoberta.

O sistema deverá localizar:

```text
*.sln
*.slnx
*.csproj
```

A descoberta deverá evitar análise duplicada.

Se uma solution encontrada já referencia determinado projeto, esse projeto não poderá ser analisado novamente apenas porque seu `.csproj` também foi encontrado durante a varredura.

Deverá ser construído internamente um conjunto único de projetos a analisar.

A identidade do projeto deverá ser baseada preferencialmente no caminho físico normalizado do `.csproj`.

---

# 8. Tratamento de múltiplas solutions

Uma pasta poderá conter:

```text
Root/
  Application.sln

  services/
    Billing/
      Billing.sln

    Payment/
      Payment.csproj
```

Todos os projetos C# únicos encontrados dentro da árvore deverão ser analisados.

Projetos repetidos entre solutions deverão ser analisados apenas uma vez.

---

# 9. Projetos que não puderem ser carregados

Uma falha ao carregar um projeto não deverá necessariamente abortar toda a execução.

O comportamento padrão deverá ser:

```text
registrar erro
continuar próximos projetos
registrar problema no relatório
```

Com:

```text
--fail-on-error
```

a aplicação deverá terminar com exit code diferente de zero quando qualquer projeto não puder ser analisado corretamente.

Falhas inesperadas não deverão ser silenciosamente ignoradas.

---

# 10. Output

O diretório de output deverá conter no mínimo:

```text
ciir-output/
  ciir.jsonl
  ciir.schema.json
  manifest.json
  analysis-report.json
```

---

# 11. `ciir.jsonl`

Deverá utilizar JSON Lines.

Cada linha representa exatamente uma entidade CIIR.

Exemplo:

```text
{"schemaVersion":"1.0",...}
{"schemaVersion":"1.0",...}
{"schemaVersion":"1.0",...}
```

Nunca produzir:

```json
[
  {...},
  {...}
]
```

para o arquivo principal.

O objetivo é permitir streaming de arquivos com milhões de registros.

---

# 12. Requisitos de memória

O sistema deverá ser capaz de trabalhar com bases extensas.

Não carregar toda a CIIR em memória.

Preferir:

```text
Project
  ↓
Document
  ↓
Semantic analysis
  ↓
CIIR record
  ↓
JSONL writer
```

Os registros deverão ser escritos progressivamente.

Evitar coleções globais contendo:

* todos os métodos;
* todos os relations;
* todo o source code;
* todas as syntax trees da solution;
* todos os CIIR documents.

Roslyn inevitavelmente manterá algumas estruturas internamente, mas a aplicação não deverá adicionar retenção desnecessária.

---

# 13. CIIR v1

Cada registro deverá possuir o seguinte envelope conceitual:

```json
{
  "schemaVersion": "1.0",
  "id": "...",
  "kind": "...",
  "language": "...",
  "project": "...",
  "symbol": {},
  "source": {},
  "documentation": {},
  "comments": [],
  "relations": [],
  "conditions": [],
  "controlFlow": {},
  "embeddingText": "...",
  "embeddingTextStrategy": "semantic-v1",
  "embeddingTextHash": "...",
  "extensions": {}
}
```

Nem todas as propriedades são obrigatórias para todos os `kind`.

Propriedades sem conteúdo deverão preferencialmente ser omitidas em vez de serializadas com grandes estruturas vazias.

---

# 14. `schemaVersion`

Obrigatório.

Versão inicial:

```json
"schemaVersion": "1.0"
```

Mudanças incompatíveis deverão alterar major version.

---

# 15. `id`

Todo elemento deverá possuir uma identidade determinística.

Não utilizar GUID aleatório.

Duas análises do mesmo símbolo, com a mesma identidade semântica, devem produzir o mesmo ID.

Formato recomendado:

```text
sha256:<hash>
```

O hash deverá ser construído a partir de uma chave canônica.

Conceitualmente:

```text
language
+
project identity
+
kind
+
canonical symbol identity
```

Exemplo de chave anterior ao hash:

```text
csharp|
Payments|
method|
Payments.Application.PaymentService.AuthorizeAsync(
  Payments.Domain.Order,
  System.Threading.CancellationToken
)
```

Overloads deverão possuir IDs diferentes.

O algoritmo utilizado deverá estar documentado e testado.

---

# 16. `kind`

O schema CIIR deverá prever inicialmente:

```text
project
namespace

type
method
constructor
property
field
event

function

file

parameter

database
table
column
view
procedure
function_db
trigger

configuration
configuration_key

endpoint
message
```

O gerador C# v1 não precisa implementar todos esses tipos.

### Tipos obrigatórios para o C# v1

```text
project
namespace
type
method
constructor
property
field
event
```

Não implementar funcionalidades artificiais apenas para preencher todos os kinds.

YAGNI deve ser respeitado.

---

# 17. `language`

Para o gerador inicial:

```json
"language": "csharp"
```

A especificação deverá permitir outros valores futuramente.

---

# 18. `project`

Identifica o projeto lógico de origem.

Exemplo:

```json
"project": "Payments.Application"
```

O manifest deverá conter informação suficiente para correlacionar esse nome ao `.csproj`.

---

# 19. `symbol`

Formato:

```json
{
  "symbol": {
    "name": "AuthorizeAsync",
    "qualifiedName": "Payments.Application.PaymentService.AuthorizeAsync",
    "canonicalName": "Payments.Application.PaymentService.AuthorizeAsync(Payments.Domain.Order,System.Threading.CancellationToken)",
    "container": "Payments.Application.PaymentService"
  }
}
```

### `name`

Nome simples.

### `qualifiedName`

Nome completo legível.

### `canonicalName`

Identidade completa e não ambígua do símbolo.

Deve distinguir overloads.

### `container`

Entidade semanticamente proprietária.

---

# 20. Types

Um registro `type` deverá possuir:

```json
{
  "type": {
    "typeKind": "class",
    "accessibility": "public",
    "modifiers": [
      "abstract"
    ],
    "genericParameters": []
  }
}
```

`typeKind` poderá representar:

```text
class
interface
struct
record
enum
delegate
unknown
```

A especificação CIIR poderá permitir outros valores para outras linguagens.

---

# 21. Methods

Exemplo:

```json
{
  "method": {
    "accessibility": "public",
    "modifiers": [
      "async"
    ],
    "parameters": [
      {
        "name": "order",
        "type": "Payments.Domain.Order"
      },
      {
        "name": "cancellationToken",
        "type": "System.Threading.CancellationToken"
      }
    ],
    "returnType": "System.Threading.Tasks.Task<Payments.Domain.PaymentResult>"
  }
}
```

Deverão ser preservados quando semanticamente relevantes:

```text
static
abstract
virtual
override
sealed
async
extern
```

---

# 22. Source location

Todo símbolo declarado no source deverá possuir:

```json
{
  "source": {
    "path": "src/Payments/PaymentService.cs",
    "startLine": 10,
    "startColumn": 5,
    "endLine": 48,
    "endColumn": 6,
    "hash": "sha256:..."
  }
}
```

Os caminhos deverão ser preferencialmente relativos à raiz de análise.

Nunca depender exclusivamente de paths absolutos da máquina.

Linhas e colunas deverão utilizar convenção documentada e consistente.

Preferência:

```text
line: 1-based
column: 1-based
```

---

# 23. Source text

Por padrão, o código-fonte completo do elemento não precisa ser colocado no JSONL.

Quando:

```text
--include-source
```

for informado, poderá ser emitido:

```json
{
  "source": {
    "...": "...",
    "text": "public async Task..."
  }
}
```

O `embeddingText` deverá continuar sendo produzido mesmo sem `source.text`.

---

# 24. Documentation

Documentações formais da linguagem deverão ser extraídas.

Para C#:

```text
XML Documentation Comments
```

Exemplo de source:

```csharp
/// <summary>
/// Authorizes a payment for the given order.
/// </summary>
/// <param name="order">
/// Order being authorized.
/// </param>
/// <returns>
/// Authorization result.
/// </returns>
public Task<PaymentResult> AuthorizeAsync(Order order);
```

CIIR:

```json
{
  "documentation": {
    "format": "xml-doc",
    "source": "declared",
    "summary": "Authorizes a payment for the given order.",
    "remarks": null,
    "parameters": [
      {
        "name": "order",
        "description": "Order being authorized."
      }
    ],
    "returns": "Authorization result.",
    "exceptions": []
  }
}
```

O schema deverá prever formatos futuros:

```text
xml-doc
javadoc
jsdoc
tsdoc
docstring
markdown
plain
unknown
```

---

# 25. Comentários comuns

Comentários comuns deverão ser preservados separadamente da documentação formal.

Exemplo:

```csharp
// Workaround for legacy SAP payment status.
if (status == 17)
{
}
```

CIIR:

```json
{
  "comments": [
    {
      "kind": "line",
      "text": "Workaround for legacy SAP payment status.",
      "location": {
        "startLine": 57,
        "startColumn": 9,
        "endLine": 57,
        "endColumn": 51
      }
    }
  ]
}
```

Kinds inicialmente suportados:

```text
line
block
todo
fixme
warning
note
```

Comentários devem ser associados ao elemento semântico apropriado quando for possível fazer isso de maneira determinística.

---

# 26. Relações

Cada entidade poderá conter:

```json
{
  "relations": [
    {
      "kind": "calls",
      "target": {
        "id": "sha256:...",
        "symbol": "Payments.Domain.IPaymentGateway.AuthorizeAsync"
      },
      "resolution": {
        "status": "resolved",
        "origin": "project"
      },
      "location": {
        "startLine": 31,
        "startColumn": 20,
        "endLine": 31,
        "endColumn": 55
      }
    }
  ]
}
```

---

# 27. Relações obrigatórias para C# v1

Quando detectáveis estaticamente:

```text
contains
inherits
implements
overrides

calls
constructs

reads
writes

throws
catches
```

Não é necessário criar a relação inversa.

Exemplo:

```text
A CALLS B
```

é suficiente.

Não gerar também:

```text
B CALLED_BY A
```

Essa relação poderá ser construída posteriormente pelo Code Graph.

---

# 28. Relações com propriedades

Quando relevante:

```csharp
order.Total
```

deverá poder produzir:

```text
READS Order.Total
```

Uma atribuição:

```csharp
order.Status = OrderStatus.Paid;
```

deverá produzir:

```text
WRITES Order.Status
```

---

# 29. Criação de objetos

Código:

```csharp
new Payment(...)
```

deverá poder produzir:

```text
CONSTRUCTS Payment
```

---

# 30. Calls

A análise deverá utilizar o Semantic Model.

Não inferir chamadas apenas pelo texto da syntax tree.

Por exemplo:

```csharp
_gateway.AuthorizeAsync(...)
```

deverá tentar resolver o `IMethodSymbol`.

O CIIR deve utilizar a informação semanticamente resolvida.

---

# 31. Polimorfismo

Não fingir que análise estática conhece necessariamente a implementação concreta chamada em runtime.

Exemplo:

```csharp
IPaymentGateway gateway;

gateway.AuthorizeAsync();
```

O fato diretamente observável é:

```text
CALLS IPaymentGateway.AuthorizeAsync
```

Implementações possíveis deverão ser representadas pelo grafo de tipos:

```text
StripeGateway IMPLEMENTS IPaymentGateway
AdyenGateway IMPLEMENTS IPaymentGateway
```

Não gerar falsamente:

```text
CALLS StripeGateway.AuthorizeAsync
```

sem evidência estática suficiente.

---

# 32. Resolution

Formato:

```json
{
  "resolution": {
    "status": "resolved",
    "origin": "project"
  }
}
```

Statuses:

```text
resolved
unresolved
ambiguous
external
dynamic
```

Origins:

```text
project
solution
dependency
framework
runtime
external_service
unknown
```

`solution` é usado quando o alvo pertence a um projeto **diferente** do projeto atualmente
analisado, mas que faz parte desta mesma execução de análise (ex.: projeto A tem
`ProjectReference` para o projeto B, e ambos são analisados na mesma execução). Nesse caso,
`status` é `resolved` (não `external`), e `relationTarget.id` deve ser preenchido com o id do
documento CIIR do alvo, exatamente como para um alvo do próprio projeto.

---

# 33. External symbols

Chamadas a frameworks ou dependências externas ao **run** de análise (pacotes NuGet, BCL, ou
qualquer assembly que não corresponda a um projeto também carregado nesta mesma execução) não
exigem que uma entidade CIIR completa seja gerada para o alvo.

Exemplo:

```json
{
  "kind": "calls",
  "target": {
    "symbol": "System.String.IsNullOrEmpty(System.String)"
  },
  "resolution": {
    "status": "external",
    "origin": "framework"
  }
}
```

Um alvo em outro projeto que também faz parte desta mesma execução (ver seção 32, origin
`solution`) NÃO é considerado externo: `status` é `resolved`, e `relationTarget.id` é preenchido.

---

# 34. Conditions

Preservar condições relevantes existentes no código.

Código:

```csharp
if (order.Total <= 0)
    throw new InvalidOrderException();
```

CIIR:

```json
{
  "conditions": [
    {
      "kind": "if",
      "expression": "order.Total <= 0",
      "location": {
        "startLine": 17,
        "endLine": 18
      },
      "reads": [
        "Payments.Domain.Order.Total"
      ]
    }
  ]
}
```

Kinds iniciais:

```text
if
else_if
switch
switch_expression
while
do_while
for
foreach
conditional_expression
guard
```

Não tentar interpretar a condição como regra de negócio nesta etapa.

---

# 35. Control Flow

A v1 não deverá serializar um CFG completo.

Deverá registrar inicialmente métricas úteis:

```json
{
  "controlFlow": {
    "basicBlockCount": 7,
    "cyclomaticComplexity": 3,
    "hasBranches": true,
    "hasLoops": false
  }
}
```

O detalhamento futuro em basic blocks deverá ser possível sem quebrar o CIIR v1.

---

# 36. `embeddingText`

O `embeddingText` deverá ser produzido pelo analisador e armazenado diretamente no JSONL.

Exemplo:

```json
{
  "embeddingText": "Entity: method\nQualified name: Payments.Application.PaymentService.AuthorizeAsync\nDocumentation: Authorizes a payment for the given order.\nParameters:\n- order: Payments.Domain.Order\nReturns: Payments.Domain.PaymentResult\nReads:\n- Payments.Domain.Order.Total\nCalls:\n- Payments.Domain.IPaymentGateway.AuthorizeAsync\nThrows:\n- Payments.Domain.InvalidOrderException",
  "embeddingTextStrategy": "semantic-v1",
  "embeddingTextHash": "sha256:..."
}
```

O vetor NÃO deverá ser produzido.

Não incluir:

```json
"embedding": [0.12, -0.34]
```

---

# 37. Estratégia de geração do `embeddingText`

A estratégia inicial deverá se chamar:

```text
semantic-v1
```

Ela deverá ser determinística.

Mesma CIIR semanticamente relevante deverá produzir o mesmo texto.

A ordem recomendada é:

```text
Entity
Qualified name
Container
Documentation
Signature
Parameters
Returns
Relevant comments
Reads
Writes
Calls
Constructs
Throws
Conditions
```

Se uma seção estiver vazia, deverá ser omitida.

---

# 38. Exemplo de embeddingText

```text
Entity: method
Qualified name: Payments.Application.PaymentService.AuthorizeAsync
Container: Payments.Application.PaymentService

Documentation:
Authorizes a payment for the given order.

Parameters:
- order: Payments.Domain.Order
- cancellationToken: System.Threading.CancellationToken

Returns:
Payments.Domain.PaymentResult

Reads:
- Payments.Domain.Order.Id
- Payments.Domain.Order.Total

Calls:
- Payments.Domain.IPaymentGateway.AuthorizeAsync
- Payments.Domain.IPaymentRepository.SaveAsync

Throws:
- Payments.Domain.InvalidOrderException
```

---

# 39. Informação irrelevante no embeddingText

O CIIR deverá manter todas as relações extraídas relevantes para o grafo.

Porém o `embeddingText` é uma **projeção semântica**, não uma cópia integral do CIIR.

Chamadas extremamente genéricas de framework não precisam necessariamente participar do embedding.

Exemplos frequentemente pouco relevantes:

```text
ILogger.LogInformation
String.IsNullOrEmpty
Task.WhenAll
Enumerable.ToList
```

A filtragem deverá ser implementada por uma política explícita e testável.

Não espalhar `if` de filtragem pelo analisador.

Criar um componente coeso responsável por essa decisão.

Por exemplo:

```text
IEmbeddingTextPolicy
```

ou equivalente, caso a abstração se justifique na implementação.

---

# 40. Hash do embedding text

Gerar:

```json
"embeddingTextHash": "sha256:..."
```

calculado sobre o texto exato enviado futuramente ao modelo de embedding.

Isso permitirá:

```text
embeddingTextHash não mudou
            ↓
     vetor continua válido
```

e evitará reprocessamento desnecessário futuramente.

---

# 41. Separação entre fatos e inferência

O CIIR deverá armazenar prioritariamente fatos observáveis.

Exemplos:

```text
CALLS PaymentGateway.Authorize
READS Order.Total
WRITES Payment.Status
THROWS InvalidOrderException
```

Não gerar automaticamente:

```text
"This method performs fraud validation."

"Business rule: a payment cannot..."
```

como fatos semânticos.

Interpretação por LLM será responsabilidade de outro componente futuro.

---

# 42. Uso de Roslyn

A análise C# deverá utilizar a combinação adequada de:

```text
Syntax Tree
+
Semantic Model
+
Compilation
```

A Syntax Tree identifica estruturas sintáticas.

O Semantic Model deverá ser utilizado para resolver significado.

Exemplo:

```csharp
payment.Authorize();
```

Não registrar apenas:

```text
Authorize
```

quando Roslyn puder determinar:

```text
Payments.Domain.Payment.Authorize()
```

---

# 43. Símbolos

Usar `ISymbol` e suas especializações para produzir identidades canônicas.

Considerar:

```text
INamespaceSymbol
INamedTypeSymbol
IMethodSymbol
IPropertySymbol
IFieldSymbol
IEventSymbol
IParameterSymbol
```

Não guardar instâncias Roslyn dentro dos objetos Core.

Converter imediatamente para tipos CIIR.

---

# 44. Partial classes

Partial classes deverão representar **uma única entidade semântica de type**.

As múltiplas declarações deverão ser preservadas.

O schema deverá permitir:

```json
{
  "sourceLocations": [
    {...},
    {...}
  ]
}
```

ou estrutura equivalente.

Não criar dois IDs de type somente porque existem dois arquivos `partial`.

---

# 45. Partial methods

Aplicar a mesma lógica de identidade semântica.

Não gerar duplicação artificial.

---

# 46. Generated code

Generated code deverá ser ignorado por padrão quando puder ser identificado com segurança.

Exemplos:

```text
*.g.cs
*.generated.cs
obj/
```

Código explicitamente pertencente ao projeto mas identificado como generated poderá ser ignorado.

Essa decisão deverá ser contabilizada no relatório.

Adicionar opção futura deverá ser possível sem modificar o núcleo.

---

# 47. Records

C# records deverão gerar:

```text
kind = type
type.typeKind = record
```

Detalhes exclusivamente C# poderão utilizar:

```json
{
  "extensions": {
    "csharp": {}
  }
}
```

---

# 48. Extensions

Qualquer informação específica de linguagem que não pertença ao modelo universal deverá ficar sob:

```json
{
  "extensions": {
    "csharp": {
    }
  }
}
```

O Core não deverá começar a acumular propriedades como:

```text
isCSharpRecordStruct
usesNullableContext
hasPrimaryConstructor
```

a menos que sejam transformadas posteriormente em conceitos realmente universais.

---

# 49. Exemplo completo de method

```json
{
  "schemaVersion": "1.0",
  "id": "sha256:a81f...",
  "kind": "method",
  "language": "csharp",
  "project": "Payments.Application",

  "symbol": {
    "name": "AuthorizeAsync",
    "qualifiedName": "Payments.Application.PaymentService.AuthorizeAsync",
    "canonicalName": "Payments.Application.PaymentService.AuthorizeAsync(Payments.Domain.Order,System.Threading.CancellationToken)",
    "container": "Payments.Application.PaymentService"
  },

  "method": {
    "accessibility": "public",
    "modifiers": [
      "async"
    ],
    "parameters": [
      {
        "name": "order",
        "type": "Payments.Domain.Order"
      },
      {
        "name": "cancellationToken",
        "type": "System.Threading.CancellationToken"
      }
    ],
    "returnType": "System.Threading.Tasks.Task<Payments.Domain.PaymentResult>"
  },

  "source": {
    "path": "src/Payments.Application/PaymentService.cs",
    "startLine": 9,
    "startColumn": 5,
    "endLine": 24,
    "endColumn": 6,
    "hash": "sha256:..."
  },

  "documentation": {
    "format": "xml-doc",
    "source": "declared",
    "summary": "Authorizes a payment for the given order.",
    "parameters": [
      {
        "name": "order",
        "description": "Order to authorize."
      }
    ],
    "returns": "Payment authorization result."
  },

  "comments": [],

  "relations": [
    {
      "kind": "calls",
      "target": {
        "symbol": "Payments.Domain.IPaymentGateway.AuthorizeAsync"
      },
      "resolution": {
        "status": "resolved",
        "origin": "project"
      }
    },
    {
      "kind": "reads",
      "target": {
        "symbol": "Payments.Domain.Order.Total"
      },
      "resolution": {
        "status": "resolved",
        "origin": "project"
      }
    },
    {
      "kind": "throws",
      "target": {
        "symbol": "Payments.Domain.InvalidOrderException"
      },
      "resolution": {
        "status": "resolved",
        "origin": "project"
      }
    }
  ],

  "conditions": [
    {
      "kind": "if",
      "expression": "order.Total <= 0",
      "reads": [
        "Payments.Domain.Order.Total"
      ],
      "location": {
        "startLine": 13,
        "endLine": 14
      }
    }
  ],

  "controlFlow": {
    "basicBlockCount": 5,
    "cyclomaticComplexity": 2,
    "hasBranches": true,
    "hasLoops": false
  },

  "embeddingText": "Entity: method\nQualified name: Payments.Application.PaymentService.AuthorizeAsync\nContainer: Payments.Application.PaymentService\nDocumentation: Authorizes a payment for the given order.\nParameters:\n- order: Payments.Domain.Order\n- cancellationToken: System.Threading.CancellationToken\nReturns: Payments.Domain.PaymentResult\nReads:\n- Payments.Domain.Order.Total\nCalls:\n- Payments.Domain.IPaymentGateway.AuthorizeAsync\nThrows:\n- Payments.Domain.InvalidOrderException",

  "embeddingTextStrategy": "semantic-v1",
  "embeddingTextHash": "sha256:..."
}
```

---

# 50. JSON Schema

O projeto deverá gerar e manter:

```text
ciir.schema.json
```

Esse schema é um **artifact público do projeto**.

Sua finalidade é permitir que terceiros criem:

```text
Java CIIR Generator
TypeScript CIIR Generator
Python CIIR Generator
SQL CIIR Generator
```

sem depender da implementação C#.

O JSON Schema deverá representar integralmente o CIIR v1.

---

# 51. Requisitos do JSON Schema

Utilizar uma versão moderna e amplamente suportada de JSON Schema.

O schema deverá:

* possuir `$schema`;
* possuir `$id`;
* possuir `title`;
* possuir `description`;
* possuir `$defs`;
* declarar campos obrigatórios;
* declarar enums quando apropriado;
* declarar formatos;
* impedir estruturas obviamente inválidas;
* documentar semanticamente propriedades;
* representar corretamente tipos opcionais;
* ser legível por humanos;
* funcionar em validadores JSON Schema independentes de .NET.

Utilizar `$defs` para estruturas reutilizáveis.

Exemplo:

```text
$defs:
  symbol
  sourceLocation
  documentation
  relation
  relationTarget
  relationResolution
  condition
  controlFlow
  parameter
  comment
```

---

# 52. O JSON Schema é o contrato

Testes deverão validar que todo registro CIIR produzido pelo gerador C# é válido contra:

```text
ciir.schema.json
```

Isso deverá fazer parte da suíte automática de testes.

O resultado esperado é:

```text
C# generator
      │
      ▼
 CIIR JSON
      │
      ▼
JSON Schema Validator
      │
      ▼
    valid
```

---

# 53. Evolução do Schema

Mudanças backward-compatible podem evoluir minor version.

Exemplo:

```text
1.0 → 1.1
```

Mudanças incompatíveis exigem:

```text
2.0
```

Não alterar silenciosamente a semântica de propriedades existentes.

---

# 54. Manifest

Gerar:

```text
manifest.json
```

Exemplo:

```json
{
  "format": "ciir",
  "schemaVersion": "1.0",

  "generator": {
    "name": "ciir-csharp",
    "version": "1.0.0"
  },

  "input": {
    "type": "directory",
    "path": "."
  },

  "generatedAt": "2026-09-07T03:00:00Z",

  "projects": [
    {
      "name": "Payments.Application",
      "path": "src/Payments.Application/Payments.Application.csproj"
    }
  ],

  "files": [
    {
      "path": "ciir.jsonl",
      "records": 18245,
      "sha256": "..."
    },
    {
      "path": "ciir.schema.json",
      "sha256": "..."
    }
  ],

  "statistics": {
    "projects": 7,
    "filesAnalyzed": 824,
    "types": 1231,
    "methods": 8752,
    "relations": 52173,
    "unresolvedRelations": 213
  }
}
```

---

# 55. Analysis report

Gerar:

```text
analysis-report.json
```

O relatório deverá conter informações operacionais, não CIIR.

Exemplo:

```json
{
  "success": true,

  "projects": {
    "discovered": 8,
    "analyzed": 7,
    "failed": 1
  },

  "documents": {
    "analyzed": 824,
    "ignored": 75
  },

  "relations": {
    "resolved": 51960,
    "unresolved": 213
  },

  "errors": [
    {
      "project": "Legacy.Reporting",
      "message": "Project could not be loaded.",
      "category": "project_load"
    }
  ],

  "warnings": []
}
```

---

# 56. Relações não resolvidas

O relatório deverá contabilizar relações:

```text
unresolved
ambiguous
dynamic
```

Sempre que possível registrar a razão.

Exemplo:

```json
{
  "source": "Legacy.PaymentService.Process",
  "relation": "calls",
  "target": "processor.Process",
  "status": "dynamic",
  "reason": "Invocation target could not be determined statically."
}
```

Não inventar resolução.

---

# 57. Logging

Logging deverá ser estruturado.

Utilizar abstração padrão:

```text
Microsoft.Extensions.Logging
```

O Application e demais bibliotecas não deverão escrever diretamente em:

```csharp
Console.WriteLine(...)
```

Somente a camada CLI poderá controlar apresentação de console.

---

# 58. Progresso

Para bases grandes, o CLI deverá informar progresso.

Exemplo:

```text
Discovering projects...
Found 17 projects.

[1/17] Payments.Domain
       153 files
       1,824 entities

[2/17] Payments.Application
       ...
```

A implementação do progresso não poderá ser dependência da lógica de análise.

Utilizar abstração/eventos/callback apropriado.

---

# 59. Cancellation

Toda operação longa deverá aceitar:

```csharp
CancellationToken
```

`Ctrl+C` no CLI deverá solicitar cancelamento gracioso.

O writer deverá finalizar/fechar corretamente arquivos quando possível.

---

# 60. Exit codes

No mínimo:

```text
0 = success
1 = analysis completed with fatal failure
2 = invalid arguments/input
3 = output/write failure
```

É permitido definir outros códigos se documentados.

---

# 61. Determinismo

Executar:

```bash
ciir ./MySolution.sln
```

duas vezes sobre o mesmo conteúdo deverá produzir semanticamente o mesmo CIIR.

Quando possível, inclusive a ordenação deverá ser estável.

Campos naturalmente variáveis como:

```text
generatedAt
```

ficam restritos ao manifest.

Não incluir timestamps variáveis em cada documento CIIR.

---

# 62. Ordenação

Para facilitar diff, testes e reprodutibilidade:

* projetos devem possuir ordem determinística;
* documentos devem possuir ordem determinística;
* relations devem possuir ordem determinística;
* parâmetros mantêm ordem de declaração;
* modifiers deverão possuir ordem canônica;
* arrays sem significado posicional deverão ser ordenados deterministicamente.

---

# 63. Thread safety

A implementação poderá utilizar paralelismo, mas:

* não comprometer determinismo;
* não compartilhar SemanticModel de maneira insegura;
* não criar consumo ilimitado de memória;
* limitar concorrência.

Caso o paralelismo complique significativamente a v1, preferir implementação correta e extensível.

YAGNI.

---

# 64. Performance

A prioridade inicial é:

```text
correctness
>
semantic accuracy
>
memory efficiency
>
performance
```

Evitar micro-otimizações prematuras.

Entretanto, não criar arquitetura que exija carregar toda uma solution transformada em objetos CIIR antes de gravar o arquivo.

---

# 65. Testes unitários

Criar testes para no mínimo:

* geração de IDs determinísticos;
* canonical names;
* tipos;
* métodos;
* overloads;
* constructors;
* properties;
* fields;
* events;
* accessibility;
* modifiers;
* generics;
* inheritance;
* interfaces;
* overrides;
* calls;
* reads;
* writes;
* constructs;
* throws;
* catches;
* conditions;
* XML documentation;
* comments;
* embeddingText;
* embeddingTextHash;
* serialization;
* JSON Schema validation.

---

# 66. Testes de integração

Criar pequenos projetos C# de fixture.

Exemplo:

```text
fixtures/
  BasicSolution/
  MultipleProjects/
  Inheritance/
  AsyncCalls/
  Documentation/
  Conditions/
```

Executar o pipeline real:

```text
fixture
   ↓
MSBuildWorkspace
   ↓
Roslyn
   ↓
CIIR
   ↓
JSONL
   ↓
JSON Schema validation
```

---

# 67. Teste de diretório

Deverá existir um teste com estrutura semelhante a:

```text
repo/
  App.sln

  src/
    Domain/
      Domain.csproj

    Application/
      Application.csproj

  tools/
    Tool/
      Tool.csproj
```

Validar que cada `.csproj` é processado apenas uma vez.

---

# 68. Testes snapshot/golden file

Para casos pequenos, utilizar arquivos esperados de CIIR.

Exemplo:

```text
AuthorizePayment.cs
        ↓
AuthorizePayment.expected.jsonl
```

Isso é especialmente importante para impedir mudanças acidentais no contrato.

Snapshots deverão ser pequenos, legíveis e revisáveis.

---

# 69. Code coverage

Cobertura deverá ser utilizada como métrica auxiliar, não como objetivo isolado.

Priorizar cobertura das regras semanticamente importantes.

---

# 70. Build

A solution deverá compilar sem warnings relevantes.

O pipeline esperado deverá suportar:

```bash
dotnet restore
dotnet build
dotnet test
```

Preferencialmente:

```bash
dotnet build --no-restore
dotnet test --no-build
```

quando usado em CI.

---

# 71. Formatting

Adicionar:

```text
.editorconfig
```

com regras consistentes.

Utilizar analyzers padrão do .NET e configurações que favoreçam código compatível com Sonar.

---

# 72. Dependency injection

Utilizar DI na composition root.

O CLI deverá realizar a composição.

Exemplo conceitual:

```text
CLI
 │
 └── service registration
          │
          ├── Application
          ├── CSharp Analyzer
          ├── JSONL Writer
          ├── Schema Provider
          └── Reporter
```

Evitar Service Locator.

Evitar passar `IServiceProvider` para serviços de domínio/aplicação.

---

# 73. Fluxo principal

O fluxo deve ser aproximadamente:

```text
ciir <path>
      │
      ▼
Input Resolver
      │
      ├── solution?
      ├── project?
      └── directory?
      │
      ▼
Project Discovery
      │
      ▼
Unique Project Set
      │
      ▼
C# Analyzer
      │
      ▼
Roslyn Compilation
      │
      ▼
Syntax + Semantic Analysis
      │
      ▼
CIIR Documents
      │
      ├─────────────► EmbeddingTextBuilder
      │
      ▼
Streaming CIIR Writer
      │
      ▼
ciir.jsonl
      │
      ├─────────────► manifest.json
      ├─────────────► analysis-report.json
      └─────────────► ciir.schema.json
```

---

# 74. Fronteira essencial

A seguinte dependência é permitida:

```text
Ciir.CSharp
      ↓
Ciir.Core
```

A seguinte dependência é proibida:

```text
Ciir.Core
      ↓
Microsoft.CodeAnalysis
```

Da mesma maneira:

```text
Ciir.Application
      ↓
Ciir.Core
```

é permitido.

Mas:

```text
Ciir.Application
      ↓
Ciir.Cli
```

é proibido.

---

# 75. Preparação para futuros geradores

A arquitetura deverá possibilitar futuramente algo semelhante a:

```csharp
public interface ICodeAnalyzer
{
    bool CanAnalyze(AnalysisInput input);

    IAsyncEnumerable<CiirDocument> AnalyzeAsync(
        AnalysisInput input,
        CancellationToken cancellationToken);
}
```

Essa assinatura é apenas ilustrativa.

A implementação deverá escolher a API concreta mais apropriada.

O requisito arquitetural é que o Application dependa de uma abstração, e não diretamente de Roslyn.

---

# 76. Preparação para outros iniciadores

O principal caso de uso deverá poder ser chamado aproximadamente assim:

```csharp
await analyzer.ExecuteAsync(
    request,
    cancellationToken);
```

sem qualquer necessidade de:

```text
Console
CommandLine
args[]
```

Isso deverá permitir futuramente:

```text
Ciir.Api
Ciir.Worker
Ciir.KubernetesJob
Ciir.GitWebhook
```

reutilizando integralmente o mesmo Application/Core/CSharp engine.

---

# 77. O que NÃO implementar agora

Não implementar:

* banco PostgreSQL;
* pgvector;
* embeddings reais;
* chamada a modelos de IA;
* LLM;
* reranking;
* REST API;
* Kubernetes;
* queue;
* graph database;
* Neo4j;
* graph traversal;
* business-rule extraction;
* execução dinâmica do código;
* instrumentação runtime;
* data-flow interprocedural sofisticado;
* CFG completo serializado;
* análise de Java;
* análise de JavaScript;
* análise de Python.

Esses pontos fazem parte de possíveis fases futuras.

YAGNI.

---

# 78. README

Criar README contendo:

* propósito do projeto;
* arquitetura;
* definição de CIIR;
* estrutura dos projetos;
* build;
* testes;
* instalação;
* exemplos CLI;
* arquivos gerados;
* exemplos JSONL;
* como validar CIIR contra JSON Schema;
* regras de versionamento;
* como criar futuramente um novo generator;
* limitações conhecidas da análise estática.

---

# 79. Documento de especificação do CIIR

Além do JSON Schema, criar:

```text
docs/ciir-specification.md
```

Esse documento deverá explicar semanticamente o contrato.

O JSON Schema diz:

```text
"o que é válido"
```

A especificação Markdown deve explicar:

```text
"o que significa"
```

Exemplo:

```text
calls

Represents a statically observable invocation from the
source entity to the target callable symbol.

It MUST NOT be used to represent a possible runtime
implementation derived only from polymorphism.
```

Essa distinção é essencial para futuros generators implementados por terceiros.

---

# 80. Definição formal

Utilizar a seguinte definição:

> Code Intelligence Intermediate Representation (CIIR) is a language-independent intermediate representation designed to describe statically observable software entities, their semantic properties, source evidence, relationships, documentation and selected control-flow characteristics.
>
> Language-specific analyzers translate native syntax and semantic models into CIIR.
>
> CIIR serves as a stable interchange format for downstream code-intelligence systems including search, embedding generation, dependency graphs, execution-flow analysis and software comprehension.
>
> CIIR represents observable facts and SHALL avoid presenting probabilistic or AI-generated interpretations as deterministic program facts.

---

# 81. Critérios de aceite

A implementação somente estará completa quando todos os critérios abaixo forem atendidos.

### Entrada project

```bash
ciir MyProject.csproj
```

gera CIIR válido para o projeto.

### Entrada solution

```bash
ciir MySolution.sln
```

gera CIIR para todos os projetos C# da solution.

### Entrada directory

```bash
ciir ./repository
```

descobre e processa todos os projetos C# únicos existentes abaixo da pasta.

### JSONL

Produz:

```text
ciir.jsonl
```

válido e processável linha a linha.

### Schema

Produz:

```text
ciir.schema.json
```

e todos os registros gerados são válidos contra ele.

### Manifest

Produz:

```text
manifest.json
```

com informações da execução.

### Report

Produz:

```text
analysis-report.json
```

com erros, warnings e relações não resolvidas.

### Semantic analysis

A implementação utiliza Roslyn Semantic Model e não somente parsing textual.

### Relations

Extrai no mínimo:

```text
inherits
implements
overrides
calls
constructs
reads
writes
throws
catches
```

quando estaticamente observáveis.

### Documentation

Extrai XML Documentation.

### Comments

Preserva comentários normais relevantes separadamente.

### Conditions

Preserva condições encontradas nos métodos.

### Embedding text

Todo elemento indexável possui:

```text
embeddingText
embeddingTextStrategy
embeddingTextHash
```

### Determinismo

Mesmo source produz os mesmos IDs e conteúdo semântico.

### Arquitetura

CLI não contém lógica de análise.

### Roslyn isolation

Nenhum projeto genérico depende de tipos Roslyn.

### Testes

Todos passam com:

```bash
dotnet test
```

### Quality

Solution compila sem warnings relevantes e segue boas práticas compatíveis com análise Sonar.

---

# 82. Diretriz final para implementação

Antes de escrever código:

1. criar a solution;
2. estabelecer dependências entre projetos;
3. implementar o modelo CIIR;
4. criar `ciir.schema.json`;
5. criar testes de conformidade do schema;
6. implementar serialização JSONL;
7. implementar resolução de input;
8. implementar descoberta de projetos;
9. implementar análise Roslyn;
10. implementar geração do `embeddingText`;
11. implementar orchestration;
12. implementar CLI;
13. implementar manifest e relatório;
14. criar testes de integração;
15. documentar o CIIR.

Não inverter essa ordem criando primeiro um grande `Program.cs`.

A arquitetura deverá existir antes da integração CLI.

---

# 83. Regra arquitetural central

O sistema deverá permanecer conceitualmente dividido em:

```text
             ENTRY POINTS
                  │
         ┌────────┴────────┐
         │                 │
        CLI             future API
         │                 │
         └────────┬────────┘
                  ▼
             Application
                  │
        ┌─────────┴──────────┐
        ▼                    ▼
   Analyzer Contract       Writers
        ▲
        │
    C# / Roslyn
        │
        ▼
       CIIR
```

O **CIIR é o contrato central da plataforma**, e não Roslyn e não o CLI.

Roslyn é apenas o primeiro produtor desse contrato.

O CLI é apenas o primeiro consumidor do caso de uso de geração desse contrato.
