export default class BrowserScreenCapture {
  public static async getStream() {
    try {
      //@ts-ignore next-line
      return navigator.mediaDevices.getDisplayMedia({
        // video: { width: 640, height: 320 },
        video: true,
        audio: false,
      });
    } catch (err) {
      console.error("Could not get media stream", err);
      return null;
    }
  }
}
