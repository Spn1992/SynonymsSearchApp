# Deploy.ps1
# Automated Deployment Script for Search Configuration

param(
    [string]$TempDirectory = "C:\TempProcessingDir"
)

Write-Host "Starting Automated Service Provisioning..."

# 1. Start and Configure the MSSQLFDLauncher Service
try {
    $service = Get-Service -Name "MSSQLFDLauncher" -ErrorAction Stop
    if ($service.Status -ne 'Running') {
        Write-Host "Starting MSSQLFDLauncher service..."
        Start-Service -Name "MSSQLFDLauncher"
    }
    Set-Service -Name "MSSQLFDLauncher" -StartupType Automatic
    Write-Host "MSSQLFDLauncher service is configured to start automatically and is currently running."
}
catch {
    Write-Host "Error configuring MSSQLFDLauncher service: $($_.Exception.Message)"
}

# 2. Set NTFS permissions for the temporary processing directory
try {
    if (-not (Test-Path $TempDirectory)) {
        New-Item -ItemType Directory -Path $TempDirectory | Out-Null
        Write-Host "Created temporary processing directory at $TempDirectory"
    }

    $Acl = Get-Acl $TempDirectory
    # Give 'NT SERVICE\MSSQLFDLauncher' full control over the temp directory
    $AccessRule = New-Object System.Security.AccessControl.FileSystemAccessRule("NT SERVICE\MSSQLFDLauncher", "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow")
    $Acl.AddAccessRule($AccessRule)
    Set-Acl -Path $TempDirectory -AclObject $Acl
    Write-Host "NTFS permissions for $TempDirectory have been verified and set."
}
catch {
    Write-Host "Error setting permissions for $TempDirectory: $($_.Exception.Message)"
}

Write-Host "Deployment completed successfully."
