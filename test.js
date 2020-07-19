const http = require('http');
const cp = require('child_process');
const path = require('path');

// const handel = cp.spawn(path.join(__dirname, 'dist/NodeAddons.exe'));
const handel = cp.spawn(path.join(__dirname, 'dist/NodeAddons_WithConsole.exe'));
handel.stdout.on('data', chunk => {
  const str = chunk.toString();
  // 约定 ##数据## 的字符串为通讯数据
  let res = str.match(/##([\S\s]*)##/g);
  if (!Array.isArray(res)) return;
  res = res[0].match(/(?<=(\?))(.*)(?=(\sHTTP\/1.1))/);
  if (!Array.isArray(res)) return;
  console.log('[stdout queryString]', res[0]);
});

function query(param, cb) {
  http.get(`http://127.0.0.1:8899/?${(new URLSearchParams(param)).toString()}`,
    res => {
      res.on('data', chunk => {
        cb(chunk.toString());
      });
    });
}

query({ name: 'anan', age: 29, time: Date.now() }, httpRawTxt => {
  console.log('[http response]', httpRawTxt);
});

// 监听 Ctrl + c
process.on('SIGINT', () => {
  handel.kill();
  process.exit(0);
});
