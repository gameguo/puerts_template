// "build": "node config/ncc.js -build",
// "tsbuild": "tsc -p tsconfig.json"

// https://www.npmjs.com/package/@vercel/ncc
const ncc = require('@vercel/ncc');
const utils = require('./pack_utils');
const path = require('path');
const fs = require('fs');
const config = require('./pack_config');


var inputPath = path.join(__dirname, config.srcPath, config.mainPath);
var outputPath = path.join(__dirname, config.outputPath);

var isBuild = utils.isArg('-build');

let option = {
    // 提供自定义缓存路径或禁用缓存
    cache: ".cache" | false,
    // externals to leave as requires of the build
    externals: ["externalpackage"],
    // directory outside of which never to emit assets
    filterAssetBase: process.cwd(), // default
    minify: isBuild, // default false
    sourceMap: !isBuild, // default false
    sourceMapBasePrefix: '../', // default treats sources as output-relative
    // when outputting a sourcemap, automatically include
    // source-map-support in the output file (increases output by 32kB).
    sourceMapRegister: true, // default
    watch: false, // default
    license: '', // default does not generate a license file
    v8cache: false, // default
    quiet: false, // default
    debugLog: false // default
}

function build() {
    ncc(inputPath, option).then(({ code, map, assets }) => {
        utils.clearDir(outputPath);
        utils.createDir(outputPath);

        let fileName = "index.js"; //path.basename(inputPath);

        let outputJsPath = path.join(outputPath, fileName);
        fs.writeFileSync(outputJsPath, code);

        if (option.sourceMap) {
            let outputSourceMapPath = path.join(outputPath, fileName + ".map");
            fs.writeFileSync(outputSourceMapPath, map);
        }
        for (const key in assets) {
            const element = assets[key];
            let assetPath = path.join(outputPath, key);
            console.log(typeof (element.source));
            fs.writeFileSync(assetPath, element.source);
        }
        // console.log(assets);

        console.log("build success : " + outputJsPath);
        // console.log(code);
        // Assets is an object of asset file names to { source, permissions, symlinks }
        // expected relative to the output code (if any)
    }).catch(function (error) {
        console.log(error);
    });
}

build();
