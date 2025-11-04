# How Versioning Works in the Power-Fx CI/CD Pipeline

This document explains, in plain language, how the versioning system works in the Power-Fx CI/CD pipeline using **Nerdbank.GitVersioning (NBGV)**.
It focuses on the overall process and logic—not the code or YAML details.

---

## 1. What Nerdbank.GitVersioning Does

NBGV automatically creates version numbers for every build by reading the Git history.
It ensures:

* Each build from any branch has a unique version.
* Stable releases and preview builds are automatically separated.
* Developers never have to manually edit version numbers in project files.

---

## 2. Base Version and Version Rules

At the heart of it is the `version.json` file.
In this setup, it defines:

* **Base version** → `1.6-CI`
  This tells NBGV that the current development cycle is for version 1.6, and it’s a continuous integration (CI) build.

* **Public release rules** → Only builds from *release branches* (like `release/1.6`) or *tags* (like `v1.6.0`) are considered **official public releases**.

That means:

* Anything built from **main** or feature branches is treated as a **preview**.
* Only tagged or release branches produce the final, clean version numbers.

---

## 3. How Versions Are Automatically Created

Whenever a new build runs:

1. NBGV looks at the base version (`1.6-CI`).
2. It counts how many commits have been made since the last tag.
3. It checks which branch or tag is being built.
4. Based on that, it generates a version string following **Semantic Versioning** rules.

Example outcomes:

* From `main`: `1.6.45-CI.20251102`
  → Means 45 commits since the last tag, a CI (preview) build on November 2, 2025.
* From `release/1.6` or tag `v1.6.0`: `1.6.0`
  → A clean, official release version.

---

## 4. How the Pipeline Uses These Versions

When the pipeline runs:

* It lets NBGV decide the version number.
* This version is automatically applied to all assemblies and NuGet packages.
* The build artifacts (packages) are then signed and published with that version.

So every build produces packages like:

* **Preview feeds**: contain versions like `1.6.45-CI.20251102`
* **Official feeds**: contain versions like `1.6.0`, coming only from tagged releases

This makes it easy to distinguish:

* Internal or daily development builds → for internal testing
* Public or customer-ready versions → for production distribution

---

## 5. Why This Matters

✅ **Consistency** – every environment uses the same versioning logic.
✅ **Automation** – no manual version bumps or mistakes.
✅ **Traceability** – any version can be traced back to a specific commit.
✅ **Clarity** – developers know which builds are previews and which are official releases.

---

## In Summary

* **Main branch builds:** create preview versions like `1.6.x-CI.<date>`
* **Release branches or tags:** create stable versions like `1.6.0`
* **NBGV handles all version math automatically** based on Git commits and branch names.
* **The pipeline just follows these rules**, ensuring that every package published is versioned accurately and predictably.

This approach allows the team to continuously build and test from `main`, while only producing stable, public-ready versions when a release is formally tagged.
