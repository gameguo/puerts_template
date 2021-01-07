// pack
const fs = require('fs');
const path = require('path');
const parser = require('@babel/parser');  // 解析ast
const traverse = require('@babel/traverse').default; // 遍历ast
const babel = require('@babel/core');
const presetEnv = require('@babel/preset-env');

const utils = require('./pack_utils');
const config = require('./pack_config');

var srcPath = config.srcPath;
var mainPath = path.join(srcPath, config.mainPath);
var outputPath = config.outputPath;
const presetEnvPlugin = [presetEnv, config.presetEnvOption]

let ID = 0;

function createAsset(filename) {
    const filepath = path.join(__dirname, filename);
    if (!fs.existsSync(filepath)) {


        console.log(filepath + " notfound");
        return;
    }
    const content = fs.readFileSync(filepath, 'utf-8');
    const ast = parser.parse(content, {  // 解析出抽象语法树
        sourceType: 'module',
    });
    const dependencies = [];
    traverse(ast, {  // 遍历解析完成的抽象语法树，查找依赖
        ImportDeclaration: ({ node }) => {
            dependencies.push(node.source.value);
        }
    });
    const { code, map } = babel.transformFromAstSync(ast, null, {  // 转换ast代码
        presets: [presetEnvPlugin],
        sourceMaps: true,
        sourceRoot: '',
        // sourceFileName: path.basename(filename),
        // plugins: []
    })
    let id = ID++;
    return {
        id,
        filename,
        code,
        map,
        dependencies,
    }
}

// 创建依赖关系图
function createGraph(entry) {
    const mainAsset = createAsset(entry);
    if (!mainAsset) return;
    const queue = [mainAsset];
    for (const asset of queue) {
        const dirname = path.dirname(asset.filename);
        asset.mapping = {};
        asset.dependencies.forEach(relativePath => {
            const absolutePath = path.join(dirname, relativePath);
            const child = createAsset(absolutePath);
            asset.mapping[relativePath] = child ? child.id : '';
            if (child) {
                queue.push(child);
            }
        });
    }
    return queue;
}

// 打包
function pack(entry) {
    const graph = createGraph(entry);
    if (!graph) return;
    let modules = [];
    graph.forEach(mod => {
        console.log(mod);
    });

    console.log('------------ success ------------');
}

pack(mainPath);
