#Connect-AzureRmAccount
./Scripts/Deploy-FabricApplication.ps1 -PublishProfileFile "PublishProfiles\Cloud.xml" -ApplicationParameterFilePath "pkg\Release" -OverwriteBehavior "Always" -StartupServicesFile "./StartupServices.xml"

 #Get-ServiceFabricClusterManifest