# Push to GitHub using `gh auth token`, bypassing Git Credential Manager when it uses the wrong account.
# Usage (from repo root, after dot-sourcing):
#   . .\scripts\git-push-gh.ps1
#   gpush
#   gpush -u
#   gpush origin my-branch
#   gpush -u origin my-branch

function Invoke-GitPushGh {
	[CmdletBinding()]
	param(
		[Parameter(ValueFromRemainingArguments = $true)]
		[string[]] $GitArgs
	)

	$token = gh auth token 2>$null
	if (-not $token) {
		throw 'No GitHub CLI token. Run: gh auth login'
	}

	$originUrl = (git remote get-url origin 2>$null).Trim()
	if (-not $originUrl) {
		throw 'No origin remote.'
	}

	$originUrl = $originUrl -replace '^https://[^@/]+@', 'https://'
	if ($originUrl -notmatch '\.git$') {
		$originUrl += '.git'
	}

	if ($originUrl -notmatch '^https://github\.com/') {
		Write-Warning 'origin is not GitHub HTTPS; running plain git push.'
		& git push @GitArgs
		$global:LASTEXITCODE = $LASTEXITCODE
		return
	}

	$pushUrl = $originUrl -replace '^https://', "https://x-access-token:${token}@"

	$setUpstream = $false
	$refSpecs = [System.Collections.Generic.List[string]]::new()
	$gitFlags = [System.Collections.Generic.List[string]]::new()

	foreach ($arg in $GitArgs) {
		switch ($arg) {
			{ $_ -eq '-u' -or $_ -eq '--set-upstream' } { $setUpstream = $true; continue }
			'origin' { continue }
			default {
				if ($arg -match '^-' -and $arg -notmatch ':') {
					$gitFlags.Add($arg)
				}
				else {
					$refSpecs.Add($arg)
				}
			}
		}
	}

	if ($refSpecs.Count -eq 0) {
		$branch = git branch --show-current 2>$null
		if (-not $branch) {
			throw 'Detached HEAD; specify a refspec (e.g. gpush origin my-branch).'
		}
		$refSpecs.Add("${branch}:${branch}")
	}

	& git -c credential.helper= push @gitFlags $pushUrl @refSpecs
	if ($LASTEXITCODE -ne 0) {
		$global:LASTEXITCODE = $LASTEXITCODE
		return
	}

	if ($setUpstream) {
		$remoteBranch = $refSpecs[-1]
		if ($remoteBranch -match '^[^:]+:(.+)$') {
			$remoteBranch = $Matches[1]
		}
		else {
			$remoteBranch = $remoteBranch.Split(':')[0]
		}

		& git -c credential.helper= fetch $pushUrl `
			"refs/heads/${remoteBranch}:refs/remotes/origin/${remoteBranch}" 2>$null | Out-Null
		& git branch -u "origin/${remoteBranch}" 2>$null | Out-Null
	}

	$global:LASTEXITCODE = 0
}

Set-Alias -Name gpush -Value Invoke-GitPushGh -Force
