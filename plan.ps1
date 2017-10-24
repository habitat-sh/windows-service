$pkg_name="windows-service"
$pkg_origin="core"
$pkg_version="0.1.0"
$pkg_source="nosuchfile.tar.gz"
$pkg_maintainer="The Habitat Maintainers <humans@habitat.sh>"
$pkg_license=@('Apache-2.0')
$pkg_description="A Windows Service for runnung the Habitat Supervisor"
$pkg_deps=@("core/hab-launcher", "core/powershell")
$pkg_build_deps=@("core/nuget")
$pkg_bin_dirs=@("bin")

function invoke-download { }
function invoke-verify { }

function Invoke-Build {
  Copy-Item $PLAN_CONTEXT/* $HAB_CACHE_SRC_PATH/$pkg_dirname -force
  Copy-Item $PLAN_CONTEXT/Properties $HAB_CACHE_SRC_PATH/$pkg_dirname -force -recurse
  nuget restore $HAB_CACHE_SRC_PATH/$pkg_dirname/packages.config -PackagesDirectory $HAB_CACHE_SRC_PATH/$pkg_dirname/packages -Source "https://www.nuget.org/api/v2"
  $env:MSBuildToolsPath = "$env:SystemRoot\Microsoft.NET\Framework64\v2.0.50727"
  ."$env:SystemRoot\Microsoft.NET\Framework64\v2.0.50727\MSBuild.exe" $HAB_CACHE_SRC_PATH/$pkg_dirname/WindowsService.csproj /t:Build /p:Configuration=Release
  if($LASTEXITCODE -ne 0) {
    Write-Error "dotnet build failed!"
  }
}

function Invoke-Install {
  Copy-Item $HAB_CACHE_SRC_PATH/$pkg_dirname/bin/release/* $pkg_prefix/bin
  Copy-Item $HAB_CACHE_SRC_PATH/$pkg_dirname/* $pkg_prefix/bin -Include @("*.bat", "habitat.ps1")
}