param(
    [Parameter(Mandatory = $true)]
    [string]$AssemblyPath
)

$resolvedAssembly = (Resolve-Path -LiteralPath $AssemblyPath).Path
$assembly = [Reflection.Assembly]::LoadFrom($resolvedAssembly)
$testType = $assembly.GetType("ConfocalKonfigurator.RegressionTests", $true)
$flags = [Reflection.BindingFlags]::Static -bor [Reflection.BindingFlags]::NonPublic
$testMethod = $testType.GetMethod("Main", $flags)
$result = $testMethod.Invoke($null, $null)
exit [int]$result
