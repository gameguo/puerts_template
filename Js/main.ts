import { JsIndex } from './src/JsIndex';
import * as cs from 'csharp';
import * as jsTest from './src/jsTest';

class Main {
    public canvas: cs.UnityEngine.GameObject;
    public jsIndex: JsIndex;
    constructor(canvas: cs.UnityEngine.GameObject) {
        this.canvas = canvas;
        console.log(canvas.name);
        jsTest.test();
    }
}

export function init(jsEnvMgr: cs.JsEnvManager) {
    new Main(jsEnvMgr.canvas);
}
