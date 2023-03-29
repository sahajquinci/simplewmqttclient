<#
.SYNOPSIS
  Name: Build.ps1
  Script to automate the processes of build both TLS and Non-TLS versions of SimplMqttClient.
#>

function BuildRelease([Bool]$SSL) {
	# Variables
	$Name = if ($SSL) {"TLS"} else {"Non-TLS"};
	$Configuration = if ($SSL) {"ReleaseSSL"} else {"Release"};
	$FolderName = "./" + $Name;
	$MSBuild = &"${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -prerelease -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe
		
	# Start!	
	Write-Host "Start building $name version..."
	
	# Clean Up old version
	if (Test-Path $FolderName) {
		Write-Host "`tCleaning up existing folder: $FolderName"
		Remove-Item $FolderName -Recurse -Force
	}
	
	# Build
	try {
		Write-Host "`tBuilding SimplMqttClient $Name ($Configuration)..."
		&$MSBuild .\SimplMqttClient\SimplMqttClient.sln /t:Build /p:Configuration=$($Configuration) -m
	} catch {
	  Write-Host "`tSimplMqttClient $Name ($Configuration) Build FAILED"
	  throw $_
	}
	
	# Create new folder
	Write-Host "`tCreating folder: $FolderName"
	$null = New-item -Path $FolderName -ItemType Directory
	
	# Copy CZL
	Write-Host "`tCopy CZL..."
	Copy-Item ".\SimplMqttClient\bin\$Configuration\SimplMqttClient.clz" -Destination $FolderName
	
	# Copy Simpl Templates
	Write-Host "`tCopy Simpl Templates..."
	Copy-Item "MQTT TCP Client Analog.usp.template" -Destination "$FolderName/MQTT TCP Client Analog.usp"
	Copy-Item "MQTT TCP Client Digital.usp.template" -Destination "$FolderName/MQTT TCP Client Digital.usp"
	Copy-Item "MQTT TCP Client Serial.usp.template" -Destination "$FolderName/MQTT TCP Client Serial.usp"
	
	# Modify Simpl Templates
	Write-Host "`tUpdate Simpl Files..."
	(Get-Content "$FolderName\MQTT TCP Client Analog.usp").replace("{{VARIATION}}", $Name) | Set-Content "$FolderName\MQTT TCP Client Analog.usp"
	(Get-Content "$FolderName\MQTT TCP Client Digital.usp").replace("{{VARIATION}}", $Name) | Set-Content "$FolderName\MQTT TCP Client Digital.usp"
	(Get-Content "$FolderName\MQTT TCP Client Serial.usp").replace("{{VARIATION}}", $Name) | Set-Content "$FolderName\MQTT TCP Client Serial.usp"
	
	# Done!	
	Write-Host "Done with $name version!"
}

BuildRelease($false);
BuildRelease($true);