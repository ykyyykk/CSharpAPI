# 從mac 部署到 GCELinux
dotnet publish -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true &&(
scp -r -i export_key bin/Release/net8.0/linux-x64/publish/* louise@34.82.250.51:/var/www/csharp_server
)