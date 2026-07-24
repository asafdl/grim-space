# Git push vs GitHub CLI auth (Windows)

When `git push` fails with **403** but you believe you are signed in as the correct GitHub user, Git and the GitHub CLI (`gh`) are often using **different credentials**.

## Symptom

```text
remote: Permission to <owner>/<repo>.git denied to <wrong-account>.
fatal: unable to access 'https://github.com/<owner>/<repo>.git/': The requested URL returned error: 403
```

Meanwhile:

```powershell
gh auth status
```

shows a **different** account, and that account has push access (e.g. `gh repo view <owner>/<repo> --json viewerPermission` returns `WRITE`).

## Diagnosis (safe checks)

Run from the repo root:

```powershell
git remote -v
gh auth status
gh repo view <owner>/<repo> --json name,viewerPermission
```

See which account Git would ask for credentials (no secrets printed if you only inspect username):

```powershell
@"
protocol=https
host=github.com

"@ | git credential fill
```

On Windows, list legacy Git-related entries in Credential Manager:

```powershell
cmdkey /list | Select-String -Pattern "git|github|gh:"
```

Typical layout:

| Layer | What it is |
|-------|------------|
| **`gh`** | OAuth/token for the account you chose with `gh auth login` |
| **Git Credential Manager (GCM)** | Ships with Git for Windows; `credential.helper=manager` in the **system** `gitconfig` |
| **Windows Credential Manager** | May still hold an old `git:https://github.com` generic credential |

Your user `~/.gitconfig` may already route GitHub to `gh`:

```ini
[credential "https://github.com"]
	helper =
	helper = !'<path-to>/gh.exe' auth git-credential
```

GCM can still answer **first** with a cached **work or secondary** GitHub account, so `git push` and `gh` disagree.

## One-off push using the `gh` account

Use the token from `gh` and **disable** credential helpers for that single command so GCM does not override it.

PowerShell (replace branch name as needed):

```powershell
git -c credential.helper= push `
  "https://x-access-token:$(gh auth token)@github.com/<owner>/<repo>.git" `
  <local-branch>:<remote-branch>
```

First push with upstream:

```powershell
git -c credential.helper= push `
  "https://x-access-token:$(gh auth token)@github.com/<owner>/<repo>.git" `
  <local-branch>:<remote-branch> `
  --set-upstream
```

Verify on GitHub (no Git credential stack):

```powershell
gh api "repos/<owner>/<repo>/branches/<url-encoded-branch-name>" --jq .name
```

### Do not use `--set-upstream` with the token URL as a “remote”

If Git writes the push URL (including the token) into `.git/config` under `[branch "..."]` → `remote =`, **fix it immediately**:

```ini
[branch "<branch-name>"]
	remote = origin
	merge = refs/heads/<branch-name>
```

Then set tracking against `origin` after `git fetch`:

```powershell
git fetch origin
git branch -u origin/<branch-name> <branch-name>
```

**Security:** A token must never live in `.git/config`, shell history, or docs. If one was ever stored or logged, **revoke it** on GitHub (Settings → Developer settings → Personal access tokens) and run `gh auth login` again.

## Permanent fixes (pick one)

### 1. Clear stale Windows / GCM GitHub credentials

1. **Credential Manager** → **Windows Credentials** → remove entries such as `git:https://github.com` tied to the wrong account.
2. Optionally erase the host for GCM:

   ```powershell
   @"
   protocol=https
   host=github.com

   "@ | git credential reject
   ```

3. Reconcile Git with `gh`:

   ```powershell
   gh auth setup-git
   git push -u origin <branch-name>
   ```

### 2. Prefer SSH for GitHub

Add an SSH key to the intended GitHub account and point `origin` at `git@github.com:<owner>/<repo>.git`. SSH bypasses the HTTPS credential helper chain for that remote.

### 3. Confirm the failure is really auth

If the denied username matches the account you intend to use, the problem is **repo permissions** (collaborator access, org policy, or pushing to upstream instead of a fork)—not cached credentials.

## Quick reference

| Goal | Command |
|------|---------|
| Who is `gh`? | `gh auth status` |
| Can `gh` push to this repo? | `gh repo view <owner>/<repo> --json viewerPermission` |
| Who does Git HTTPS use? | Error line `denied to <username>` on failed push |
| Push once as `gh` user | `git -c credential.helper= push "https://x-access-token:$(gh auth token)@github.com/..." ...` |
| Branch tracking | Always `origin`, never an embedded-token URL |

## PowerShell alias: `gpush`

Repo script [`scripts/git-push-gh.ps1`](../../scripts/git-push-gh.ps1) defines **`gpush`** (uses `gh auth token`, never writes the token into `.git/config`).

**Per session** (from repo root):

```powershell
. .\scripts\git-push-gh.ps1
gpush              # push current branch to origin
gpush -u           # push and set upstream to origin/<branch>
gpush origin my-branch
```

**Every new terminal** — add to your [PowerShell profile](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_profiles):

```powershell
function gpush {
	$repoRoot = '<path-to-repo>'
	if (-not (Get-Command Invoke-GitPushGh -ErrorAction SilentlyContinue)) {
		. (Join-Path $repoRoot 'scripts\git-push-gh.ps1')
	}
	Invoke-GitPushGh @args
}
```

Replace `<path-to-repo>` with this clone’s root (or use a stable path you keep clones under).

Requires `gh auth login` for the account that can push to `origin`.
