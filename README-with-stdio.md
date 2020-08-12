## Node.js 利用 stdio 标准输入/输出实现与 C# 程序通讯

- [完整代码](https://github.com/caoxiemeihao/node-addons-c_sharp)

![stdio 通讯方式](https://raw.githubusercontent.com/caoxiemeihao/node-addons-c_sharp/master/node-addons-with-stdio.png)


之前写过一篇文章 **[使用 C# 开发 node.js 插件](https://www.jianshu.com/p/9ac4f9ef9625)** 中实现原理是通过使用 `C# ` 手写一个 `web服务器` + `Console.WriteLine()` 的形式实现的通讯，也就是 `http + stdout` 组合的方式。

写完文章后我觉得自己是不是有些傻。为啥不用 `stdin + stdout` 纯 `标准输入/输出流` 的方式实现通讯，实现简单不说，还会规避一些可能的隐患；
1. 比如 http 端口可能会被占用！
2. 或者烦人的 `windows defender` 会弹出提示！
![windows defender](https://upload-images.jianshu.io/upload_images/6263326-f08007e48a696ac1.png?imageMogr2/auto-orient/strip%7CimageView2/2/w/1240)

###### 综上所述改进方案使用 stdio —— 简单、粗暴、直接、有效

- 刚好最近又学习了一下 `child_process` 了解到子进程对象接收、输入标准流很方便
- Node.js 端

```javascript
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
```

- C# 端
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading;

/// <summary>
/// 通过 stdio 和 node.js 实现通讯
/// </summary>
namespace NodeAddon_WithStdio
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;

            new Thread(new ThreadStart(StartStdioListener)).Start();
        }

        static void StartStdioListener()
        {
            string strin = Console.ReadLine();

            Console.WriteLine("C# 收到的数据: {0}", strin);
        }

    }
}
```

- 执行下试试
```shell
$ node test-stdio.js
>> node 发送数据: Hello C#
<< C# 收到的数据: Hello C#
```
