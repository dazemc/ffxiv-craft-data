#! /bin/bash
git submodule update --init --recursive
git submodule foreach git pull origin master
cd ./src/SaintcoinachSrc/
dotnet build -c Release
cd ../../src/CraftDataSrc/CraftData/
dotnet publish -c Release
cd ../../
git add .
git commit -m "updated submodules and build"
git push
exit
