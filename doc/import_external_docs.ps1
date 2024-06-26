param(
  [Parameter(ValueFromPipeLineByPropertyName = $true)]
  $branches = $null
)

Set-PSDebug -Trace 1

$external_docs = @{
    # use either commit, or branch name to use its latest commit
    "uno.wasm.bootstrap" = "9b559fc9a8b5e939fd99fa119a243e760ae3770d" #latest main commit
    "uno.themes"         = "722dd3386d16523d1e1139d916aa89610f7118ac" #latest release branch commit
    "uno.toolkit.ui"     = "e8821e38a8a0fe8225211a80fd5c74481b9243d1" #latest release branch commit
    "uno.check"          = "4a7dd7290daf0aabfbb8efabcd2b067898b7f45e" #latest main commit
    "uno.xamlmerge.task" = "21f02c98702b875a9942047ca042e41810b6fe56" #latest main commit
    "figma-docs"         = "e48addd1a2ac27a865b0ae95d9502578df5950a2" #latest main commit
    "uno.resizetizer"    = "ec018465f3b7dd7286a46597ebc2b9b66776ee7f" #latest main commit
    "uno.uitest"         = "9669fd2783187d06c36dd6a717c1b9f08d1fa29c" #latest master commit
    "uno.extensions"     = "a7871fabe0bad1ab6a837212854bd7736af8a53d" #latest release branch commit
    "workshops"          = "f87cb67d9fd9cb3e78c740b62d40fe8d3201182a" #latest master commit
    "uno.samples"        = "4599f4e09d12cfef46f5f70e65347e93600af499" #latest master commit
}

$uno_git_url = "https://github.com/unoplatform/"

if($branches -ne $null)
{
    foreach ($repo in $branches.keys)
    {
        $branch = $branches[$repo]

        $external_docs[$repo] = $branch
    }
}

echo "Current setup:"
$external_docs

$ErrorActionPreference = 'Stop'

function Assert-ExitCodeIsZero()
{
    if ($LASTEXITCODE -ne 0)
    {
        popd
        popd

        Set-PSDebug -Off

        throw "Exit code must be zero."
    }
}

if (-Not (Test-Path articles\external))
{
    mkdir articles\external -ErrorAction Continue
}

pushd articles\external

# ensure long paths are supported on Windows
git config --global core.longpaths true

$detachedHeadConfig = git config --get advice.detachedHead
git config advice.detachedHead false

# Heads - Release
foreach ($repoPath in $external_docs.keys)
{
    $repoUrl = "$uno_git_url$repoPath"
    $repoBranch = $external_docs[$repoPath]
        
    if (-Not (Test-Path $repoPath))
    {        
        echo "Cloning $repoPath ($repoUrl@$repoBranch)..."
        git clone $repoUrl $repoPath
        Assert-ExitCodeIsZero
    }

    pushd $repoPath

    echo "Checking out $repoUrl@$repoBranch..."
    git fetch
    git checkout --force $repoBranch
    Assert-ExitCodeIsZero

    # if not detached
    if ((git symbolic-ref -q HEAD) -ne $null)
    {
        echo "Resetting to $repoUrl@$repoBranch..."
        git reset --hard origin/$repoBranch
        Assert-ExitCodeIsZero
    }

    popd
}

git config advice.detachedHead $detachedHeadConfig

popd

Set-PSDebug -Off
