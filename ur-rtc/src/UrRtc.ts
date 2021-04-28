// import { MessagePackHubProtocol } from "@microsoft/signalr-protocol-msgpack";
import * as signalR from "@microsoft/signalr";
import { SimpleEventDispatcher } from "strongly-typed-events";

export default class UrRtc {

  private signalingServer: string;
  private config: RTCConfiguration;
  private connection: signalR.HubConnection | null = null;
  private pc: RTCPeerConnection | null = null;
  private _stream: MediaStream | null = null;
  private _onRemoteTrack = new SimpleEventDispatcher<MediaStream>();

  public get onRemoteTrack() {
    return this._onRemoteTrack.asEvent();
  }

  constructor(signalingServer: string, config: RTCConfiguration) {
    this.signalingServer = signalingServer;
    this.config = config;
  }

  set stream(value: MediaStream) {
    this._stream = value;
    // TODO code to switch stream
  }

  public connect = (): Promise<void> => {
    return new Promise<void>(async (resolve, reject) => {
      this.connection = new signalR.HubConnectionBuilder()
        .withUrl(this.signalingServer)
        .configureLogging(signalR.LogLevel.Debug)
        //.withHubProtocol(new MessagePackHubProtocol())
        .withAutomaticReconnect()
        .build();

      try {
        await this.connection.start();
        console.log("Connected to Signaling Server");

        this.connection.on("AddRemoteIceCandidate", this.addRemoteIceCandidate);
        this.connection.on("processAnswer", this.processAnswer);
        this.connection.on("processOffer", this.processOffer);
        this.connection.on("Pong", () => console.log("Pong"));
        this.connection.onreconnecting((e) => console.warn("Reconnecting ", e));
        this.connection.onreconnected(async () => await this.connection!.invoke("Ping"));

        await this.connection.invoke("Ping");
        resolve();
      } catch (e) {
        console.error("Error with Signaling Server", e);
        reject();
      }
    });
  };

  public disconnect = async () => {
    return this.connection!.stop();
  };

  public startRecording = async () => {
    return this.connection!.invoke("Start");
  };

  public stopRecording = async () => {
    return this.connection!.invoke("Stop");
  };

  public startPeerConnection = async () => {
    this.pc = this.createRtcPeerConnection();

    const offer = await this.pc.createOffer(/*{offerToReceiveAudio: true}*/);
    await this.pc.setLocalDescription(offer);
    await this.connection!.invoke("AddOffer", offer.sdp);
  };

  private addRemoteIceCandidate = async (candidate: string) => {
    console.log("addRemoteIceCandidate ", candidate);

    try {
      await this.pc!.addIceCandidate(JSON.parse(candidate));
    } catch (err) {
      console.warn("Could not add candidate due", err);
    }
  };

  private processOffer = async (sdp: string) => {
    console.log("processOffer ", { type: "offer", sdp: sdp });

    this.pc = this.createRtcPeerConnection();

    await this.pc.setRemoteDescription({ type: "offer", sdp: sdp });

    const answer = await this.pc!.createAnswer();
    this.pc.setLocalDescription(answer);
    await this.connection!.invoke("AddAnswer", answer.sdp);
  };

  private processAnswer = async (sdp: string) => {
    console.log("processAnswer ", { type: "answer", sdp });
    await this.pc!.setRemoteDescription({ type: "answer", sdp });
  };

  private onTrack = (event: RTCTrackEvent) => {
    console.log("UrRtc.onTrack", event);
    this._onRemoteTrack.dispatch(event.streams[0]);
  };

  private createRtcPeerConnection(): RTCPeerConnection {
    console.log("Creating RTCPeerConnection with ", this.config);
    const pc = new RTCPeerConnection(this.config);
    pc.onicecandidate = this.onLocalIceCandidate;
    // pc.ontrack = this.onTrack;
    pc.addEventListener(
      "track",
      (e) => {
        console.log('on track from peer connection');
        this.onTrack(e);
      },
      false
    );
    // this._stream!.getTracks().forEach(t => pc.addTrack(t));
    console.log('Setting stream', this._stream);
    if (this._stream) pc.addTrack(this._stream.getTracks()[0], this._stream);
    else console.warn("No media stream to share is present.");

    return pc;
  }

  private onLocalIceCandidate = (event: RTCPeerConnectionIceEvent) => {
    console.log("onLocalIceCandidate", event.candidate);
    if (!event.candidate /*|| event.candidate.type !== "relay"*/) return;

    this.connection!.invoke("AddIceCandidate", JSON.stringify(event.candidate));
  };
}