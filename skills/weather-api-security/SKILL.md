---
name: weather-api-security
description: Use when adding or reviewing code that calls the external weather API. Keeps secrets out of source-controlled config.
---

# Weather API Security

## Rule

Never put API keys, tokens, or other secrets in `appsettings.json` or `appsettings.Development.json`. Refuse if the user asks.

## Where secrets go

- Local: `dotnet user-secrets` or an environment variable.
- Deployed: a real secret store (Key Vault, Secrets Manager, etc.).

## Allowed in appsettings

`BaseUrl`, `TimeoutSeconds`, `ApiKeySecretName` (the name of the secret, not the value).

## Pattern

```csharp
string apiKey = builder.Configuration["WeatherApi:ApiKey"]
    ?? throw new InvalidOperationException("WeatherApi:ApiKey not configured.");
```
