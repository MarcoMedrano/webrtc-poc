import { observable } from "mobx";
import isElectron from "is-electron";
import { UrRtc } from "ur-rtc";
import { SimpleEventDispatcher } from "strongly-typed-events";
class AppStore {
  private connection: UrRtc;
  private _onRemoteTrack = new SimpleEventDispatcher<MediaStream>();

  @observable public emulationType = isElectron() ? "callbar" : "live_monitoring";
  @observable public connected = false;

  @observable public stunOrTurn = "stun:stun.l.google.com:19302";
  @observable public turnUser: any;
  @observable public TurnPassword: any;
  @observable public signalingServer = "http://localhost:5000";

  // @observable public stunOrTurn = "turn:54.242.2.183:3478";
  // @observable public turnUser = "tdx";
  // @observable public TurnPassword = "1234";
  // @observable public signalingServer = "http://54.211.79.31:5000";

  public get onRemoteTrack() {
    return this._onRemoteTrack.asEvent();
  }

  public set stream(value: MediaStream) {
    this.connection.stream = value;
  }

  public get isCallbar() {
    return this.emulationType === "callbar";
  }

  public connect = (): Promise<void> => {
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

    console.log("Creating ur-rtc with ", config);
    const hub = this.isCallbar ? "recording" : "liveMonitoring";

    this.connection = new UrRtc(
      `${this.signalingServer}/${hub}`,
      config);

    this.connection.onRemoteTrack.sub((stream: MediaStream) => this._onRemoteTrack.dispatch(stream));

    return this.connection.connect();
  };

  public disconnect = async () => {
    return this.connection!.disconnect();
  };

  public startRecording = async () => {
    return this.connection!.startRecording();
  };

  public stopRecording = async () => {
    return this.connection!.stopRecording();
  };

  public startPeerConnection = async () => {
    return this.connection!.startPeerConnection();
  };
}

export default new AppStore();
