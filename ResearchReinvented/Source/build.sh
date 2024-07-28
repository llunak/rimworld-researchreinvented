#! /bin/sh
FrameworkPathOverride=$(dirname $(which mono))/../lib/mono/4.7.2-api/ dotnet build *.csproj /property:Configuration=Release
if test $? -eq 0; then
    # no idea why these get created, but they break game loading
    mv ../v1.5/Assemblies/net472/ResearchReinvented.dll ../v1.5/Assemblies/
    rm -rf ../v1.5/Assemblies/net472
fi
