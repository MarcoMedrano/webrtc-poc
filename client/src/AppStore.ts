import { observable } from "mobx";

import { MessagePackHubProtocol } from "@microsoft/signalr-protocol-msgpack";
import * as signalR from "@microsoft/signalr";
class AppStore {
  @observable public connected = false;
  @observable public stunList = "stun:stun.l.google.com:19302";
  // `stun:stun.l.google.com:19302` + `\nstun:stun1.l.google.com:19302`;

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
        //.withHubProtocol(new MessagePackHubProtocol())
        .withAutomaticReconnect()
        .build();

      try {
        await this.connection.start();
        console.log("Connected to Signaling Server");

        this.connection.on('AddRemoteIceCandidate', this.addRemoteIceCandidate)
        this.connection.on('AddRemoteSdp', this.addRemoteSdp)
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

  private startIceNegotiation = async () => {
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

    const offer = await this.rtcPeerConnection.createOffer({ offerToReceiveAudio: true, offerToReceiveVideo: true });

    await this.rtcPeerConnection.setLocalDescription(offer);
    await this.connection?.invoke('AddSdp', offer.sdp);
  }

  private addRemoteIceCandidate(candidate: RTCIceCandidateInit) {
    console.log("addRemoteIceCandidate ", candidate);
    // TODO arriving string, check if need to be an object instead
    this.rtcPeerConnection?.addIceCandidate(new RTCIceCandidate(candidate));
  }

  private addRemoteSdp(sdpAnswer: string) {
    console.log("addRemoteSdp ", sdpAnswer);
    this.rtcPeerConnection?.setRemoteDescription(new RTCSessionDescription({ type: "answer", sdp: sdpAnswer }));
  }
}

export default new AppStore();
