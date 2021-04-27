import { observable } from "mobx";
import isElectron from "is-electron";


// import { MessagePackHubProtocol } from "@microsoft/signalr-protocol-msgpack";
import * as signalR from "@microsoft/signalr";
class AppStore {
  @observable public emulationType = isElectron() ? "callbar" : "live_monitoring";
  @observable public connected = false;
 
  // @observable public stunOrTurn = "stun:stun.l.google.com:19302";
  // @observable public turnUser:any;
  // @observable public TurnPassword:any;
  // @observable public signalingServer = "http://localhost:5000";

  @observable public stunOrTurn = "turn:54.242.2.183:3478";
  @observable public turnUser = "tdx";
  @observable public TurnPassword = "1234";
  @observable public signalingServer = "http://54.211.79.31:5000";
  
  private connection: signalR.HubConnection | null = null;
  private pc: RTCPeerConnection | null = null;

  public onRemoteTrack: null | ((ms: MediaStream) => void) = null;
  public stream: MediaStream | null = null;

  public get isCallbar() {
    return this.emulationType === "callbar";
  }

  public connect = (): Promise<void> => {
    return new Promise<void>(async (resolve, reject) => {
      this.connection = new signalR.HubConnectionBuilder()
        .withUrl(
          this.signalingServer +
            (this.isCallbar ? "/recording" : "/liveMonitoring")
        )
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

        await this.connection.invoke("Ping");
        resolve();
      } catch (e) {
        console.error("Error with Signaling Server", e);
        reject();
      }
    });
  };

  public disconnect = async () => {
    this.connection?.stop();
  };

  public startRecording = async () => {
    await this.connection!.invoke("Start");
  };

  public stopRecording = async () => {
    await this.connection!.invoke("Stop");
  };

  public startPeerConnection = async () => {
    this.pc = this.createRtcPeerConnection();

    const offer = await this.pc.createOffer(/*{offerToReceiveAudio: true}*/);
    await this.pc.setLocalDescription(offer);
    await this.connection?.invoke("AddOffer", offer.sdp);
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
    console.log("processOffer ", sdp);

    this.pc = this.createRtcPeerConnection();

    await this.pc.setRemoteDescription({ type: "offer", sdp: sdp });

    const answer = await this.pc?.createAnswer();
    this.pc.setLocalDescription(answer);
    await this.connection?.invoke("AddAnswer", answer.sdp);
  };

  private processAnswer = async (sdp: string) => {
    console.log("processAnswer ", sdp);
    await this.pc!.setRemoteDescription({ type: "answer", sdp });
  };

  private onTrack = (event: RTCTrackEvent) => {
    console.log("AppStore.onTrack", event);
    this.onRemoteTrack!(event.streams[0]);
  };

  private createRtcPeerConnection(): RTCPeerConnection {
    const config = {
      iceServers: [
        {
          urls: this.stunOrTurn,
          credential: this.TurnPassword,
          username: this.turnUser,
          // iceTransportPolicy: "relay",
        },
      ],
      // sdpSemantics: "unified-plan",
    };

    console.log("Creating RTCPeerConnection with ", config);
    const pc = new RTCPeerConnection(config);
    pc.onicecandidate = this.onLocalIceCandidate;
    // pc.ontrack = this.onTrack;
    pc.addEventListener(
      "track",
      (e) => {
        this.onTrack(e);
      },
      false
    );
    // this.stream!.getTracks().forEach(t => pc.addTrack(t));

    if (this.stream) pc.addTrack(this.stream?.getTracks()[0], this.stream!);
    else console.warn("No media stream to share is present.");

    return pc;
  }

  private onLocalIceCandidate = (event: RTCPeerConnectionIceEvent) => {
    console.log("onLocalIceCandidate", event.candidate);
    if (!event.candidate /*|| event.candidate.type !== "relay"*/) return;

    this.connection?.invoke("AddIceCandidate", JSON.stringify(event.candidate));
  };
}

export default new AppStore();
