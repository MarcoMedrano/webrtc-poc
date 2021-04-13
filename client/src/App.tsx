import React from "react";
import {
  Accordion,
  AccordionSummary,
  Theme,
  createStyles,
  withStyles,
  WithStyles,
  Button,
  AccordionDetails,
} from "@material-ui/core";
import ExpandMoreIcon from "@material-ui/icons/ExpandMore";
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
        <h2>CLIENT</h2>
        <Accordion>
          <AccordionSummary expandIcon={<ExpandMoreIcon />}>
            STUN
          </AccordionSummary>
          <AccordionDetails>
            <TextField
              fullWidth
              multiline
              label="Urls"
              variant="outlined"
              value={AppStore.stunList}
              onChange={(e) => {
                console.log("Changing to ", e.target.value);
                AppStore.stunList = e.target.value;
              }}
            />
          </AccordionDetails>
        </Accordion>
        <Accordion>
          <AccordionSummary expandIcon={<ExpandMoreIcon />}>
            TURN
          </AccordionSummary>
          <AccordionDetails></AccordionDetails>
        </Accordion>
        <Accordion>
          <AccordionSummary expandIcon={<ExpandMoreIcon />}>
            Signaling
          </AccordionSummary>
          <AccordionDetails>
            <TextField
              fullWidth
              label="Url"
              variant="outlined"
              value={AppStore.signalingServer}
            />
          </AccordionDetails>
        </Accordion>
        <br />
        <br />
        <h1>
          <Timer
            startImmediately={false}
            formatValue={(value) => `${value < 10 ? `0${value}` : value}`}
          >
            {(timer: any) => (
              <React.Fragment>
                <div>
                  <Timer.Hours />:<Timer.Minutes />:<Timer.Seconds />
                </div>
                <Button
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
                          maxWidth: display.bounds.width * 1,
                          maxHeight: display.bounds.height * 1,
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
                <Button
                  variant="contained"
                  color="primary"
                  onClick={async () => {
                    AppStore.startRecording();
                  }}
                >
                  RECORD
                </Button>
                <Button
                  variant="contained"
                  color="primary"
                  onClick={async () => {
                    timer.stop();
                    AppStore.stopRecording();
                  }}
                >
                  STOP RECORDING
                </Button>
                <br />
              </React.Fragment>
            )}
          </Timer>
        </h1>
        <br />
        <br />
        LOCAL
        <br />
        <video ref={(video) => (this.videoRef = video)} autoPlay />
        <br />
        MIRRORED (REMOTE)
        <br />
        <video ref={(video) => (this.peerVideoRef = video)} autoPlay />
      </div>
    );
  }
}

export default withStyles(styles)(App);
