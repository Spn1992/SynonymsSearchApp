<#
.SYNOPSIS
    Automates the infrastructure setup for the SQL Full-text Filter Daemon Launcher.

.DESCRIPTION
    This script ensures that the SQL Full-text Filter Daemon Launcher service is configured and running,
    that the correct NTFS permissions are applied to the temporary directory for the service account,
    and that the required iFilters (e.g., PDF, DOCX) are registered on the system.
    It supports a -CheckMode switch to verify configuration without applying changes.

.PARAMETER CheckMode
    If specified, the script audits the current state and reports any configuration drift without making changes.

.PARAMETER TempDir
    The temporary directory used by the indexing service. Defaults to 'C:\Windows\Temp'.

.PARAMETER ServiceAccount
    The service account running the indexing service. Defaults to 'NT Service\MSSQLFDLauncher'.

.PARAMETER ServiceName
    The name of the Windows service for the Filter Daemon. Defaults to 'MSSQLFDLauncher'.

.PARAMETER LogPath
    The path to the log file. Defaults to 'C:\Logs\IndexingInfraSetup.log'.
#>

param (
    [switch]$CheckMode,
    [string]$TempDir = "C:\Windows\Temp",
    [string]$ServiceAccount = "NT Service\MSSQLFDLauncher",
    [string]$ServiceName = "MSSQLFDLauncher",
    [string]$LogPath = "C:\Logs\IndexingInfraSetup.log",
    [string[]]$RequiredExtensions = @(".pdf", ".docx")
)

# Ensure log directory exists
$LogDir = Split-Path $LogPath -Parent
if ($LogDir -ne "" -and -not (Test-Path $LogDir)) {
    if (-not $CheckMode) {
        New-Item -ItemType Directory -Path $LogDir -Force | Out-Null
    }
}

function Write-Log {
    param([string]$Message, [string]$Level = "INFO")
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logLine = "[$timestamp] [$Level] $Message"
    Write-Host $logLine
    if ($CheckMode -and (-not (Test-Path (Split-Path $LogPath -Parent)))) {
        # Can't write to a log if directory doesn't exist in check mode
        return
    }
    try {
        Add-Content -Path $LogPath -Value $logLine -ErrorAction SilentlyContinue
    } catch { }
}

Write-Log "Starting Setup-IndexingInfra.ps1"
if ($CheckMode) { Write-Log "Running in Check Mode (No changes will be made)" }

$driftDetected = $false
$missingComponents = 0

# 1. Service Configuration
try {
    $service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    if ($service) {
        if ($service.StartType -ne 'Automatic') {
            Write-Log "Service '$ServiceName' StartType is $($service.StartType), expected 'Automatic'." "WARNING"
            $driftDetected = $true
            if (-not $CheckMode) {
                Set-Service -Name $ServiceName -StartupType Automatic
                Write-Log "Set Service '$ServiceName' StartType to Automatic."
            }
        } else {
            Write-Log "Service '$ServiceName' StartType is 'Automatic'."
        }

        if ($service.Status -ne 'Running') {
            Write-Log "Service '$ServiceName' Status is $($service.Status), expected 'Running'." "WARNING"
            $driftDetected = $true
            if (-not $CheckMode) {
                Start-Service -Name $ServiceName
                Write-Log "Started Service '$ServiceName'."
            }
        } else {
            Write-Log "Service '$ServiceName' is 'Running'."
        }
    } else {
        Write-Log "Service '$ServiceName' not found on this system." "ERROR"
        $missingComponents++
    }
} catch {
    Write-Log "Error checking service: $($_.Exception.Message)" "ERROR"
}

# 2. Folder Permissions
try {
    if (-not (Test-Path $TempDir)) {
        Write-Log "Temporary directory '$TempDir' does not exist." "WARNING"
        $driftDetected = $true
        if (-not $CheckMode) {
            New-Item -ItemType Directory -Path $TempDir -Force | Out-Null
            Write-Log "Created directory '$TempDir'."
        }
    }

    if (Test-Path $TempDir) {
        $acl = Get-Acl -Path $TempDir
        # Check if rule exists
        $accessRule = $acl.Access | Where-Object { 
            $_.IdentityReference.Value -eq $ServiceAccount -and 
            $_.FileSystemRights -match "Modify|FullControl" -and 
            $_.AccessControlType -eq "Allow" 
        }
        
        if (-not $accessRule) {
            Write-Log "Account '$ServiceAccount' does not have sufficient access to '$TempDir'." "WARNING"
            $driftDetected = $true
            if (-not $CheckMode) {
                $accessRuleObj = New-Object System.Security.AccessControl.FileSystemAccessRule(
                    $ServiceAccount,
                    "Modify",
                    "ContainerInherit, ObjectInherit",
                    "None",
                    "Allow"
                )
                $acl.SetAccessRule($accessRuleObj)
                Set-Acl -Path $TempDir -AclObject $acl
                Write-Log "Granted 'Modify' permissions to '$ServiceAccount' on '$TempDir'."
            }
        } else {
            Write-Log "Account '$ServiceAccount' already has correct permissions on '$TempDir'."
        }
    }
} catch {
    Write-Log "Error checking/setting permissions: $($_.Exception.Message)" "ERROR"
}

# 3. iFilter Registration
try {
    foreach ($ext in $RequiredExtensions) {
        # Check HKCR\.ext\PersistentHandler
        $regPath = "Registry::HKEY_CLASSES_ROOT\$ext\PersistentHandler"
        if (Test-Path $regPath) {
            $handler = (Get-ItemProperty -Path $regPath -Name "(default)" -ErrorAction SilentlyContinue)."(default)"
            if ([string]::IsNullOrWhiteSpace($handler)) {
                Write-Log "iFilter PersistentHandler for '$ext' is empty." "WARNING"
                $driftDetected = $true
                $missingComponents++
            } else {
                Write-Log "iFilter registered for '$ext' with PersistentHandler '$handler'."
            }
        } else {
            Write-Log "iFilter for '$ext' is missing (PersistentHandler not found)." "WARNING"
            $driftDetected = $true
            $missingComponents++
        }
    }
} catch {
    Write-Log "Error checking iFilters: $($_.Exception.Message)" "ERROR"
}

Write-Log "Execution completed."

if ($CheckMode) {
    if ($driftDetected -or $missingComponents -gt 0) {
        Write-Log "Check mode finished: Configuration drift or missing components detected." "WARNING"
        exit 1
    } else {
        Write-Log "Check mode finished: System is fully configured."
        exit 0
    }
} else {
    if ($missingComponents -gt 0) {
        Write-Log "Setup finished, but some required components (like iFilters or services) are missing from the system." "WARNING"
        exit 1
    } else {
        Write-Log "Setup finished successfully."
        exit 0
    }
}
