FROM kurento/kurento-media-server:latest

COPY kurento/dotnet-install.sh /
COPY kurento/start.sh /

RUN chmod +x dotnet-install.sh
RUN chmod +x start.sh

RUN apt-get update
RUN apt-get install --no-install-recommends --no-install-suggests -y ca-certificates
RUN ./dotnet-install.sh 

COPY /lb-agent /lb-agent
COPY /s3-mover /s3-mover
ENV DOTNET_USE_POLLING_FILE_WATCHER=true
ENTRYPOINT [ "./start.sh" ] 