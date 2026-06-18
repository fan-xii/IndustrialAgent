# 工业上位机研发助手 Agent

> 代码 + 文档双知识库的垂直领域 AI 研发助手
> ASP.NET Core 8 + Semantic Kernel + GLM-5.2 [1M] + Qdrant + Roslyn + React

## 项目简介

面向工业软件团队的垂直领域代码研发助手，解决工业上位机开发中"协议复杂、规范多、新人上手慢、查历史代码耗时"的痛点。

### 五大核心能力

1. **整仓代码智能索引与问答** — C#/WPF/ASP.NET 全量解析，按模块/类/函数切片向量化
2. **工业协议文档 RAG** — Modbus、SECS/GEM、S7、EtherNet/IP 协议 PDF/Word 导入问答
3. **规范化代码生成** — 遵循 Prism MVVM 架构，生成 ViewModel/服务/单元测试
4. **日志故障排查** — 分析运行日志/异常堆栈，定位根因，给出修复方案
5. **项目脚手架一键生成** — 输入业务需求，自动生成模块目录结构与基础类文件

## 技术栈

| 层级 | 技术 | 版本 |
|------|------|------|
| 后端 | ASP.NET Core 8 Web API | net8.0 |
| Agent 核心 | Semantic Kernel | 1.21.1 |
| 大模型 | GLM-5.2 [1M]（智谱 OpenAI 兼容端点） | glm-5.2 |
| 向量检索 | Qdrant | 1.12.0 |
| 代码解析 | Roslyn | 4.11.0 |
| PDF 解析 | PdfPig | 0.1.10 |
| Word 解析 | OpenXml SDK | 3.2.0 |
| 前端 | React 18 + Vite + TypeScript | 18.x |
| 样式 | Tailwind CSS | 3.4.x |
| 代码编辑器 | Monaco Editor | 4.6.x |

## 项目结构

```
VibeProj2/
├── IndustrialAgent.slnx              # .NET 解决方案
├── src/
│   ├── IndustrialAgent.Api/          # ASP.NET Core Web API（控制器、DI 配置）
│   ├── IndustrialAgent.Core/         # Agent 核心（Semantic Kernel 插件、提示词）
│   ├── IndustrialAgent.Indexing/     # 代码/文档索引（Roslyn、Qdrant、PdfPig）
│   ├── IndustrialAgent.Shared/       # 共享模型、架构规范、配置
│   └── IndustrialAgent.Scaffolding/  # 脚手架模板引擎
├── web/                              # React 前端
├── docker-compose.yml                # Qdrant 一键启动
└── README.md
```

## 快速开始

### 1. 环境要求

- .NET 8 SDK
- Node.js 18+
- Docker（用于运行 Qdrant）
- Visual Studio Build Tools（Roslyn MSBuildWorkspace 需要）

### 2. 启动 Qdrant 向量数据库

```bash
docker-compose up -d
```

Qdrant 将在 `localhost:6333`（HTTP）和 `localhost:6334`（gRPC）启动。

### 3. 配置 GLM API Key

编辑 `src/IndustrialAgent.Api/appsettings.json`，填入智谱 API Key：

```json
{
  "ZhipuAI": {
    "ApiKey": "你的智谱API Key"
  }
}
```

或通过环境变量注入（推荐）：

```bash
# PowerShell
$env:ZhipuAI__ApiKey = "你的智谱API Key"
```

### 4. 启动后端 API

```bash
cd src/IndustrialAgent.Api
dotnet run
```

API 默认运行在 `http://localhost:5000`，Swagger 文档在 `http://localhost:5000/swagger`。

### 5. 启动前端

```bash
cd web
npm install
npm run dev
```

前端默认运行在 `http://localhost:5173`。

## 使用指南

### 代码问答

1. 调用 `POST /api/index/project` 索引你的 .sln 或 .csproj 文件
2. 在前端"代码问答"页面输入问题，如"报警逻辑在哪实现"

### 协议文档 RAG

1. 在"协议文档"页面上传 PDF/Word 协议文档并选择协议类型
2. 输入问题，如"读保持寄存器功能码 03 的报文格式"

### 代码生成

1. 在"代码生成"页面选择类型（ViewModel/Service/UnitTest）
2. 输入模块名和业务需求，点击生成

### 日志排查

1. 在"日志排查"页面粘贴运行日志和异常堆栈
2. 点击"开始诊断"获取四段式诊断报告

### 项目脚手架

1. 在"项目脚手架"页面输入业务需求
2. 查看生成的方案预览
3. 输入目标目录路径，执行生成

## API 端点

| 方法 | 路径 | 说明 |
|------|------|------|
| POST | `/api/index/project` | 索引解决方案/项目 |
| GET | `/api/index/status` | 查询索引状态 |
| POST | `/api/codeqa/ask` | 代码问答 |
| POST | `/api/docrag/upload` | 上传协议文档 |
| POST | `/api/docrag/ask` | 协议文档问答 |
| POST | `/api/codegen/generate` | 生成代码 |
| POST | `/api/logdiagnose/analyze` | 日志故障诊断 |
| POST | `/api/scaffold/plan` | 生成脚手架方案 |
| POST | `/api/scaffold/execute` | 执行脚手架生成 |
