#!/bin/bash
PATH=$PATH:/root/.dotnet/

if [ "$MS_ROLE" == "recorder" ]
then
cd /s3-mover
dotnet run folder=/tmp &
cd ..
fi

exec /entrypoint.sh "$@" &
# exec /entrypoint.sh "$@" 2>&1 1>&1 &

cd /lb-agent
dotnet publish -o out
cd out
dotnet lb-agent.dll

# cd /lb-agent
# dotnet run & 
# cd ..

# exec /entrypoint.sh "$@"
