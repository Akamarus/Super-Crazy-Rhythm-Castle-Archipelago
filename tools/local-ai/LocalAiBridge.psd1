@{
    RootModule = 'LocalAiBridge.psm1'
    ModuleVersion = '0.1.0'
    GUID = '8c30da9e-66cb-4cc8-b895-5c894db0509b'
    Author = 'SCRC Archipelago contributors'
    Description = 'Controlled local AI development bridge for SCRC Archipelago.'
    PowerShellVersion = '7.0'
    FunctionsToExport = @(
        'Assert-LoopbackUri'
        'Assert-TaskOperation'
        'Add-LocalAiTaskEvent'
        'Complete-LocalAiReview'
        'Get-LocalAiConfiguration'
        'Get-AllowedLocalAiModelId'
        'Get-LocalAiTask'
        'Get-LocalAiWorktreeStatus'
        'Invoke-OpenWebUiChat'
        'Invoke-LocalAiModelEvaluation'
        'Invoke-LocalAiInvestigation'
        'Invoke-LocalAiBuild'
        'Invoke-LocalAiValidation'
        'New-LocalAiContext'
        'New-LocalAiHandoff'
        'New-LocalAiWorktree'
        'Publish-LocalAiClient'
        'New-LocalAiTask'
        'Resolve-ContainedPath'
        'Resolve-GameLogPath'
        'Resolve-PluginDestination'
        'Set-LocalAiTaskState'
        'Start-LocalAiDevelopmentSession'
        'Stop-LocalAiDevelopmentSession'
    )
    CmdletsToExport = @()
    VariablesToExport = @()
    AliasesToExport = @()
}
