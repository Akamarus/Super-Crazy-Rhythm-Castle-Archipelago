@{
    RootModule = 'LocalAiBridge.psm1'
    ModuleVersion = '0.1.0'
    GUID = '8c30da9e-66cb-4cc8-b895-5c894db0509b'
    Author = 'SCRC Archipelago contributors'
    Description = 'Controlled local AI development bridge for SCRC Archipelago.'
    PowerShellVersion = '7.0'
    FunctionsToExport = @(
        'Assert-LoopbackUri'
        'Get-LocalAiConfiguration'
    )
    CmdletsToExport = @()
    VariablesToExport = @()
    AliasesToExport = @()
}
