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
// import isElectron from 'is-electron';

import { desktopCapturer, remote } from "electron";
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
  private videoRef: HTMLVideoElement | null = null;
  private peerVideoRef: HTMLVideoElement | null = null;

  public render() {
    return (
      <div className="App">
        <RadioGroup
          name="emulationType"
          value={AppStore.emulationType}
          onChange={(e) => {
            AppStore.emulationType = e.target.value;
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
            value={AppStore.user}
            onChange={(e) => {
              AppStore.user = e.target.value;
            }}
          />
          <TextField
            style={{ padding: 8 }}
            label="password"
            variant="outlined"
            value={AppStore.password}
            onChange={(e) => {
              AppStore.password = e.target.value;
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
                    timer.start();
                    const sources = await desktopCapturer.getSources({
                      types: ["screen"],
                    });
                    const display = remote.screen.getPrimaryDisplay();
                    const source = sources.find((s: any) =>
                      s.id.includes(display.id)
                    );

                    console.log("CLICK", source);
                    const constrains = {
                      audio: false,
                      video: {
                        mandatory: {
                          chromeMediaSource: "desktop",
                          chromeMediaSourceId: source!.id,
                          maxWidth: display.bounds.width * 0.25,
                          maxHeight: display.bounds.height * 0.25,
                          maxFrameRate: 5,
                          minFrameRate: 1,
                        },
                      },
                    };
                    // const stream = await navigator.mediaDevices.getDisplayMedia({audio:false});
                    const stream = await navigator.mediaDevices.getUserMedia(
                      // @ts-ignore next-line
                      constrains
                    );

                    this.videoRef!.srcObject = stream;
                    AppStore.connect(stream);
                    AppStore.onRemoteTrack = (stream: MediaStream) => {
                      this.peerVideoRef!.srcObject = stream;
                    };
                  }}
                >
                  CONNECT
                </Button>
                {AppStore.emulationType === "callbar" && (
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
        {AppStore.emulationType === "callbar" && (
          <>
            LOCAL
            <br />
            <video ref={(video) => (this.videoRef = video)} autoPlay />
            <br />
          </>
        )}
        REMOTE
        <br />
        <video ref={(video) => (this.peerVideoRef = video)} autoPlay />
      </div>
    );
  }
}

export default withStyles(styles)(App);
