import { observable } from "mobx";
import Peer from "peerjs";

class AppStore {

  @observable public connected = false;
  @observable public stunList = `stun:stun.l.google.com:19302`
    + `\nstun1.l.google.com:19302`;

  @observable public signalingServer = "https://localhost:9000/myapp";

  @observable public uiMessages = observable([]) as any;
  @observable communicationValues = observable(['chat', 'screen', 'call']) as any;

  public connect = (): Promise<void> => {
    return new Promise<void>((resolve, reject) => {

      const signalingServerUrl = new URL(this.signalingServer);
      const config = {
        iceServers: this.stunList.split('\n').map(s => { return { urls: s } }),
        // sdpSemantics: "unified-plan",
      }

      console.log('Will connect using config ', config);

      const peer = new Peer("agent", {
        host: signalingServerUrl.hostname,
        port: parseInt(signalingServerUrl.port),
        path: signalingServerUrl.pathname,
        config,
      });

      peer.on('open', () => {
        console.log('Connected to Signaling Server')
        resolve();
      })

      peer.on('error', (e) => {
        console.error('Error with Signaling Server', e)
        reject();
      })

    })
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