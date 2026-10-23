using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
namespace JouchoPet
{
    static class ApiClient
    {
        public static Uri CompletionUri(string endpoint)
        {
            Uri uri;
            if(!Uri.TryCreate(endpoint.Trim(),UriKind.Absolute,out uri))throw new ArgumentException("请输入完整 API 地址，例如 https://api.openai.com/v1");
            if(uri.UserInfo.Length>0 || uri.Query.Length>0 || uri.Fragment.Length>0)throw new ArgumentException("API 地址不能包含账号、查询参数或片段。");
            if(uri.Scheme!="https" && !(uri.Scheme=="http" && uri.IsLoopback))throw new ArgumentException("远程 API 必须使用 HTTPS；本机 localhost 服务可以使用 HTTP。");
            string path=uri.AbsoluteUri.TrimEnd('/');
            if(!uri.AbsolutePath.TrimEnd('/').EndsWith("/chat/completions",StringComparison.OrdinalIgnoreCase))path+="/chat/completions";
            return new Uri(path);
        }
        public static async Task<Dictionary<string,object>> Complete(ApiSettings settings,List<Dictionary<string,object>> messages,object[] tools,CancellationToken token)
        {
            Uri endpoint=CompletionUri(settings.Endpoint);
            if(string.IsNullOrWhiteSpace(settings.Model))throw new ArgumentException("请先在 API 设置中填写服务商提供的模型名。");
            var payload=new Dictionary<string,object>
            {
                {
                    "model",settings.Model
                },
                {
                    "messages",messages
                },
                {
                    "stream",false
                }
            };
            if(tools!=null && tools.Length>0)
            {
                payload["tools"]=tools;
                payload["tool_choice"]="auto";
            }
            ServicePointManager.SecurityProtocol|=SecurityProtocolType.Tls12;
            using(var handler=new HttpClientHandler
            {
                AllowAutoRedirect=false
            })
            using(var client=new HttpClient(handler))
            using(var timeout=CancellationTokenSource.CreateLinkedTokenSource(token))
            {
                client.Timeout=Timeout.InfiniteTimeSpan;
                timeout.CancelAfter(TimeSpan.FromMinutes(3));
                using(var request=new HttpRequestMessage(HttpMethod.Post,endpoint))
                {
                    if(!string.IsNullOrWhiteSpace(settings.Key)) request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",settings.Key.Trim());
                    request.Content=new StringContent(Json.Encode(payload),Encoding.UTF8,"application/json");
                    using(var response=await client.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,timeout.Token))
                    {
                        if((int)response.StatusCode>=300 && (int)response.StatusCode<400)throw new InvalidOperationException("API 返回重定向。请填写最终地址；应用不会把密钥转发到重定向站点。");
                        byte[] data;
                        using(var input=await response.Content.ReadAsStreamAsync()) using(var output=new MemoryStream())
                        {
                            byte[] buffer=new byte[8192];
                            int n;
                            while((n=await input.ReadAsync(buffer,0,buffer.Length,timeout.Token))>0)
                            {
                                if(output.Length+n>2000000)throw new InvalidOperationException("API 返回内容超过 2 MB，请缩短请求。");
                                output.Write(buffer,0,n);
                            }
                            data=output.ToArray();
                        }
                        string body=Encoding.UTF8.GetString(data);
                        if(!response.IsSuccessStatusCode)
                        {
                            string detail=body;
                            try
                            {
                                detail=Json.Text(Json.Object(body)["error"] as Dictionary<string,object>,"message",body);
                            }
                            catch(Exception)
                            {
                            }
                            if(settings.Key.Length>0)detail=detail.Replace(settings.Key,"[密钥已隐藏]");
                            if(detail.Length>800)detail=detail.Substring(0,800);
                            throw new InvalidOperationException("API 错误 "+(int)response.StatusCode+"："+detail);
                        }
                        var parsed=Json.Object(body);
                        var choices=Json.Array(parsed,"choices");
                        if(choices.Length==0)throw new InvalidOperationException("接口未返回 choices；请确认这是 Chat Completions 兼容接口。");
                        var choice=choices[0] as Dictionary<string,object>;
                        if(choice==null || !choice.ContainsKey("message"))throw new InvalidOperationException("接口返回缺少 message。");
                        var message=choice["message"] as Dictionary<string,object>;
                        if(message==null)throw new InvalidOperationException("接口的 message 格式不正确。");
                        return message;
                    }
                }
            }
        }
    }
}
