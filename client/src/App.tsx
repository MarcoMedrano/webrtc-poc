import React from "react";
import {
  Theme,
  createStyles,
  withStyles,
  WithStyles,
  Button,
  RadioGroup,
  FormControlLabel,
  Radio,
  Grid,
} from "@material-ui/core";

import { TextField } from "@material-ui/core";
import { observer } from "mobx-react";
import AppStore from "./AppStore";
import isElectron from "is-electron";

import Timer from "react-compound-timer";

const styles = ({ spacing, palette }: Theme) =>
  createStyles({
    root: {
      position: "relative",
      display: "inline-block",
    },
    video: {
      width: "100%!important",
    },
    toolbar: {
      top: 10,
      left: 10,
      position: "absolute",
      cursor: "pointer",
      zIndex: 99999,
    },
    td: {
      backgroundColor: "#08314DC0!important",
      color: "white!important",
    },
  });

interface AppProps extends WithStyles<typeof styles> {}

@observer
class App extends React.Component<AppProps> {
  private localVideo: HTMLVideoElement | null = null;
  private remoteVideo: HTMLVideoElement | null = null;

  public async componentDidMount() {
    await AppStore.connect();
  }

  public render() {
    return (
      <div className="App">
        <RadioGroup
          value={AppStore.emulationType}
          onChange={async (e) => {
            AppStore.emulationType = e.target.value;
            await AppStore.disconnect()
            await AppStore.connect()
          }}
        >
          <FormControlLabel
            value="callbar"
            control={<Radio />}
            label="Callbar"
          />
          <FormControlLabel
            value="live_monitoring"
            control={<Radio />}
            label="Live Monitoring"
          />
        </RadioGroup>
        <br />
        <h3>STUN/TURN</h3>
        <Grid container>
          <TextField
            style={{ padding: 8 }}
            label="Url"
            variant="outlined"
            value={AppStore.stunOrTurn}
            onChange={(e) => {
              AppStore.stunOrTurn = e.target.value;
            }}
          />
          <TextField
            style={{ padding: 8 }}
            label="user"
            variant="outlined"
            value={AppStore.turnUser}
            onChange={(e) => {
              AppStore.turnUser = e.target.value;
            }}
          />
          <TextField
            style={{ padding: 8 }}
            label="password"
            variant="outlined"
            value={AppStore.TurnPassword}
            onChange={(e) => {
              AppStore.TurnPassword = e.target.value;
            }}
          />
        </Grid>
        <h3>SIGNALING</h3>
        <TextField
          label="Url"
          variant="outlined"
          value={AppStore.signalingServer}
        />
        <br />
        <h1>
          <Timer
            startImmediately={false}
            formatValue={(value) => `${value < 10 ? `0${value}` : value}`}
          >
            {(timer: any) => (
              <React.Fragment>
                <div>
                  {/* <Timer.Hours />:<Timer.Minutes />:<Timer.Seconds /> */}
                </div>
                <Button
                  style={{ margin: 4 }}
                  variant="contained"
                  color="primary"
                  onClick={async () => {
                    AppStore.onRemoteTrack.sub((stream: MediaStream) => {
                      console.log('Presenting remote track', stream);
                      
                      this.remoteVideo!.srcObject = stream;
                      this.remoteVideo!.onloadedmetadata = (e) => {
                        this.remoteVideo!.play();
                      };
                      this.forceUpdate();
                    });
                
                    await this.setupStream();
                  }}
                >
                  SETUP
                </Button>
                <Button
                  style={{ margin: 4 }}
                  variant="contained"
                  color="primary"
                  onClick={async () => {
                    timer.start();

                    await AppStore.startPeerConnection();
                  }}
                >
                  CONNECT
                </Button>
                {AppStore.isCallbar && (
                  <>
                    <Button
                      style={{ margin: 4 }}
                      variant="contained"
                      color="primary"
                      onClick={async () => {
                        AppStore.startRecording();
                      }}
                    >
                      START RECORDING
                    </Button>
                    <Button
                      style={{ margin: 4 }}
                      variant="contained"
                      color="primary"
                      onClick={async () => {
                        timer.stop();
                        AppStore.stopRecording();
                      }}
                    >
                      STOP RECORDING
                    </Button>
                  </>
                )}
                <br />
              </React.Fragment>
            )}
          </Timer>
        </h1>
        <br />
        <br />
        {AppStore.isCallbar && (
          <>
            LOCAL
            <br />
            <video style={{height:300}} ref={(video) => (this.localVideo = video)} autoPlay />
            <br />
          </>
        )}
        REMOTE
        <br />
        <video style={{height:300}} ref={(video) => (this.remoteVideo = video)} autoPlay />
      </div>
    );
  }

  private async setupStream(){
    let ScreenCapture = null;
    console.log("Is Electron ", isElectron());

    if (isElectron()) {
      ScreenCapture = require("./ElectronScreenCapture")
        .default;
    } else {
      ScreenCapture = require("./BrowserScreenCapture").default;
    }

    const stream = await ScreenCapture.getStream();

    if (AppStore.isCallbar) this.localVideo!.srcObject = stream;

    AppStore.stream = stream;
  }
}

export default withStyles(styles)(App);
