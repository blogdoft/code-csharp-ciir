using Ciir.Serialization.SchemaProvider;
using Json.Schema;
using Shouldly;
using System.Text.Json;

namespace Ciir.Serialization.Tests;

public class SchemaConformanceTests
{
    private const string ValidNamespaceJson = """
        {
          "schemaVersion": "1.0",
          "id": "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
          "kind": "namespace",
          "language": "csharp",
          "project": "Payments.Domain",
          "symbol": { "name": "Payments.Domain", "qualifiedName": "Payments.Domain", "canonicalName": "Payments.Domain" }
        }
        """;

    private const string ValidTypeJson = """
        {
          "schemaVersion": "1.0",
          "id": "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
          "kind": "type",
          "language": "csharp",
          "project": "Payments.Domain",
          "symbol": { "name": "Order", "qualifiedName": "Payments.Domain.Order", "canonicalName": "Payments.Domain.Order" },
          "type": { "typeKind": "class", "accessibility": "public", "modifiers": ["sealed"] }
        }
        """;

    private const string ValidMethodJson = """
        {
          "schemaVersion": "1.0",
          "id": "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
          "kind": "method",
          "language": "csharp",
          "project": "Payments.Application",
          "symbol": {
            "name": "AuthorizeAsync",
            "qualifiedName": "Payments.Application.PaymentService.AuthorizeAsync",
            "canonicalName": "Payments.Application.PaymentService.AuthorizeAsync(Payments.Domain.Order)",
            "container": "Payments.Application.PaymentService"
          },
          "method": {
            "accessibility": "public",
            "modifiers": ["async"],
            "parameters": [{ "name": "order", "type": "Payments.Domain.Order" }],
            "returnType": "System.Threading.Tasks.Task"
          },
          "embeddingText": "Entity: method",
          "embeddingTextStrategy": "semantic-v1",
          "embeddingTextHash": "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
        }
        """;

    private const string ValidConstructorJson = """
        {
          "schemaVersion": "1.0",
          "id": "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
          "kind": "constructor",
          "language": "csharp",
          "project": "Payments.Domain",
          "symbol": {
            "name": ".ctor",
            "qualifiedName": "Payments.Domain.Order..ctor",
            "canonicalName": "Payments.Domain.Order..ctor()",
            "container": "Payments.Domain.Order"
          },
          "method": { "accessibility": "public" }
        }
        """;

    private const string ValidPropertyJson = """
        {
          "schemaVersion": "1.0",
          "id": "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
          "kind": "property",
          "language": "csharp",
          "project": "Payments.Domain",
          "symbol": {
            "name": "Total",
            "qualifiedName": "Payments.Domain.Order.Total",
            "canonicalName": "Payments.Domain.Order.Total",
            "container": "Payments.Domain.Order"
          },
          "property": { "accessibility": "public", "type": "System.Decimal", "hasGetter": true, "hasSetter": false }
        }
        """;

    private const string ValidFieldJson = """
        {
          "schemaVersion": "1.0",
          "id": "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
          "kind": "field",
          "language": "csharp",
          "project": "Payments.Domain",
          "symbol": {
            "name": "_gateway",
            "qualifiedName": "Payments.Application.PaymentService._gateway",
            "canonicalName": "Payments.Application.PaymentService._gateway",
            "container": "Payments.Application.PaymentService"
          },
          "field": { "accessibility": "private", "modifiers": ["readonly"], "type": "Payments.Domain.IPaymentGateway" }
        }
        """;

    private const string ValidEventJson = """
        {
          "schemaVersion": "1.0",
          "id": "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
          "kind": "event",
          "language": "csharp",
          "project": "Payments.Domain",
          "symbol": {
            "name": "PaymentAuthorized",
            "qualifiedName": "Payments.Domain.Order.PaymentAuthorized",
            "canonicalName": "Payments.Domain.Order.PaymentAuthorized",
            "container": "Payments.Domain.Order"
          },
          "event": { "accessibility": "public", "type": "System.EventHandler" }
        }
        """;

    public static TheoryData<string, string> ValidSamples() => new()
    {
        { "project", """{"schemaVersion":"1.0","id":"sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa","kind":"project","language":"csharp","project":"Payments.Application","symbol":{"name":"Payments.Application","qualifiedName":"Payments.Application","canonicalName":"Payments.Application"}}""" },
        { "namespace", ValidNamespaceJson },
        { "type", ValidTypeJson },
        { "method", ValidMethodJson },
        { "constructor", ValidConstructorJson },
        { "property", ValidPropertyJson },
        { "field", ValidFieldJson },
        { "event", ValidEventJson },
    };

    public static TheoryData<string, string> InvalidSamples() => new()
    {
        { "missing required id", """{"schemaVersion":"1.0","kind":"type","language":"csharp","project":"P","symbol":{"name":"Order","qualifiedName":"Order","canonicalName":"Order"},"type":{"typeKind":"class","accessibility":"public"}}""" },
        { "invalid kind enum value", """{"schemaVersion":"1.0","id":"sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa","kind":"not-a-real-kind","language":"csharp","project":"P","symbol":{"name":"Order","qualifiedName":"Order","canonicalName":"Order"}}""" },
        { "type kind missing type block", """{"schemaVersion":"1.0","id":"sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa","kind":"type","language":"csharp","project":"P","symbol":{"name":"Order","qualifiedName":"Order","canonicalName":"Order"}}""" },
        { "unknown top-level property", """{"schemaVersion":"1.0","id":"sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa","kind":"namespace","language":"csharp","project":"P","symbol":{"name":"Order","qualifiedName":"Order","canonicalName":"Order"},"notAField":true}""" },
    };

    [Fact]
    public void CiirSchema_IsItselfAValidJsonSchema()
    {
        var schemaElement = JsonDocument.Parse(CiirSchemaProvider.GetSchemaJson()).RootElement;

        var results = MetaSchemas.Draft202012.Evaluate(schemaElement);

        results.IsValid.ShouldBeTrue();
    }

    [Theory]
    [MemberData(nameof(ValidSamples))]
    public void ValidSample_PassesSchemaValidation(string label, string json)
    {
        label.ShouldNotBeNullOrEmpty();
        var instance = JsonDocument.Parse(json).RootElement;

        var results = CiirSchemaFixture.Schema.Evaluate(instance, new EvaluationOptions { OutputFormat = OutputFormat.List });

        results.IsValid.ShouldBeTrue();
    }

    [Theory]
    [MemberData(nameof(InvalidSamples))]
    public void InvalidSample_FailsSchemaValidation(string label, string json)
    {
        label.ShouldNotBeNullOrEmpty();
        var instance = JsonDocument.Parse(json).RootElement;

        var results = CiirSchemaFixture.Schema.Evaluate(instance, new EvaluationOptions { OutputFormat = OutputFormat.List });

        results.IsValid.ShouldBeFalse();
    }
}
