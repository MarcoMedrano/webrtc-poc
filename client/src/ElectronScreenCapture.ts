export default class ElectronScreenCapture{

    public static async getStream(){
        //@ts-ignore next-line
        return navigator.mediaDevices.getDisplayMedia({video:true, audio:false});
    }
}