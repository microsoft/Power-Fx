# 🧩 Power Fx NuGet Versioning

This document summarizes how **Nerdbank.GitVersioning (NBGV)** manages package versions in the Power Fx OneBranch pipeline.

---

## Overview

Power Fx uses **Git-driven semantic versioning** to ensure:
- Consistent and reproducible package versions  
- Automatic detection of public vs. CI builds  
- Unified versioning for all assemblies and NuGet packages  

---

## Version Config (`version.json`)

```json
{
  "version": "1.6",
  "publicReleaseRefSpec": [
    "^refs/tags/v\\d+\\.\\d+\\.\\d+$",
    "^refs/heads/release/\\d+\\.\\d+$"
  ],
  "cloudBuild": {
    "setVersionVariables": true,
    "setBuildNumber": true
  }
}
```

**Meaning:**
- `version`: Base major.minor version  
- `publicReleaseRefSpec`: Marks `release/x.y` branches or `vX.Y.Z` tags as *public releases*  
- `cloudBuild`: Exports version variables and updates build numbers  

---

## How Versioning Works

1. Pipeline checks out Power Fx repo.  
2. `dotnet tool run nbgv get-version -f json` reads version info from Git.  
3. JSON output is exported to pipeline variables (`NBGV_NuGetPackageVersion`, `NBGV_PublicRelease`, etc.).  
4. `VSBuild` uses these values to stamp DLLs and `.nupkg` files.  
5. Non-public builds append a date suffix `-CI-YYYYMMDD`.  

| Example Branch | Resulting Package Version |
|-----------------|---------------------------|
| `release/1.6` | `1.6.0` |
| `main` | `1.6.0-CI-20251112` |

---

## Publishing Logic

| Build Type | Condition | Target Feed |
|-------------|------------|-------------|
| Public Release | `NBGV_PublicRelease=True` | Publishes to **NuGet.org** |
| CI / Internal | `NBGV_PublicRelease=False` | Publishes to internal feeds (`Power-Fx`, `OneAgile/PowerApps-Studio-Official`) |

---

## Key MSBuild Arguments

```bash
-p:PublicRelease=$(NBGV_PublicRelease)
-p:RepositoryBranch=$(NBGV_BuildingRef)
-p:RepositoryCommit=$(NBGV_GitCommitId)
-p:PackageVersion=$(NBGV_NuGetPackageVersion)
```

---

## Optional Tagging

If the pipeline parameter `tags=true`, the build:
- Creates a git tag (e.g., `1.6.0`)  
- Pushes it to origin and tags the Azure DevOps build  

---

## Common Issues

| Problem | Cause | Fix |
|----------|--------|-----|
| Wrong version source | `nbgv cloud` used | Use `nbgv get-version` |
| `-CI` suffix on release branch | Branch name doesn’t match regex | Update `publicReleaseRefSpec` |
| NuGet push failed | Missing token | Verify `PFX_ADO_NUGET_ORG_CONNECTION_API_KEY` |

---

## References

- Nerdbank.GitVersioning: https://github.com/dotnet/Nerdbank.GitVersioning  
- OneBranch Versioning Docs: https://aka.ms/obpipelines/versioning  
- NuGet Publish Task: https://learn.microsoft.com/azure/devops/pipelines/tasks/package/nuget

