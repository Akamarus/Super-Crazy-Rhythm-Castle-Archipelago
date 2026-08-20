# First GitLab Setup

This repository package is already arranged as the intended source tree. Do **not** upload the old version ZIP/RAR history into Git.

## 1. Create the GitLab project

In GitLab:

1. Select **Create new → New project/repository**.
2. Select **Create blank project**.
3. Suggested project name: `Super Crazy Rhythm Castle Archipelago`.
4. Suggested slug: `super-crazy-rhythm-castle-archipelago`.
5. Choose the visibility you want.
6. **Leave “Initialize repository with a README” unchecked** because this folder already contains the initial repository files.
7. Create the project.

Copy the repository URL shown by GitLab. SSH is convenient once your SSH key is configured; HTTPS also works.

## 2. Initialize this local folder

Open PowerShell in the extracted repository folder:

```powershell
git init
git branch -M main
git status
python .\tools\validate-repo.py
git add .
git commit -m "chore: import v0.67.59 client and v0.15 APWorld baseline"
```

If Git asks for your identity first:

```powershell
git config --global user.name "YOUR NAME"
git config --global user.email "YOUR EMAIL"
```

Then run the commit again.

## 3. Connect GitLab and push

Replace the URL below with the **exact URL GitLab shows for your new project**.

SSH example:

```powershell
git remote add origin git@gitlab.com:YOUR_NAMESPACE/super-crazy-rhythm-castle-archipelago.git
git push -u origin main
```

HTTPS example:

```powershell
git remote add origin https://gitlab.com/YOUR_NAMESPACE/super-crazy-rhythm-castle-archipelago.git
git push -u origin main
```

After the push, verify GitLab shows `client/`, `apworld/`, `docs/`, `tools/`, and the root documentation.

## 4. Normal future workflow

Create a feature branch before a new milestone:

```powershell
git switch -c feature/roots-level4-glasses
```

After changes and local testing:

```powershell
git status
git diff
git add .
git commit -m "feat: map Roots Level 4 glasses progression"
git push -u origin feature/roots-level4-glasses
```

Then merge it through GitLab after gameplay validation.

## 5. Important exclusions

Never commit:

- `BepInEx/` or game DLLs
- IL2CPP interop DLLs
- `bin/` or `obj/`
- generated `.apworld` files
- release ZIP/RAR files
- `LogOutput.log`
- passwords, access tokens, or `.env` secrets

The included `.gitignore` covers these common cases.
