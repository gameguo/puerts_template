// pack
const fs = require('fs');
const path = require('path');

const utils = require('./pack_utils');
const config = require('./pack_config');

const isDev = !utils.isArg('-build'); // 是否是开发环境打包
const isNode = !utils.isArg('-nonode'); // 是否打包nodemodule

const tsconfig = require(path.join(config.rootPath, 'tsconfig.json'));

const inputPath = path.join(__dirname, config.rootPath);
const outputPath = path.join(__dirname, config.rootPath, tsconfig.compilerOptions.outDir); // 输出路径

// console.log(tsconfig);

function copyFile(filePath, toPath) {
    const dir = path.dirname(toPath);
    utils.createDir(dir);
    fs.copyFileSync(filePath, toPath);
}

// 打包
function pack() {
    const codeExtension = ['.js', '.jsx', '.ts', '.tsx'];
    const outputFolder = outputPath;
    const inputFolder = inputPath;
    utils.clearDir(outputFolder, isNode ? null : ['node_modules']);
    utils.createDir(outputFolder);
    console.log('\n------------ start tsc build : ------------\n');
    utils.runCmd('tsc', path.join(inputFolder, 'node_modules/.bin'));
    utils.forEachDir(inputFolder, false, tsconfig.exclude, file => {
        if (file == 'tsconfig.json') return;
        let ext = path.extname(file);
        if (codeExtension.indexOf(ext) < 0) {
            copyFile(path.join(inputFolder, file), path.join(outputFolder, file));
        }
    });
    console.log('\n------------ tsc build success ------------\n');

    if (isNode || !fs.existsSync(path.join(outputFolder, 'node_modules'))) {
        console.log('------------ start install npm dependent : ------------\n');
        // npm install --production
        utils.runCmd('npm install --production', outputFolder);
        console.log('------------ install npm dependent success ! ------------\n');
    }

    if (!isDev) {
        // console.log('------------ clear build temp : ------------\n');
        const delFolders = config.delFolders;
        let delFiles = config.delFiles;
        for (let i = 0; i < delFolders.length; i++) {
            utils.delDir(path.join(outputFolder, delFolders[i]));
        }
        for (let i = 0; i < delFiles.length; i++) {
            const file = path.join(outputFolder, delFiles[i]);
            if (fs.existsSync(file) && fs.statSync(file).isFile()) {
                fs.unlinkSync(file);
            }
        }
        // console.log('------------ clear build temp success ! ------------\n');
    }
    console.log('------------ pack success ! ------------\n');
}

pack()
