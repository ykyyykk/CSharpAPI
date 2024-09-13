@REM build 並且 從健行科大Windows 部署到 GCELinux
dotnet publish -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true &&(
scp -i C:\Users\User\Desktop\export_key bin/Release/net8.0/linux-x64/publish/C#Server louise@34.82.250.51:/var/www/csharp_server
)