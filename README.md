# HTTPBlocker
拦截含有指定HOST、PATH的HTTP流量：HTTPBlocker

### 规则格式
``` json
{
  "host": [
    {
      "*.sample*.com": { //支持用*作为占位符匹配任意字符
        "allow": false,  //为false则拦截HOST所有请求，
        "path": [
          ""
        ]
      }
    },
    {
      "*.demo.com": {    //支持用*作为占位符匹配任意字符
        "allow": true,   //为true则只拦截匹配PATH的请求
        "path": [
          "abc",
          "*/abc",
          "abc/*"
        ]
      }
    }
]
```
使用系统代理，无法与其他系统代理软件共同使用，如Clash、v2ray等
