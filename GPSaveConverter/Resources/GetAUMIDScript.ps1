<#	
	.NOTES
	===========================================================================
	 Created on:    December 20, 2020
	 Created by:   	Brian Lima, Joseph Tignor
	 Filename:     	GetAUMIDScript.ps1
	===========================================================================
	.DESCRIPTION
		Load package name, family name, and logo for all installed UWP packages.
#>
$installedapps = get-AppxPackage
$invalidNames = '*ms-resource*', '*DisplayName*'
$aumidList = @()

foreach ($app in $installedapps)
{
    try {
            if(-not $app.IsFramework){
            foreach ($id in (Get-AppxPackageManifest $app).package.applications.application.id)
            {
                    $appx = Get-AppxPackageManifest $app;
                    $name = $appx.Package.Properties.DisplayName;

                    if($name -like '*DisplayName*' -or $name  -like '*ms-resource*')
                    {
                        $name = $appx.Package.Applications.Application.VisualElements.DisplayName;
                    }
                    if($name -like '*DisplayName*' -or $name  -like '*ms-resource*')
                    {
                        $name = "";
                    }

                    $logo = $app.InstallLocation + "\" + $appx.Package.Applications.Application.VisualElements.Square150x150Logo;

                    $aumidList += $name + "|" + $logo + "|" +
                    $app.packagefamilyname #+ "`r"
                }
            }
        }
        catch
        {
            $ErrorMessage = $_.Exception.Message
            $FailedItem = $_.Exception.ItemName
        }
}

$aumidList;