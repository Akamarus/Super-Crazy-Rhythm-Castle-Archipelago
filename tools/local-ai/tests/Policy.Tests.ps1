BeforeAll {
    $script:ModulePath = Join-Path $PSScriptRoot '..\LocalAiBridge.psd1'
    Import-Module $script:ModulePath -Force
}

Describe 'Local AI bridge path policy' {
    It 'resolves a normal child path' {
        $root = Join-Path $TestDrive 'root'
        $child = Join-Path $root 'child\file.txt'
        New-Item -ItemType Directory -Path (Split-Path $child) -Force | Out-Null
        Set-Content -LiteralPath $child -Value 'ok'

        Resolve-ContainedPath -Root $root -Path $child -MustExist |
            Should -Be ([IO.Path]::GetFullPath($child))
    }

    It 'rejects traversal outside the root' {
        $root = Join-Path $TestDrive 'root'
        New-Item -ItemType Directory -Path $root -Force | Out-Null
        { Resolve-ContainedPath -Root $root -Path (Join-Path $root '..\outside.txt') } |
            Should -Throw '*outside approved root*'
    }

    It 'rejects a sibling-prefix path' {
        $root = Join-Path $TestDrive 'repo'
        $sibling = Join-Path $TestDrive 'repo-other\file.txt'
        New-Item -ItemType Directory -Path $root -Force | Out-Null
        { Resolve-ContainedPath -Root $root -Path $sibling } |
            Should -Throw '*outside approved root*'
    }

    It 'rejects wildcard input' {
        $root = Join-Path $TestDrive 'root'
        New-Item -ItemType Directory -Path $root -Force | Out-Null
        { Resolve-ContainedPath -Root $root -Path (Join-Path $root '*.dll') } |
            Should -Throw '*wildcard*'
    }

    It 'requires an existing root' {
        { Resolve-ContainedPath -Root (Join-Path $TestDrive 'missing') -Path 'file.txt' } |
            Should -Throw '*Root does not exist*'
    }

    It 'rejects a reparse-point ancestor' {
        $root = Join-Path $TestDrive 'root'
        $outside = Join-Path $TestDrive 'outside'
        $link = Join-Path $root 'link'
        New-Item -ItemType Directory -Path $root,$outside -Force | Out-Null
        New-Item -ItemType Junction -Path $link -Target $outside | Out-Null

        { Resolve-ContainedPath -Root $root -Path (Join-Path $link 'file.txt') } |
            Should -Throw '*reparse point*'
    }

    It 'derives the exact plugin destination' {
        $game = Join-Path $TestDrive 'Titus'
        New-Item -ItemType Directory -Path $game -Force | Out-Null
        $expected = [IO.Path]::GetFullPath((Join-Path $game 'BepInEx\plugins\RhythmCastleAP'))

        Resolve-PluginDestination -GameDir $game | Should -Be $expected
    }

    It 'does not accept a deployment destination parameter' {
        (Get-Command Resolve-PluginDestination -ErrorAction Stop).Parameters.Keys | Should -Not -Contain 'Destination'
    }

    It 'derives only the exact BepInEx log path' {
        $game = Join-Path $TestDrive 'Titus'
        New-Item -ItemType Directory -Path $game -Force | Out-Null
        $expected = [IO.Path]::GetFullPath((Join-Path $game 'BepInEx\LogOutput.log'))

        Resolve-GameLogPath -GameDir $game | Should -Be $expected
    }

    It 'rejects implementation-only operations in investigation mode' -ForEach @(
        'edit', 'validate', 'build', 'deploy', 'watch-log', 'create-worktree'
    ) {
        { Assert-TaskOperation -Mode investigation -Operation $_ } |
            Should -Throw '*not allowed for investigation*'
    }

    It 'allows read and handoff operations in investigation mode' -ForEach @(
        'read-context', 'invoke-model', 'create-handoff'
    ) {
        { Assert-TaskOperation -Mode investigation -Operation $_ } | Should -Not -Throw
    }

    It 'rejects unknown operations for every mode' {
        { Assert-TaskOperation -Mode implementation -Operation 'run-arbitrary-command' } |
            Should -Throw '*Unknown bridge operation*'
    }
}
