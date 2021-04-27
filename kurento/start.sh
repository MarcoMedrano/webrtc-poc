#!/bin/bash
PATH=$PATH:/root/.dotnet/
# dotnet --version &
# echo $(/root/.dotnet/dotnet --version) > test1
cd /s3-mover
dotnet run folder=/tmp &
cd ..

exec /entrypoint.sh "$@"