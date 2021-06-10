$Script:ErrorActionPreference = 'Stop'

$exclusions = @(
    'EventFlow\Core\AsyncHelper.cs'
    'EventFlow\Core\HashHelper.cs'
    'EventFlow\Logs\Internals\ImportedLibLog.cs'
)

$defaultAuthors = @(
    'Rasmus Mikkelsen'
    'eBay Software Foundation'
)

$files = Get-ChildItem -Path $PSScriptRoot -Directory -Recurse |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)(\\|$)' } |
    Get-ChildItem -File -Filter *.cs |
    Select-Object -ExpandProperty FullName

$year = [datetime]::Now.Year

function IsExcluded([string] $File) {
    foreach ($exclusion in $exclusions) {
        if ($File.EndsWith($exclusion)) { return $true }
    }
}

function CreateCopyright([string] $FromYear, [string] $Name) {
    if ($FromYear -eq $year) {
        "// Copyright (c) $year $Name"
    }
    else {
        "// Copyright (c) $FromYear-$year $Name"
    }
}

function CreateHeader($Copyrights) {
    @"
// The MIT License (MIT)
// 
$(@($Copyrights | ForEach-Object { CreateCopyright $_.FromYear $_.Name }) -join "`r`n")
// https://github.com/eventflow/EventFlow
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.


"@
}

function UpdateFileHeaders {

    $headerRegex = [regex]::new('^\s*\/\/ The MIT License .+(?:\n\/\/.*)+\s*', 'Compiled')
    $copyrightRegex = [regex]::new('\/\/ Copyright \(c\) (\d{4})(?:-\d{4})? (.+)', 'Compiled')

    $defaultHeader = CreateHeader ($defaultAuthors | ForEach-Object {
            [PSCustomObject]@{
                FromYear = 2015
                Name     = $_
            } 
        })

    foreach ($file in $files) {
        if (IsExcluded $file) { continue }

        $content = Get-Content -Path $file -Raw
        $headerMatch = $headerRegex.Match($content)

        $result = if ($headerMatch.Success) {
            $currentHeader = $headerMatch.Value
            $copyrightMatches = $copyrightRegex.Matches($currentHeader)
            $copyrights = $copyrightMatches | ForEach-Object { 
                [PSCustomObject]@{
                    FromYear = $_.Groups[1].Value
                    Name     = $_.Groups[2].Value.Trim()
                } 
            }
        
            $generatedHeader = CreateHeader($copyrights)
            if ($generatedHeader -eq $currentHeader) { continue }    

            $newContent = $generatedHeader + $content.Substring($headerMatch.Length)

            $currentLines = $currentHeader -split "`r`n"
            $generatedLines = $generatedHeader -split "`r`n"
            $differences = Compare-Object $currentLines $generatedLines |
                Where-Object SideIndicator -EQ '<=' |
                ForEach-Object {
                    [PSCustomObject]@{
                        File   = $file
                        Change = $_.InputObject
                    }
                }
            
            [PSCustomObject]@{
                NewContent  = $newContent
                Differences = $differences
            }
        }
        else {
            [PSCustomObject]@{
                NewContent  = $defaultHeader + $content.TrimStart()
                Differences = @(
                    [PSCustomObject]@{
                        File   = $file
                        Change = "New header"
                    })
            } 
        }

        $result.NewContent | Set-Content -Path $file -NoNewline
        $result.Differences
    }
}

UpdateFileHeaders |
    Group-Object Change |
    ForEach-Object {
        [PSCustomObject]@{
            Count  = $_.Count
            Change = $_.Name
            Files  = $_.Group | Select-Object -ExpandProperty File
        }
    }