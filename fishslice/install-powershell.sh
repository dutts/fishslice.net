#!/bin/bash

if [ -z ${TARGETARCH+x} ]; then echo "var is unset"; else echo "var is set to '$TARGETARCH'"; fi

if [ "${TARGETARCH}" = "arm64" ]; then

echo "------------ARM64---------------------"
apt-get update
apt-get install -y wget libssl3 libunwind8
mkdir -p /opt/microsoft/powershell/7
wget -O /tmp/powershell.tar.gz https://github.com/PowerShell/PowerShell/releases/download/v7.2.6/powershell-7.2.6-linux-arm64.tar.gz
tar zxf /tmp/powershell.tar.gz -C /opt/microsoft/powershell/7
chmod +x /opt/microsoft/powershell/7/pwsh
ln -s /opt/microsoft/powershell/7/pwsh /usr/bin/pwsh
rm /tmp/powershell.tar.gz

else

echo "------------AMD64---------------------"
apt update && apt install -y curl gpg
curl https://packages.microsoft.com/keys/microsoft.asc | gpg --yes --dearmor --output /usr/share/keyrings/microsoft.gpg
sh -c 'echo "deb [arch=amd64 signed-by=/usr/share/keyrings/microsoft.gpg] https://packages.microsoft.com/repos/microsoft-debian-bullseye-prod bullseye main" > /etc/apt/sources.list.d/microsoft.list'
apt update && apt install -y powershell

fi