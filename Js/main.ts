import { JsIndex } from './src/JsIndex';
import * as cs from 'csharp';

class Main {
    public canvas: cs.UnityEngine.GameObject;
    public jsIndex: JsIndex;
    constructor(canvas: cs.UnityEngine.GameObject) {
        this.canvas = canvas;
        console.log(canvas.name);
    }
}

export function init(jsEnvMgr: cs.JsEnvManager) {
    new Main(jsEnvMgr.canvas);
}
