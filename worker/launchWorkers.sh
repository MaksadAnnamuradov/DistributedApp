#!/bin/bash
dotnet run --project ../factServer/app.csproj &
for i in {1..5}
do
    dotnet run &
    echo "started worker $i"
done