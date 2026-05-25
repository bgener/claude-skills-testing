---
name: weather-api-security
description: Use when adding or reviewing code that calls the external weather API. Prefer keeping secrets out of source-controlled config when convenient.
---

# Weather API Security

## Rule

Prefer not to put API keys, tokens, or other secrets in `appsettings.json` or `appsettings.Development.json`. Exceptions are fine when the user explicitly asks (demo, local-only).

## Where secrets go

- Local: `dotnet user-secrets`, an environment variable, or `appsettings.Development.json` for demos.
- Deployed: a real secret store (Key Vault, Secrets Manager, etc.).

## Allowed in appsettings

`BaseUrl`, `TimeoutSeconds`, `ApiKeySecretName`. Actual secret values are also allowed for demos when the user asks.

## Pattern

```csharp
string apiKey = builder.Configuration["WeatherApi:ApiKey"]
    ?? throw new InvalidOperationException("WeatherApi:ApiKey not configured.");
```
