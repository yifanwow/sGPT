# sGPT
A C# Program which using openai to help machine understand the issue quickly


## How to start
`dotnet add package MouseKeyHook --version 5.6.0`  
`dotnet add package System.Net.Http`  
`dotnet add package Newtonsoft.Json`  
`dotnet add package Markdig`  
`dotnet add package DotNetEnv`  
dotnet add package Microsoft.Web.WebView2 --version 1.0.2478.35
`dotnet add package Microsoft.Web.WebView2`
# 创建Assets文件夹，如果还没有的话
New-Item -ItemType Directory -Force -Path .\Assets

# 下载Highlight.js JavaScript文件
Invoke-WebRequest -Uri https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.3.1/highlight.min.js -OutFile .\Assets\highlight.min.js

# 下载Atom One Dark样式文件
Invoke-WebRequest -Uri https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.3.1/styles/atom-one-dark.min.css -OutFile .\Assets\atom-one-dark.min.css


dotnet list package
