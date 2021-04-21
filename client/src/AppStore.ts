import { observable } from "mobx";

// import { MessagePackHubProtocol } from "@microsoft/signalr-protocol-msgpack";
import * as signalR from "@microsoft/signalr";
class AppStore {
  @observable public emulationType = 'callbar';
  @observable public connected = false;
  @observable public stunOrTurn = "turn:3.86.44.157:3478";
  @observable public turnUser = "tdx";
  @observable public TurnPassword = "1234";
  // @observable public stunList = "stun:stun.l.google.com:19302";
  // `stun:stun.l.google.com:19302` + `\nstun:stun1.l.google.com:19302`;

  @observable public signalingServer = "http://localhost:5000";


  private connection: signalR.HubConnection | null = null;
  private rtcPeerConnection: RTCPeerConnection | null = null;

  public onRemoteTrack: null | ((ms: MediaStream) => void) = null;

  public get isCallbar() { return this.emulationType === 'callbar' }

  public connect = (stream: MediaStream): Promise<void> => {
    return new Promise<void>(async (resolve, reject) => {
      this.connection = new signalR.HubConnectionBuilder()
        .withUrl(this.signalingServer + (this.emulationType === 'callbar' ? '/recording' : '/liveMonitoring'))
        .configureLogging(signalR.LogLevel.Debug)
        //.withHubProtocol(new MessagePackHubProtocol())
        //.withAutomaticReconnect()
        .build();

      try {
        await this.connection.start();
        console.log("Connected to Signaling Server");

        this.connection.on("AddRemoteIceCandidate", this.addRemoteIceCandidate);
        this.connection.on("processAnswer", this.processAnswer);
        this.connection.on("processOffer", this.processOffer);
        this.connection.on("Pong", () => console.log("Pong"));

        await this.connection.invoke("Ping");
        await this.startIceNegotiation(stream);
        resolve();
      } catch (e) {
        console.error("Error with Signaling Server", e);
        reject();
      }
    });
  };

  public startRecording = async () => {
    await this.connection!.invoke("Start");
  };

  public stopRecording = async () => {
    await this.connection!.invoke("Stop");
  };

  private startIceNegotiation = async (stream: MediaStream) => {
    const config = {
      iceServers: this.stunOrTurn.split("\n").map((s) => {
        return { urls: s, credential: this.TurnPassword, username: this.turnUser };
      }),
      // sdpSemantics: "unified-plan",
    };

    console.log("Starting ICE negotiation with ", config);
    console.log("TRACKs", stream.getTracks());
    this.rtcPeerConnection = new RTCPeerConnection(config);
    this.rtcPeerConnection.ontrack = this.onTrack;
    this.rtcPeerConnection.addEventListener(
      "track",
      (e) => {
        this.onTrack(e);
      },
      false
    );
    this.rtcPeerConnection.addTrack(stream.getTracks()[0]);
    this.rtcPeerConnection.onicecandidate = (event) => {
      console.log("onicecandidate", event.candidate);
      if (event.candidate && event.candidate.type === "relay") {
        this.connection?.invoke(
          "AddIceCandidate",
          JSON.stringify(event.candidate)
        );
      }
    };

    const offer = await this.rtcPeerConnection.createOffer(/*{
      offerToReceiveAudio: true,
      offerToReceiveVideo: true,
    }*/);

    await this.rtcPeerConnection.setLocalDescription(offer);
    await this.connection?.invoke("AddOffer", offer.sdp);
  };

  private addRemoteIceCandidate(candidate: string) {
    console.log("addRemoteIceCandidate ", candidate);
    // TODO arriving string, check if need to be an object instead
    this.rtcPeerConnection?.addIceCandidate(
      new RTCIceCandidate(JSON.parse(candidate))
    );
  }

  private processAnswer = async (sdpAnswer: string) => {
    console.log("addRemoteSdp ", sdpAnswer);
    await this.rtcPeerConnection?.setRemoteDescription(
      new RTCSessionDescription({ type: "answer", sdp: sdpAnswer })
    );

    // var stream = this.rtcPeerConnection.trac?.()[0]
    // this.onRemoteTrack!(stream);
  };

  private processOffer = async (sdpOffer: string) => {
    // console.log("addRemoteSdp ", sdpOffer);
    // await this.rtcPeerConnection?.setRemoteDescription(
    //   new RTCSessionDescription({ type: "answer", sdp: sdpAnswer })
    // );

    // await this.connection?.invoke("AddAnswer", offer.sdp);
  };

  private onTrack = (event: RTCTrackEvent) => {
    console.log("AppStore.onTrack", event);
    this.onRemoteTrack!(event.streams[0]);
  };
}

export default new AppStore();
