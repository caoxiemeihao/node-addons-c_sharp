/**
 * 通过 stdio 和 C# 实现通讯 
 */
const path = require('path');
const cp = require('child_process');

const handle = cp.spawn(path.join(__dirname, 'dist/NodeAddon_WithStdio.exe'));

handle.stdout.on('data', chunk => {
  console.log('<<', chunk.toString());
});

const msg = 'Hello C#';
console.log('>> node 发送数据:', msg);

handle.stdin.write(`${msg}\n`, error => {
  if (error) {
    console.log(error);
  }
});
