
// process.on('data', chunk => {
//   console.log('== a2.js ==', chunk.toString(), '++++')
// })

process.stdin.on('data', chunk => {
  console.log('== 2.js ==', chunk.toString())
})

console.log('== 2.js ==', '222222222');

// setTimeout(() => {}, 1000 * 5)
