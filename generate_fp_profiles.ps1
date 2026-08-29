param(
    [Parameter(Mandatory = $true)]
    [string]$InputJson,
    [string]$OutputPath = ".\FpMeasuredProfiles.cs"
)

$dataset = [IO.File]::ReadAllText((Resolve-Path -LiteralPath $InputJson)) | ConvertFrom-Json
$mappings = @(
    @{ Source = "EGFP";       Condition = 'name == "EGFP" || name == "GFP" || name == "Emerald GFP"' },
    @{ Source = "mNeonGreen"; Condition = 'name == "mNeonGreen"' },
    @{ Source = "EYFP";       Condition = 'name == "YFP" || name == "EYFP"' },
    @{ Source = "Venus";      Condition = 'name == "Venus"' },
    @{ Source = "Citrine";    Condition = 'name == "Citrine"' },
    @{ Source = "mOrange";    Condition = 'name == "mOrange"' },
    @{ Source = "mOrange2";   Condition = 'name == "mOrange2"' },
    @{ Source = "DsRed";      Condition = 'name == "DsRed"' },
    @{ Source = "DsRed2";     Condition = 'name == "DsRed2"' },
    @{ Source = "tdTomato";   Condition = 'name == "tdTomato"' },
    @{ Source = "TagRFP";     Condition = 'name == "TagRFP"' },
    @{ Source = "mRuby2";     Condition = 'name == "mRuby2"' },
    @{ Source = "mCherry";    Condition = 'name == "mCherry"' },
    @{ Source = "mKate2";     Condition = 'name == "mKate2"' },
    @{ Source = "mPlum";      Condition = 'name == "mPlum"' },
    @{ Source = "iRFP670";    Condition = 'name == "iRFP670"' },
    @{ Source = "mTagBFP2";   Condition = 'name == "mTagBFP2"' },
    @{ Source = "ECFP";       Condition = 'name == "ECFP"' },
    @{ Source = "Cerulean";   Condition = 'name == "Cerulean"' }
)

$builder = New-Object Text.StringBuilder
[void]$builder.AppendLine("// Generated from normalized FPbase emission spectra. Do not edit by hand.")
[void]$builder.AppendLine("// FPbase data are provided under CC BY-SA 4.0: https://www.fpbase.org/")
[void]$builder.AppendLine("namespace ConfocalKonfigurator")
[void]$builder.AppendLine("{")
[void]$builder.AppendLine("    internal static partial class SpectralProfileLibrary")
[void]$builder.AppendLine("    {")
[void]$builder.AppendLine("        private static SpectralProfile MeasuredFluorescentProteinProfile(string name)")
[void]$builder.AppendLine("        {")

foreach ($mapping in $mappings) {
    $entry = $dataset | Where-Object { $_.name -eq $mapping.Source } | Select-Object -First 1
    if ($null -eq $entry) {
        throw "FPbase entry not found: $($mapping.Source)"
    }
    $spectrum = $entry.spectra | Where-Object { $_.state -eq "default_em" } | Select-Object -First 1
    if ($null -eq $spectrum) {
        throw "Default emission spectrum not found: $($mapping.Source)"
    }

    $significant = $spectrum.data | Where-Object { [double]$_[1] -ge 0.002 }
    $significantWavelengths = @($significant | ForEach-Object { [int]$_[0] })
    $minimum = [int](($significantWavelengths | Measure-Object -Minimum).Minimum) - 5
    $maximum = [int](($significantWavelengths | Measure-Object -Maximum).Maximum) + 5
    $minimum = [int]([Math]::Ceiling($minimum / 5.0) * 5)
    $maximum = [int]([Math]::Floor($maximum / 5.0) * 5)
    $points = $spectrum.data | Where-Object {
        ([int]$_[0] -ge $minimum) -and ([int]$_[0] -le $maximum) -and (([int]$_[0] % 5) -eq 0)
    }
    $values = @($points | ForEach-Object { [int][Math]::Round(([double]$_[1]) * 1000.0) })

    [void]$builder.AppendLine("            if ($($mapping.Condition))")
    [void]$builder.AppendLine("            {")
    [void]$builder.AppendLine("                return CreateMeasured($minimum, 5, new int[]")
    [void]$builder.AppendLine("                {")
    for ($index = 0; $index -lt $values.Count; $index += 18) {
        $last = [Math]::Min($index + 17, $values.Count - 1)
        $line = ($values[$index..$last] -join ", ")
        if ($last -lt $values.Count - 1) {
            $line += ","
        }
        [void]$builder.AppendLine("                    $line")
    }
    [void]$builder.AppendLine(('                }, "FPbase measured ' + $mapping.Source + ' emission spectrum");'))
    [void]$builder.AppendLine("            }")
}

[void]$builder.AppendLine("            return null;")
[void]$builder.AppendLine("        }")
[void]$builder.AppendLine("    }")
[void]$builder.AppendLine("}")

$resolvedOutput = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputPath)
[IO.File]::WriteAllText($resolvedOutput, $builder.ToString(), (New-Object Text.UTF8Encoding($false)))
Write-Output ("Generated " + $resolvedOutput)
