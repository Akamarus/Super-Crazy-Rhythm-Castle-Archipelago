BeforeAll {
    $script:ModulePath = Join-Path $PSScriptRoot '..\LocalAiBridge.psd1'
    Import-Module $script:ModulePath -Force
}

Describe 'Open WebUI client' {
    BeforeEach {
        $env:OPENWEBUI_API_KEY = 'open-webui-test-secret'
        $script:Configuration = [pscustomobject]@{
            OpenWebUiBaseUri = [uri] 'http://127.0.0.1:8080'
            ModelId = 'jacks-assistant'
        }
    }

    It 'posts the exact model and messages to chat completions' {
        InModuleScope LocalAiBridge -Parameters @{ Configuration = $script:Configuration } {
            Mock Invoke-RestMethod {
                [pscustomobject]@{
                    id = 'response-1'
                    choices = @([pscustomobject]@{ message = [pscustomobject]@{ content = '{"summary":"ready"}' } })
                }
            }
            $messages = @([pscustomobject]@{ role = 'user'; content = 'hello' })

            $result = Invoke-OpenWebUiChat -Configuration $Configuration -Messages $messages -TimeoutSec 17

            $result.Content | Should -Be '{"summary":"ready"}'
            $result.ResponseId | Should -Be 'response-1'
            Should -Invoke Invoke-RestMethod -Times 1 -Exactly -ParameterFilter {
                $Method -eq 'Post' -and
                $Uri -eq 'http://127.0.0.1:8080/api/chat/completions' -and
                $ConnectionTimeoutSeconds -eq 17 -and
                $Headers.Authorization -eq 'Bearer open-webui-test-secret' -and
                ($Body | ConvertFrom-Json).model -eq 'jacks-assistant' -and
                ($Body | ConvertFrom-Json).messages[0].content -eq 'hello'
            }
        }
    }

    It 'fails before HTTP when the API key is missing' {
        Remove-Item Env:OPENWEBUI_API_KEY -ErrorAction SilentlyContinue
        InModuleScope LocalAiBridge -Parameters @{ Configuration = $script:Configuration } {
            Mock Invoke-RestMethod { throw 'must not run' }
            { Invoke-OpenWebUiChat -Configuration $Configuration -Messages @() } |
                Should -Throw '*OPENWEBUI_API_KEY*'
            Should -Invoke Invoke-RestMethod -Times 0
        }
    }

    It 'rejects malformed response content' -ForEach @(
        [pscustomobject]@{},
        [pscustomobject]@{ choices = @() },
        [pscustomobject]@{ choices = @([pscustomobject]@{ message = [pscustomobject]@{} }) }
    ) {
        $response = $_
        InModuleScope LocalAiBridge -Parameters @{ Configuration = $script:Configuration; Response = $response } {
            Mock Invoke-RestMethod { $Response }
            { Invoke-OpenWebUiChat -Configuration $Configuration -Messages @() } |
                Should -Throw '*malformed response*'
        }
    }

    It 'redacts the token from HTTP failures' {
        InModuleScope LocalAiBridge -Parameters @{ Configuration = $script:Configuration } {
            Mock Invoke-RestMethod { throw 'request failed with open-webui-test-secret' }
            $message = try {
                Invoke-OpenWebUiChat -Configuration $Configuration -Messages @()
            } catch {
                $_.Exception.Message
            }
            $message | Should -Match 'Open WebUI request failed'
            $message | Should -Not -Match 'open-webui-test-secret'
            $message | Should -Match '\[REDACTED\]'
        }
    }

    It 'rejects a non-loopback configuration even if constructed manually' {
        $bad = [pscustomobject]@{ OpenWebUiBaseUri = [uri] 'http://example.com:8080'; ModelId = 'jacks-assistant' }
        { Invoke-OpenWebUiChat -Configuration $bad -Messages @() } | Should -Throw '*loopback*'
    }

    It 'rejects a model other than jacks-assistant' {
        $bad = [pscustomobject]@{ OpenWebUiBaseUri = [uri] 'http://127.0.0.1:8080'; ModelId = 'other-model' }
        { Invoke-OpenWebUiChat -Configuration $bad -Messages @() } | Should -Throw '*jacks-assistant*'
    }
}
