.PHONY: default build

default: build

build:
	dotnet build src/

test:
	dotnet run --configuration Release --project src/RSIrc.Tests/RSIrc.Tests.csproj --coverage --report-trx

ide:
	code .

clean-release:
	rm -rf src/RSIrc/bin/Release/

publish: clean-release
	dotnet pack src/RSIrc --configuration Release
	dotnet nuget push src/RSIrc/bin/Release/*.nupkg --api-key $(NUGET_KEY) --source https://api.nuget.org/v3/index.json
