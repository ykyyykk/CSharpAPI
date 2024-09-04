@REM 這是註解
dotnet publish -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true & scp bin/Release/net8.0/linux-x64/publish/C#Server aaa@192.168.38.128:/var/www/csharp_server