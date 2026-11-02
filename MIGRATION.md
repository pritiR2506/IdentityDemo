# Service Fabric to Kubernetes: migration notes

I ran this API on Kubernetes (kind). The Service Fabric column comes from Microsoft's documentation, not from running a cluster.

| Area | Service Fabric | Kubernetes (what I ran) |
|---|---|---|
| Packaging | Application and service manifests (XML), deployed as an application type with a version | Container image plus Deployment and Service YAML |
| Configuration | Settings.xml in a config package, with per-environment parameters in the application manifest | ConfigMap injected as environment variables |
| Secrets | Encrypted values in settings using a certificate, or Key Vault with managed identity | Kubernetes Secret, or an external secret store |
| Health checks | Health reports and health policies, evaluated during upgrades | Readiness and liveness probes on /health |
| Scaling | Instance count on the service, plus scaling the cluster's node set | Replica count, and optionally a horizontal pod autoscaler |
| Upgrades | Rolling upgrade by upgrade domain, rolled back on health failure | Rolling update on the Deployment, rollback with kubectl |
| Discovery | Naming service and reverse proxy | Service and cluster DNS |

## What I saw on Kubernetes
- Probes: (write what `kubectl describe pod` showed for readiness and liveness)
- Scaling: (write what happened when I went from 3 to 5 replicas)
- Config: (write how the ConfigMap values reached the pods)
- Problem I hit: the app crashed at startup because it tried to create the Cosmos database and no emulator existed in the cluster. I made that step optional, and it logs a warning and continues.

## What I would do in a real migration
1. Containerize each Service Fabric service and run it beside the old one.
2. Move configuration and secrets first, since they differ most.
3. Add health endpoints before cutover so probes and upgrades behave.
4. Shift traffic gradually and keep the old cluster until the new one is stable.
5. Check identity settings (issuer, audience, redirect URIs) in each environment.

## Not covered
Stateful Reliable Services, which need a data migration plan and not only a redeploy.code README.md