param(
    [Parameter(Mandatory = $true)]
    [string]$TemplatePath,
    [Parameter(Mandatory = $true)]
    [string]$OutputPath,
    [Parameter(Mandatory = $true)]
    [string]$ShellDllPackagePath,
    [Parameter(Mandatory = $true)]
    [string]$RootClsid,
    [string]$PackageVersion = '',
    [string]$IncludeShellExtension = 'false'
)

$shouldIncludeShellExtension = $false
[bool]::TryParse($IncludeShellExtension, [ref]$shouldIncludeShellExtension) | Out-Null

$manifest = New-Object System.Xml.XmlDocument
$manifest.PreserveWhitespace = $true
$manifest.Load($TemplatePath)

$packageNamespace = 'http://schemas.microsoft.com/appx/manifest/foundation/windows10'
$comNamespace = 'http://schemas.microsoft.com/appx/manifest/com/windows10'
$desktop4Namespace = 'http://schemas.microsoft.com/appx/manifest/desktop/windows10/4'
$desktop5Namespace = 'http://schemas.microsoft.com/appx/manifest/desktop/windows10/5'

$namespaceManager = New-Object System.Xml.XmlNamespaceManager($manifest.NameTable)
$namespaceManager.AddNamespace('pkg', $packageNamespace)

$applicationNode = $manifest.SelectSingleNode('/pkg:Package/pkg:Applications/pkg:Application', $namespaceManager)
if ($null -eq $applicationNode)
{
    throw 'Package manifest does not contain an Application node.'
}

if (-not [string]::IsNullOrWhiteSpace($PackageVersion))
{
    $identityNode = $manifest.SelectSingleNode('/pkg:Package/pkg:Identity', $namespaceManager)
    if ($null -eq $identityNode)
    {
        throw 'Package manifest does not contain an Identity node.'
    }

    $identityNode.SetAttribute('Version', $PackageVersion)
}

$existingExtensions = $applicationNode.SelectSingleNode('pkg:Extensions', $namespaceManager)
if ($null -ne $existingExtensions)
{
    [void]$applicationNode.RemoveChild($existingExtensions)
}

if ($shouldIncludeShellExtension)
{
    $extensionsNode = $manifest.CreateElement('Extensions', $packageNamespace)

    $comExtensionNode = $manifest.CreateElement('com', 'Extension', $comNamespace)
    $comExtensionNode.SetAttribute('Category', 'windows.comServer')

    $comServerNode = $manifest.CreateElement('com', 'ComServer', $comNamespace)
    $surrogateServerNode = $manifest.CreateElement('com', 'SurrogateServer', $comNamespace)
    $surrogateServerNode.SetAttribute('DisplayName', 'Copilot Profile Manager')

    $classNode = $manifest.CreateElement('com', 'Class', $comNamespace)
    $classNode.SetAttribute('Id', $RootClsid)
    $classNode.SetAttribute('Path', $ShellDllPackagePath)
    $classNode.SetAttribute('ThreadingModel', 'STA')
    [void]$surrogateServerNode.AppendChild($classNode)
    [void]$comServerNode.AppendChild($surrogateServerNode)
    [void]$comExtensionNode.AppendChild($comServerNode)
    [void]$extensionsNode.AppendChild($comExtensionNode)

    $fileExplorerExtensionNode = $manifest.CreateElement('desktop4', 'Extension', $desktop4Namespace)
    $fileExplorerExtensionNode.SetAttribute('Category', 'windows.fileExplorerContextMenus')

    $fileExplorerContextMenusNode = $manifest.CreateElement('desktop4', 'FileExplorerContextMenus', $desktop4Namespace)

    foreach ($itemType in @('Directory', 'Directory\Background'))
    {
        $itemTypeNode = $manifest.CreateElement('desktop5', 'ItemType', $desktop5Namespace)
        $itemTypeNode.SetAttribute('Type', $itemType)

        $verbNode = $manifest.CreateElement('desktop5', 'Verb', $desktop5Namespace)
        $verbNode.SetAttribute('Id', 'Copilot')
        $verbNode.SetAttribute('Clsid', $RootClsid)

        [void]$itemTypeNode.AppendChild($verbNode)
        [void]$fileExplorerContextMenusNode.AppendChild($itemTypeNode)
    }

    [void]$fileExplorerExtensionNode.AppendChild($fileExplorerContextMenusNode)
    [void]$extensionsNode.AppendChild($fileExplorerExtensionNode)

    [void]$applicationNode.AppendChild($extensionsNode)
}

$outputDirectory = Split-Path -Parent $OutputPath
if (-not [string]::IsNullOrWhiteSpace($outputDirectory))
{
    New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
}

$manifest.Save($OutputPath)
