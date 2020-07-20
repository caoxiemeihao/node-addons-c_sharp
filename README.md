## 使用 C# 开发 node.js 插件

- [完整代码](https://github.com/caoxiemeihao/node-addons-c_sharp)

#### 项目需求

  最近在开发一个 electron 程序，其中有用到和硬件通讯部分；硬件厂商给的是 `.dll` 链接库做通讯桥接，
第一版本使用 C 写的 Node.js 扩展 😁；由于有异步任务的关系，实现使用了 N-API 提供的多线程做异步任务调度，
虽然功能实现了，但是也有些值得思考的点。

  - 纯 C 编程效率低，木有 trycatch 的语言调试难度也大 (磕磕绊绊的)
  - 编写好的 .node 扩展文件，放在 electron 主进程中运行会有一定的隐患稍有差错会导致软件闪退 (后来用子进程隔离运行)
  - 基于 N-API 方式去编写 Node.js 插件会显得有所束缚，木有那种随心所欲写 C 的那种“顺畅”；尤其是多线程部分

  综上考虑，加上通讯功能又是调用 `.dll` 文件，索性转战 C#，对于 windows 来说再合适不过了；但是问题是 C# 咋编译到 Node.js 中？
答案是“编译不了”。
  插件实现的功能只是收到命令后调用 `.dll` 去操作硬件，再时时能把结果返回即可。
基于这个需求我们用 C# 去调用 `.dll` 文件，然后再解决派发命令、实时获取结果的通讯问题就OK了，剩下的就都是好处啦

  - C# 编写难度低于 C，又是 windows 亲儿子，基于 `.NET Framework` 编译后的程序仅 19KB (C实现同样功能编出来的.node文件 565KB)
  - 基于 C# 的插件独立于 Node.js 运行环境，程序出了问题不会影响 electron 应用
  - 木有任何的编程束缚，~亲想咋写就咋写

#### 通讯问题
  说这个之前我们还忽略了一个问题，这个 C# 的程序(.exe文件)如果启动？
既然是一个程序(.exe文件)，我们双击即可执行；既然双击即可执行，我们就可以用 `child_process` 模块提供的 
[spawn](https://nodejs.org/dist/latest-v12.x/docs/api/child_process.html#child_process_child_process_spawn_command_args_options) 去拉起程序(代替鼠标双击)；

  好！程序已经启动了，那么该到了如果通讯的环节了。
`spawn` 的执行就是开启了一个单独的进程，通讯问题也就是**进程通讯**问题。之前如果你用过 `spawn` 启动过 Node.js 程序(.js文件)，那么你肯定知道通讯使用 `send` 方法即可；这个是 Node.js 内置的方式

  我们启动的进程是 C# 程序，通讯问题只能我们自己来解决了；进程通讯的方式有好多这里不展开。对于前端(web)攻城狮来讲，我们最熟悉的莫过于 `http` 通讯方式了；就用它！

  - C# 程序端启动开启一个 `http` 服务等待 Node.js 端发送请求过来；根据参数决定要干啥
  - `spawn` 启动的应用(进程)，会返回一个 `ChildProcessWithoutNullStreams` (这个我也不能很明确的理解)；能够接收到标准的 `stdio` 输入/输出
    那我们就利用这点使用 `ChildProcessWithoutNullStreams.stdout.on('data', chunk => console.log(chunk.toString()))` 的方式就可以收到 C# 通过 `stdio` 即 `Console.WriteLine()` 发过来的数据；
    哇！好方便~
  - 可能有人会想到用双工的 `web socket` 实现通讯，很棒！实现方式确实有很多种，这里用 `Console.WriteLine()` 通过标准的 `stdio` 方式实现，算不算是一个开发成本不高的**讨巧做法**呢！

#### 大致流程
![](https://raw.github.com/caoxiemeihao/node-addons-c_sharp/process.png)

#### 开发环境
- C# 代码部分使用 Visual Studio 2017
- test.js 代码部分使用 VsCode

#### 代码实现

- C# 部分

  ```cs
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;

  using System.Net;
  using System.Net.Sockets;
  using System.Threading;
  using System.Text.RegularExpressions;

  namespace NodeAddons
  {
      class Program
      {
          static TcpListener listener;
          static int port = 8899;
          static string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

          static void Main(string[] args)
          {
              listener = new TcpListener(IPAddress.Any, port);
              listener.Start();

              // 启用服务器线程
              new Thread(new ThreadStart(StartServer)).Start();

              Console.WriteLine("Http server run at {0}.", port);
          }

          // Http 服务器
          static void StartServer()
          {
              while(true)
              {
                  // 这里会阻塞线程，直到接受到一个请求
                  Socket socket = listener.AcceptSocket();
                  // 将请求单独开一个线程处理；while(true)会回到等待下一个请求状态，周而复始
                  new Thread(new ParameterizedThreadStart(HandleRequest)).Start(socket);
              }
              
          }

          // 处理一个请求
          static void HandleRequest(object args)
          {
              Socket socket = (Socket)args;
              byte[] receive = new byte[1024];
              socket.Receive(receive, receive.Length, SocketFlags.None);
              string httpRawTxt = Encoding.ASCII.GetString(receive);

              // 通过 stdio(Console.WriteLine) 实现与 node.js 通讯
              // ## 开头、结尾，方便区分这个条输出是给 node.js 通讯用的
              Console.WriteLine("##" + httpRawTxt + "##");
              SendToBrowser(ref socket, now);
          }

          // 发送数据
          static void SendToBrowser(ref Socket socket, string body)
          {
              string header = "HTTP/1.1 200 OK\r\n"
                  + "Content-Type: text/html\r\n"
                  + "Content-Length: " + body.Length + "\r\n"
                  + "Access-Control-Allow-Origin: *\r\n" // 支持跨域
                  + "\r\n"; // 响应头与响应体分界
              byte[] data = Encoding.ASCII.GetBytes(header + body);

              if (socket.Connected)
              {
                  int res = socket.Send(data, data.Length, SocketFlags.None);
                  if (res == -1)
                  {
                      Console.WriteLine("Socket Error cannot Send Packet.");
                  }
                  else
                  {
                      Console.WriteLine(">> [{0}]", now);
                  }
                  socket.Close();
              }
          }
      }
  }
  ```

- Node.js 部分

  ```javascript
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
  ```

#### 测试一下

- 当然程序不会自己停下来哈，毕竟子进程的 http 服务一直在运行！

  ```shell
  $ node test.js
  [stdout queryString] name=anan&age=29&time=1595134635733
  [http response] 2020-07-19 12:57:15
  ```
