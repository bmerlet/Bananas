#! /bin/sh

# Builder path
# PATH=$PATH:"/c/Program Files (x86)/MSBuild/12.0/Bin/"
#PATH=$PATH:"/c/Program Files (x86)/Microsoft Visual Studio/2019/Community/MSBuild/Current/Bin"
PATH=$PATH:"/c/Program Files/Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin"

# Build the toolbox
cd ../../C#/Toolbox
msbuild.exe Toolbox.sln /property:Configuration=Debug /property:Platform="Any CPU" || exit 1

# Build bananas
cd ../Bananas
msbuild.exe Bananas.sln /property:Configuration=Debug /property:Platform="Any CPU" || exit 1


