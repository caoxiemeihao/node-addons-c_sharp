const cp = require('child_process');
const path = require('path');

const forked = cp.exec(`node ${path.join(__dirname, 'stdio2.js')}`);

forked.stdout.on('data', chunk => {
  console.log('-- 1.js --', '|', chunk.toString());
});

setInterval(() => {
  forked.stdin.write(Date.now().toString(), error => {
    error && console.log('error', error);
  });
}, 1000)
