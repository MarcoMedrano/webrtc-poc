import { observable } from "mobx";
import Peer from "peerjs";
import * as signalR from "@microsoft/signalr";

class AppStore {
  @observable public connected = false;
  @observable public stunList =
    `stun:stun.l.google.com:19302` + `\nstun1.l.google.com:19302`;

  @observable public signalingServer = "https://localhost:5001/recording";

  @observable public uiMessages = observable([]) as any;
  @observable communicationValues = observable([
    "chat",
    "screen",
    "call",
  ]) as any;

  public connect = (): Promise<void> => {
    return new Promise<void>(async (resolve, reject) => {
      const config = {
        iceServers: this.stunList.split("\n").map((s) => {
          return { urls: s };
        }),
        // sdpSemantics: "unified-plan",
      };

      console.log("Will connect using config ", config);

      const connection = new signalR.HubConnectionBuilder()
        .withUrl(this.signalingServer)
        .configureLogging(signalR.LogLevel.Information)
        .withAutomaticReconnect()
        .build();
      
      try {
        await connection.start();
        console.log("Connected to Signaling Server");
        await connection.invoke("Ping");
        connection.on("Pong", () => console.log('Pong'));
        resolve();
      } catch (e) {
        console.error("Error with Signaling Server", e);
        reject();
      }

    });
  };
}

export default new AppStore();

// // Opening a remote connection
// var remoteConnection = peer.connect("recorder");

// remoteConnection.on("open", () => {
//   console.log("remote connection opened");
//   remoteConnection.send("hey");
// });

// // Accepting a remote connection
// peer.on("connection", (remoteConnection: any) => {
//   remoteConnection.on("data", (data: any) => {
//     console.log(data);
//   });
// });
