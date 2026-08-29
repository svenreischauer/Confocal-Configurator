param(
    [Parameter(Mandatory = $true)]
    [string]$InputDirectory,
    [string]$OutputPath = ".\DyeReferenceProfiles.cs"
)

$mappings = @(
    @{ Source = "SVbDNA";                Names = @("DAPI") },
    @{ Source = "1398dna_2";             Names = @("Hoechst 33258") },
    @{ Source = "1398dna_3";             Names = @("Hoechst 33342") },
    @{ Source = "31553p72";              Names = @("Alexa Fluor 405") },
    @{ Source = "10993ph8";              Names = @("Pacific Blue") },
    @{ Source = "2761old_2";             Names = @("FITC") },
    @{ Source = "11001ph8";              Names = @("Alexa Fluor 488") },
    @{ Source = "31555p8";               Names = @("Alexa Fluor 514") },
    @{ Source = "11002p72";              Names = @("Alexa Fluor 532") },
    @{ Source = "11003p72";              Names = @("Alexa Fluor 546") },
    @{ Source = "21422p72";              Names = @("Alexa Fluor 555") },
    @{ Source = "11004p72";              Names = @("Alexa Fluor 568") },
    @{ Source = "11005p72";              Names = @("Alexa Fluor 594") },
    @{ Source = "21235p72";              Names = @("Alexa Fluor 647") },
    @{ Source = "21057p72";              Names = @("Alexa Fluor 680") },
    @{ Source = "cy3igg";                Names = @("Cy3") },
    @{ Source = "TAMRATRITC";            Names = @("TRITC") },
    @{ Source = "6390p72";               Names = @("Texas Red") },
    @{ Source = "481ph9";                Names = @("Calcein") },
    @{ Source = "14200ca";               Names = @("Fluo-4") },
    @{ Source = "1304dna";               Names = @("Propidium iodide") },
    @{ Source = "7020dna";               Names = @("SYTOX Green") },
    @{ Source = "11368dna";              Names = @("SYTOX Orange") },
    @{ Source = "34859dna";              Names = @("SYTOX Red") },
    @{ Source = "7514moh";               Names = @("MitoTracker Green") },
    @{ Source = "7512moh";               Names = @("MitoTracker Red CMXRos") },
    @{ Source = "CellTrackerOrangeCMTMR"; Names = @("CellTracker Orange") },
    @{ Source = "282lip";                Names = @("DiI") },
    @{ Source = "307lip";                Names = @("DiD") },
    @{ Source = "3166ch2";               Names = @("FM 4-64") },
    @{ Source = "2184moh";               Names = @("BODIPY FL") }
)

$resolvedInput = (Resolve-Path -LiteralPath $InputDirectory).Path
$builder = New-Object Text.StringBuilder
[void]$builder.AppendLine("// Generated from Thermo Fisher Fluorescence SpectraViewer reference curves. Do not edit by hand.")
[void]$builder.AppendLine("// Source catalog: https://www.thermofisher.com/content/dam/LifeTech/Documents/spectra/spectra.xml")
[void]$builder.AppendLine("namespace ConfocalKonfigurator")
[void]$builder.AppendLine("{")
[void]$builder.AppendLine("    internal static partial class SpectralProfileLibrary")
[void]$builder.AppendLine("    {")
[void]$builder.AppendLine("        private static SpectralProfile MeasuredChemicalDyeProfile(string name)")
[void]$builder.AppendLine("        {")

foreach ($mapping in $mappings) {
    $inputPath = Join-Path $resolvedInput ($mapping.Source + ".txt")
    if (-not (Test-Path -LiteralPath $inputPath)) {
        throw "SpectraViewer file not found: $inputPath"
    }

    $emission = @{}
    $lines = [IO.File]::ReadAllLines($inputPath)
    for ($lineIndex = 1; $lineIndex -lt $lines.Length; $lineIndex++) {
        $parts = $lines[$lineIndex].Split(',')
        if ($parts.Length -lt 4) {
            continue
        }
        $wavelengthText = $parts[2].Trim().Trim('"')
        $intensityText = $parts[3].Trim().Trim('"')
        $wavelength = 0
        $intensity = 0.0
        if ([int]::TryParse($wavelengthText, [ref]$wavelength) -and
            [double]::TryParse($intensityText,
                [Globalization.NumberStyles]::Float,
                [Globalization.CultureInfo]::InvariantCulture,
                [ref]$intensity)) {
            $emission[$wavelength] = $intensity
        }
    }
    if ($emission.Count -eq 0) {
        throw "No emission data found in $inputPath"
    }

    $maximumIntensity = [double](($emission.Values | Measure-Object -Maximum).Maximum)
    $significantWavelengths = @($emission.Keys | Where-Object {
        [double]$emission[$_] -ge $maximumIntensity * 0.002
    } | ForEach-Object { [int]$_ })
    $availableWavelengths = @($emission.Keys | ForEach-Object { [int]$_ })
    $availableMinimum = [int](($availableWavelengths | Measure-Object -Minimum).Minimum)
    $availableMaximum = [int](($availableWavelengths | Measure-Object -Maximum).Maximum)
    $minimum = [int](($significantWavelengths | Measure-Object -Minimum).Minimum) - 5
    $maximum = [int](($significantWavelengths | Measure-Object -Maximum).Maximum) + 5
    $minimum = [Math]::Max($availableMinimum, $minimum)
    $maximum = [Math]::Min($availableMaximum, $maximum)
    $minimum = [int]([Math]::Ceiling($minimum / 5.0) * 5)
    $maximum = [int]([Math]::Floor($maximum / 5.0) * 5)

    $values = New-Object Collections.Generic.List[int]
    for ($wavelength = $minimum; $wavelength -le $maximum; $wavelength += 5) {
        if (-not $emission.ContainsKey($wavelength)) {
            throw "Missing $wavelength nm emission point in $inputPath"
        }
        $normalized = [Math]::Max(0.0, [double]$emission[$wavelength] / $maximumIntensity)
        $values.Add([int][Math]::Round($normalized * 1000.0))
    }

    $conditions = @($mapping.Names | ForEach-Object { 'name == "' + $_ + '"' }) -join " || "
    [void]$builder.AppendLine("            if ($conditions)")
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
    [void]$builder.AppendLine(('                }, "Thermo Fisher SpectraViewer reference curve ' + $mapping.Source + '");'))
    [void]$builder.AppendLine("            }")
}

[void]$builder.AppendLine("            return null;")
[void]$builder.AppendLine("        }")
[void]$builder.AppendLine("    }")
[void]$builder.AppendLine("}")

$resolvedOutput = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputPath)
[IO.File]::WriteAllText($resolvedOutput, $builder.ToString(), (New-Object Text.UTF8Encoding($false)))
Write-Output ("Generated " + $resolvedOutput)
