var utils = {};

const fs = require('fs');
const shell = require('shelljs')
const nodepath = require('path');

utils.isArg = function (argName) {
    var args = process.argv;
    for (var i = 0; i < args.length; i++) {
        let option = args[i];
        if (option.indexOf(argName) == 0) {
            return true;
        }
    }
    return false;
}

utils.clearDir = function (path, ignoreNames) {
    // console.log("clear path : " + path)
    let files = [];
    if (fs.existsSync(path)) {
        if (!fs.statSync(path).isDirectory()) return;
        files = fs.readdirSync(path);
        files.forEach((file, index) => {
            if (ignoreNames != null && ignoreNames.indexOf(file) >= 0) {
                return;
            }
            let curPath = nodepath.join(path, file);
            if (fs.statSync(curPath).isDirectory()) {
                utils.delDir(curPath); //递归删除文件夹
            } else {
                fs.unlinkSync(curPath); //删除文件
            }
        });
    }
}

utils.createDir = function (path) {
    if (!fs.existsSync(path) || !fs.statSync(path).isDirectory()) {
        fs.mkdirSync(path);
    }
}

utils.forEachDir = function (path, isAbs, ignoreNames, callback) {
    function localForEachDir(path, ignoreNames, callback, currentRoot) {
        if (fs.existsSync(path)) {
            const files = fs.readdirSync(path);
            files.forEach((file, index) => {
                if (file[0] == '.') {
                    // console.log("忽略 " + file);
                    return;
                }
                if (currentRoot == '' && ignoreNames.indexOf(file) >= 0) {
                    return;
                }
                let curPath = nodepath.join(path, file);
                let rootPath = nodepath.join(currentRoot, file);
                if (fs.statSync(curPath).isDirectory()) {
                    localForEachDir(curPath, ignoreNames, callback, rootPath);
                } else {
                    callback(isAbs ? curPath : rootPath);
                }
            });
        }
    }
    localForEachDir(path, ignoreNames, callback, '');
}

utils.delDir = function (path) {
    let files = [];
    if (fs.existsSync(path)) {
        files = fs.readdirSync(path);
        files.forEach((file, index) => {
            let curPath = path + "/" + file;
            if (fs.statSync(curPath).isDirectory()) {
                utils.delDir(curPath); //递归删除文件夹
            } else {
                fs.unlinkSync(curPath); //删除文件
            }
        });
        fs.rmdirSync(path);
    }
}

utils.runCmd = function (cmd, path) {
    // console.log(path);
    let currentPath = __dirname;
    shell.cd(path);
    console.log(cmd);
    // let obj =
    shell.exec(cmd);
    shell.cd(currentPath);
}


module.exports = utils;