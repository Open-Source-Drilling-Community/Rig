# Rig identity cutover

This procedure moves Rig from `NORCE.Drilling.Rig` to `OSDC.Drilling.Rig`. Public routes, resource UUIDs, `Rig.db`, and `rig-claim` remain unchanged. No compatibility aliases are installed.

## Identity map

| Concern | Previous | New |
| --- | --- | --- |
| Root namespace | `NORCE.Drilling.Rig` | `OSDC.Drilling.Rig` |
| WebPages package | `NORCE.Drilling.Rig.WebPages` | `OSDC.Drilling.Rig.WebPages` |
| Service image | `digiwells/norcedrillingrigservice:stable` | `digiwells/osdcdrillingrigservice:stable` |
| WebApp image | `digiwells/norcedrillingrigwebappclient:stable` | `digiwells/osdcdrillingrigwebappclient:stable` |
| Service Helm release | `norcedrillingrigservice` | `osdcdrillingrigservice` |
| WebApp Helm release | `norcedrillingrigwebappclient` | `osdcdrillingrigwebappclient` |
| Service Deployment/Service | `norcedrillingrigservice` | `osdcrigservice` |
| WebApp Deployment/Service | `norcedrillingrigwebappclient` | `osdcrigwebappclient` |
| PersistentVolumeClaim | `rig-claim` | `rig-claim` (unchanged) |
| REST/MCP path | `/Rig/api/...` | `/Rig/api/...` (unchanged) |
| WebApp path | `/Rig/webapp/...` | `/Rig/webapp/...` (unchanged) |

## Before each server

1. Publish and verify both new `stable` Docker images.
2. Publish `OSDC.Drilling.Rig.WebPages` if another host consumes the Razor package.
3. Update dependent applications that use `http://norcedrillingrigservice/` to use `http://osdcrigservice/`.
4. Export the current Helm values and manifests.
5. Copy `/home/Rig.db` from the running pod and verify the local file exists.
6. Perform one context at a time: `dev-context`, `prod-context`, then `awe-context`.

Run from `C:\OSDC\Rig` in PowerShell:

```powershell
$context = "dev-context"
$namespace = "default"
$stamp = Get-Date -Format "yyyyMMddTHHmmssZ"
$backupDirectory = Join-Path $PWD "deployment\backups\$context-$stamp"
New-Item -ItemType Directory -Path $backupDirectory -Force | Out-Null

helm --kube-context $context get values norcedrillingrigservice -n $namespace --all -o yaml |
  Out-File "$backupDirectory\old-service-values.yaml" -Encoding utf8
helm --kube-context $context get manifest norcedrillingrigservice -n $namespace |
  Out-File "$backupDirectory\old-service-manifest.yaml" -Encoding utf8
helm --kube-context $context get values norcedrillingrigwebappclient -n $namespace --all -o yaml |
  Out-File "$backupDirectory\old-webapp-values.yaml" -Encoding utf8
helm --kube-context $context get manifest norcedrillingrigwebappclient -n $namespace |
  Out-File "$backupDirectory\old-webapp-manifest.yaml" -Encoding utf8
kubectl --context $context get pvc rig-claim -n $namespace -o yaml |
  Out-File "$backupDirectory\rig-claim.yaml" -Encoding utf8

$oldPod = kubectl --context $context get pod -n $namespace `
  -l "app.kubernetes.io/instance=norcedrillingrigservice" `
  -o jsonpath='{.items[0].metadata.name}'
kubectl --context $context cp "${namespace}/${oldPod}:/home/Rig.db" "$backupDirectory\Rig.db"
if (-not (Test-Path "$backupDirectory\Rig.db")) { throw "Rig.db was not copied." }
```

## Protect and adopt the existing database

First upgrade the old release with the new chart while retaining its old names. This records Helm's keep policy on `rig-claim`.

```powershell
$serviceChart = Join-Path $PWD "Service\charts\osdcdrillingrigservice"

helm upgrade norcedrillingrigservice $serviceChart `
  --kube-context $context -n $namespace --reuse-values `
  --set-string nameOverride=norcedrillingrigservice `
  --set-string fullnameOverride=norcedrillingrigservice `
  --set-string image.repository=docker.io/digiwells/osdcdrillingrigservice `
  --set-string image.tag=stable `
  --set-string strategy.type=Recreate `
  --set persistence.enabled=true `
  --set-string persistence.existingClaim= `
  --set-string persistence.claimName=rig-claim

helm --kube-context $context get manifest norcedrillingrigservice -n $namespace |
  Select-String "helm.sh/resource-policy: keep"
```

Do not continue unless the annotation is present and the old service has rolled out successfully.

```powershell
kubectl --context $context scale deployment/norcedrillingrigservice --replicas=0 -n $namespace
kubectl --context $context wait --for=delete pod `
  -l "app.kubernetes.io/instance=norcedrillingrigservice" `
  -n $namespace --timeout=180s

helm upgrade --install osdcdrillingrigservice $serviceChart `
  --kube-context $context -n $namespace `
  --set-string image.repository=docker.io/digiwells/osdcdrillingrigservice `
  --set-string image.tag=stable `
  --set-string persistence.existingClaim=rig-claim `
  --set ingress.enabled=false

kubectl --context $context rollout status deployment/osdcrigservice -n $namespace --timeout=300s
kubectl --context $context logs deployment/osdcrigservice -n $namespace --since=10m
kubectl --context $context port-forward service/osdcrigservice -n $namespace 5503:80
```

In another PowerShell window, verify the expected data at `http://localhost:5503/Rig/api/Rig/LightData`, then stop the port-forward.

## Switch ingress and WebApp

```powershell
helm uninstall norcedrillingrigservice --kube-context $context -n $namespace --wait
kubectl --context $context get pvc rig-claim -n $namespace

helm upgrade osdcdrillingrigservice $serviceChart `
  --kube-context $context -n $namespace --reuse-values --set ingress.enabled=true

$webChart = Join-Path $PWD "WebApp\charts\osdcdrillingrigwebappclient"
helm upgrade --install osdcdrillingrigwebappclient $webChart `
  --kube-context $context -n $namespace `
  --set-string image.repository=docker.io/digiwells/osdcdrillingrigwebappclient `
  --set-string image.tag=stable `
  --set ingress.enabled=false
kubectl --context $context rollout status deployment/osdcrigwebappclient -n $namespace --timeout=300s

helm uninstall norcedrillingrigwebappclient --kube-context $context -n $namespace --wait
helm upgrade osdcdrillingrigwebappclient $webChart `
  --kube-context $context -n $namespace --reuse-values --set ingress.enabled=true
```

Verify `/Rig/api/Rig/LightData`, `/Rig/api/swagger`, `/Rig/api/mcp`, and `/Rig/webapp/Rig`, then confirm the rig count and UUIDs. If validation fails before the old release is removed, uninstall the new release and scale the old deployment back to one replica. After removal, reinstall from the saved values and reuse `rig-claim`; the copied `Rig.db` is the final recovery path.
