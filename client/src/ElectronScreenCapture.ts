import { desktopCapturer, remote } from "electron";

export default class ElectronScreenCapture {

    public static async getStream() {
        const sources = await desktopCapturer.getSources({
            types: ["screen"],
        });
        const display = remote.screen.getPrimaryDisplay();
        const source = sources.find((s: any) =>
            s.id.includes(display.id)
        );

        console.log("SOURCE", source);
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
        return await navigator.mediaDevices.getUserMedia(
            // @ts-ignore next-line
            constrains
        );
    }
}