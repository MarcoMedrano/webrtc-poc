import React from "react";
import {
  Accordion,
  AccordionSummary,
  Theme,
  createStyles,
  FormControlLabel,
  withStyles,
  WithStyles,
  Button,
  Typography,
  AccordionDetails,
  Divider,
} from "@material-ui/core";
import ExpandMoreIcon from "@material-ui/icons/ExpandMore";
import { observer } from "mobx-react";
import { TextField } from "@material-ui/core";
import AppStore from "./AppStore";

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
  render() {
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
        <Button
          variant="contained"
          color="primary"
          onClick={() => {
            // window.open("chrome://webrtc-internals", "_blank");
            AppStore.connect();
          }}
        >
          CONNECT
        </Button>
      </div>
    );
  }
}

export default withStyles(styles)(App);
