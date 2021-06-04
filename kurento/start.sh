#!/bin/bash
PATH=$PATH:/root/.dotnet/
# dotnet --version &
# echo $(/root/.dotnet/dotnet --version) > test1
cd /lb-agent
dotnet run &
cd ..
if [ "$MS_ROLE" == "recorder" ]
then
cd /s3-mover
dotnet run folder=/tmp &
cd ..
fi

exec /entrypoint.sh "$@"