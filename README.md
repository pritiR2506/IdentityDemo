# IdentityDemo

A small .NET API that accepts tokens from two identity providers, Microsoft Entra ID and Auth0, routes each token by issuer, and stores data in Cosmos DB.

## What it includes
- JWT validation for Entra ID and Auth0 on one API (see CLAIMS.md for how the claims differ)
- Protected endpoints that read and write Cosmos DB (local emulator)
- GitHub Actions pipeline that builds, tests, and publishes a container image
- Kubernetes manifests (kind) with a ConfigMap and readiness and liveness probes
- MIGRATION.md comparing Service Fabric and Kubernetes

## Run locally
1. Add your own tenant, client, and Auth0 values to appsettings.json.
2. Start the Cosmos emulator, then run `dotnet run` in IdentityDemo.Api.
3. Call the API with a bearer token from either provider.

## Notes
- Built as a learning project, not production code.
- The Cosmos key in the development settings is the public emulator key.
- Known transitive package warning (NU1903) would be pinned and tracked in production.