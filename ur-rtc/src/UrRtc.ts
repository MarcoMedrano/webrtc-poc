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
    await this.createRtcPeerConnection();

    const offer = await this.pc!.createOffer();
    console.log('sending offer', offer);
    await this.pc!.setLocalDescription(offer);
    await this.connection!.invoke("AddOffer", offer.sdp);
  };

  private addRemoteIceCandidate = async (candidate: string) => {
    const candidateObj = JSON.parse(candidate);
    console.log("addRemoteIceCandidate ", candidateObj);

    try {
      await this.pc!.addIceCandidate(candidateObj);
    } catch (err) {
      console.warn("Could not add candidate due", err);
    }
  };

  private processOffer = async (sdp: string) => {
    console.log("processOffer ", { type: "offer", sdp: sdp });

    await this.createRtcPeerConnection();

    await this.pc!.setRemoteDescription({ type: "offer", sdp: sdp });

    const answer = await this.pc!.createAnswer();
    this.pc!.setLocalDescription(answer);
    console.log('sending answer', answer);
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

  private createRtcPeerConnection = async () => {
    console.log("Creating RTCPeerConnection with ", this.config);
    
    this.pc = new RTCPeerConnection(this.config);
    this.pc.onicecandidate = this.onLocalIceCandidate;
    // this.pc.ontrack = this.onTrack;
    this.pc.addEventListener(
      "track",
      (e) => {
        console.log('on track from peer connection');
        this.onTrack(e);
      },
      false
    );

    console.log('Setting stream', this._stream);
    if (this._stream) this.pc.addTrack(this._stream.getTracks()[0], this._stream);
    else {
      console.warn("No media stream to share is present. Creating offer to receive audio and video");
      // This seems no needed if setRemoteDescription is set before send and SDP answer
      const offer = await this.pc.createOffer({ offerToReceiveVideo: true });
      await this.pc.setLocalDescription(offer);
    }
  }

  private onLocalIceCandidate = (event: RTCPeerConnectionIceEvent) => {
    console.log("onLocalIceCandidate", event.candidate);
    if (!event.candidate /*|| event.candidate.type !== "relay"*/) return;

    this.connection!.invoke("AddIceCandidate", JSON.stringify(event.candidate));
  };
}