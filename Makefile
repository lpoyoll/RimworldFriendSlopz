build-server:
	dotnet build Source/Server/GameServer.csproj --configuration Release /property:WarningLevel=0

build-client:
	dotnet build Source/Client/GameClient.csproj --configuration Release /property:WarningLevel=0
