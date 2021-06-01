#!/bin/bash
PATH=$PATH:/root/.dotnet/

dotnet publish -c Release -o /srv/s3-mover

cp s3-mover.service /etc/systemd/system/s3-mover.service
sudo systemctl daemon-reload
sudo systemctl enable s3-mover.service
sudo systemctl start s3-mover
