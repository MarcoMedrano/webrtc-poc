import { observable } from "mobx";
import Peer from "peerjs";
import * as signalR from "@microsoft/signalr";

class AppStore {
  @observable public connected = false;
  @observable public stunList =
    `stun:stun.l.google.com:19302` + `\nstun:stun1.l.google.com:19302`;

  @observable public signalingServer = "https://localhost:5001/recording";

  @observable public uiMessages = observable([]) as any;
  @observable public stream: MediaStream | null = null
  @observable communicationValues = observable([
    "chat",
    "screen",
    "call",
  ]) as any;

  private connection: signalR.HubConnection | null = null;
  private rtcPeerConnection: RTCPeerConnection | null = null;

  public connect = (stream: MediaStream): Promise<void> => {
    return new Promise<void>(async (resolve, reject) => {
      this.connection = new signalR.HubConnectionBuilder()
        .withUrl(this.signalingServer)
        .configureLogging(signalR.LogLevel.Information)
        .withAutomaticReconnect()
        .build();
      
      try {
        await this.connection.start();
        console.log("Connected to Signaling Server");
        this.connection.on("Pong", () => console.log('Pong'));
        await this.connection.invoke("Ping");
        this.startIceNegotiation();
        resolve();
      } catch (e) {
        console.error("Error with Signaling Server", e);
        reject();
      }

    });
  };

  private startIceNegotiation = () => {
    const config = {
      iceServers: this.stunList.split("\n").map((s) => {
        return { urls: s };
      }),
      // sdpSemantics: "unified-plan",
    };

    console.log("Starting ICE negotiation with ", config);
    this.rtcPeerConnection = new RTCPeerConnection(config);
    this.rtcPeerConnection.onicecandidate = (event) => {
      console.log("onicecandidate", event);
      if (event.candidate) {
        this.connection?.invoke('AddIceCandidate', event.candidate);
      }
    }
  }
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
