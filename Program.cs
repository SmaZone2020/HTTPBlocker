using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using SystemHTTPListener;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Models;

namespace SystemProxySetter
{
    class Program
    {
        private static ProxyServer proxyServer;
        private static ExplicitProxyEndPoint explicitEndPoint;

        private static readonly string BlockPageHtml = @"
<!DOCTYPE html>
<html lang=""zh-CN"">
<head>
    <meta charset=""utf-8"">
    <title>访问被拦截</title>
    <style>
        body { font-family: 'Microsoft YaHei', sans-serif; text-align: center; padding: 50px; }
        h1 { color: #d9534f; }
        p { font-size: 18px; }
    </style>
</head>
<body>
    <h1>访问被拦截</h1>
    <p>此网站包含被系统屏蔽的内容。</p>
    <p>如果您认为这是误判，请联系管理员。</p>
</body>
</html>";

        static async Task Main(string[] args)
        {
            Console.WriteLine("=== URL拦截代理服务器 ===");
            Console.WriteLine("1. 启动代理服务器");
            Console.WriteLine("2. 停止代理服务器");
            Console.Write("请选择操作: ");
            UrlBlocker.LoadRules();
            if (!File.Exists("BLOCK.HTML"))
            {
                File.WriteAllText("BLOCK.HTML", BlockPageHtml);
            }
            var choice = Console.ReadLine();

            if (choice == "1")
            {
                await StartProxyServer();
            }
            else if (choice == "2")
            {
                StopProxyServer();
            }
        }

        private static async Task StartProxyServer()
        {
            proxyServer = new ProxyServer();

            proxyServer.CertificateManager.CertificateEngine = Titanium.Web.Proxy.Network.CertificateEngine.DefaultWindows;
            proxyServer.CertificateManager.EnsureRootCertificate();

            proxyServer.BeforeRequest += OnRequest;
            proxyServer.BeforeResponse += OnResponse;

            explicitEndPoint = new ExplicitProxyEndPoint(IPAddress.Any, 8888, true);

            proxyServer.AddEndPoint(explicitEndPoint);
            proxyServer.Start();

            proxyServer.SetAsSystemHttpProxy(explicitEndPoint);
            proxyServer.SetAsSystemHttpsProxy(explicitEndPoint);

            Console.WriteLine($"代理服务器已启动，监听端口 8888");
            Console.WriteLine("已设置系统代理");
            Console.WriteLine("正在拦截包含敏感内容的请求...");

            // 保持运行
            while (true)
            {
                await Task.Delay(1000);
            }
        }

        private static void StopProxyServer()
        {
            if (proxyServer != null)
            {
                proxyServer.DisableSystemHttpProxy();
                proxyServer.DisableSystemHttpsProxy();

                proxyServer.Stop();
                proxyServer.Dispose();

                Console.WriteLine("代理服务器已停止");
                Console.WriteLine("已移除系统代理设置");
            }
        }

        private static async Task OnRequest(object sender, SessionEventArgs e)
        {
            string url = e.HttpClient.Request.RequestUri.ToString();

            if (UrlBlocker.ShouldBlockRequest(url))
            {
                Console.WriteLine($"拦截访问请求: {url}");

                e.Ok(Encoding.UTF8.GetBytes(File.ReadAllText("BLOCK.HTML")),
                    new List<HttpHeader>
                    {
                        new HttpHeader("Content-Type", "text/html; charset=utf-8")
                    });

                return;
            }

            //修改请求
        }

        private static async Task OnResponse(object sender, SessionEventArgs e)
        {
            //修改响应
        }
    }
}