# Create the SMB Global Mapping
$password = ConvertTo-SecureString "password" -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential -ArgumentList "Azure\user", $password
New-SmbGlobalMapping -RemotePath "pathtoexternal" -Credential $cred

# Symlink the SMB Global Mapping to a folder on the node (this lets me avoid tracking drive letters!)
pushd C:\SfDevCluster\Data
New-Item -ItemType SymbolicLink -Name minecraft -Target "pathtoexternal" -Force