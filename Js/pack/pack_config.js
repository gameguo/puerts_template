
var config = {
    rootPath: '../',
    delFolders: ['node_modules/.bin'], // 打包完成后自动删除的文件夹列表
    delFiles: [ // 打包完成后自动删除的文件列表  -- npm run build才会删除
        'package.json',
        'package-lock.json',
        'tsconfig.json'
    ],
};

module.exports = config;
