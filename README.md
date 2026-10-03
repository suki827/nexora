# Nexora

Nexora 是一个智能媒体处理平台。

项目已包含 PostgreSQL 持久化、媒体目录 API、Cookie 身份认证、本地分片上传、Worker 中的媒体元数据和音频峰值处理、Task 技术元数据执行流程，以及使用 Blazor 和 MudBlazor 的管理工作台。具体配置与接口见 [媒体上传与分析说明](docs/media-pipeline.md)、[Task 模块说明](docs/task-module.md) 和 [管理工作台说明](docs/web-workspace.md)。视频语义理解与 AI 内容分析仍待实现。

## 技术栈

- .NET 10
- C#
- ASP.NET Core Web API
- .NET Worker Service
- xUnit
- Blazor Web App
- MudBlazor

## 目录结构

```text
nexora/
├─ src/
│  ├─ Nexora.Api/
│  ├─ Nexora.Web/
│  ├─ Nexora.Worker/
│  ├─ Nexora.Domain/
│  ├─ Nexora.Application/
│  ├─ Nexora.Infrastructure/
│  ├─ Nexora.Contracts/
│  └─ Nexora.Agent/
├─ tests/
│  ├─ Nexora.Domain.Tests/
│  ├─ Nexora.Application.Tests/
│  ├─ Nexora.IntegrationTests/
│  ├─ Nexora.Agent.Tests/
│  └─ Nexora.Agent.EvaluationTests/
├─ infrastructure/
├─ docker/
├─ docs/
├─ Nexora.sln
├─ Directory.Build.props
├─ Directory.Packages.props
└─ global.json
```

## `src` 中的项目

### Nexora.Domain

路径：`src/Nexora.Domain`

领域核心层，保存 Nexora 最稳定的业务概念与规则。

适合放置：

- 实体和值对象
- 领域枚举
- 领域规则
- 与技术实现无关的领域异常

不应放置 Controller、数据库访问、EF Core、HTTP 客户端、日志实现或文件存储实现。

该项目不依赖仓库中的其他项目，尤其不能依赖 Application、Infrastructure 或 ASP.NET Core。

### Nexora.Application

路径：`src/Nexora.Application`

应用层，描述系统可以完成哪些用例，并负责协调领域对象。

适合放置：

- 应用服务和用例
- Command、Query 及处理逻辑
- 输入验证
- 供 Infrastructure 实现的接口
- 用例返回模型

它只依赖 `Nexora.Domain`，不能依赖 `Nexora.Infrastructure`、EF Core 或具体数据库驱动。

### Nexora.Infrastructure

路径：`src/Nexora.Infrastructure`

基础设施层，实现 Application 定义的技术接口，并负责连接外部资源。

未来可以放置：

- 数据持久化实现
- 文件或对象存储实现
- 第三方 API 客户端
- 消息基础设施实现
- 基础设施依赖注入配置

它可以依赖 `Nexora.Application` 和 `Nexora.Domain`。当前已包含 EF Core、PostgreSQL 映射与媒体目录 Repository。

### Nexora.Contracts

路径：`src/Nexora.Contracts`

契约层，保存不同入口或进程之间共享的稳定数据结构。

未来可以放置：

- API 请求和响应 DTO
- Worker 消息契约
- 对外事件契约
- 契约相关的枚举

这里不应包含业务逻辑、数据库实体或基础设施实现。独立的 Contracts 项目可以避免 Api、Worker 和 Agent 互相直接引用。

### Nexora.Api

路径：`src/Nexora.Api`

ASP.NET Core Web API，是系统面向 HTTP 客户端的入口。

主要职责：

- 接收和验证 HTTP 请求
- 调用 Application 中的用例
- 将应用结果转换成 HTTP 响应
- 配置依赖注入和中间件
- 提供健康检查和 OpenAPI 文档

Controller 只负责 HTTP 边界，不应包含业务逻辑或直接访问数据库。

它可以依赖：

- `Nexora.Application`
- `Nexora.Infrastructure`
- `Nexora.Contracts`

### Nexora.Worker

路径：`src/Nexora.Worker`

独立后台服务，承载不适合放在 HTTP 请求生命周期内的后台工作。

未来可以负责：

- 后台任务调度
- 持续运行的处理流程
- 消费已定义的任务消息
- 调用 Application 中的用例
- 响应取消信号并优雅停止

Worker 负责宿主、调度和生命周期管理，不应承载领域规则。

它可以依赖：

- `Nexora.Application`
- `Nexora.Infrastructure`
- `Nexora.Contracts`

当前 Worker 会轮询待处理的媒体元数据，调用 FFmpeg 工具提取技术信息和音频峰值。

### Nexora.Agent

路径：`src/Nexora.Agent`

Agent 边界层，为未来的智能编排能力预留独立模块，避免 Agent 代码散落到 Api、Application 或 Domain。

未来可以放置：

- Agent 用例编排
- Agent 输入和输出转换
- Agent 行为抽象
- 对 Application 用例的协调调用

它可以依赖 `Nexora.Application` 和 `Nexora.Contracts`，但不能直接依赖 `Nexora.Infrastructure`。

当前阶段不实现真实 AI Agent、模型 Provider 或提示词工作流。

## `tests` 中的项目

### Nexora.Domain.Tests

路径：`tests/Nexora.Domain.Tests`

测试领域对象和纯业务规则，只依赖 `Nexora.Domain`。这些测试应快速、稳定，并且不访问数据库、网络或文件系统。

### Nexora.Application.Tests

路径：`tests/Nexora.Application.Tests`

测试应用用例、流程协调和应用层验证，只依赖 `Nexora.Application`。外部能力应通过 Application 定义的接口进行替代。

### Nexora.IntegrationTests

路径：`tests/Nexora.IntegrationTests`

验证上传存储与媒体解析等模块组合后的行为，并为后续 API 端到端验证预留位置。

### Nexora.Agent.Tests

路径：`tests/Nexora.Agent.Tests`

测试 Agent 模块中可确定、可重复的编排逻辑。它不负责评价模型回答质量，也不应调用真实付费模型。

### Nexora.Agent.EvaluationTests

路径：`tests/Nexora.Agent.EvaluationTests`

为未来的 Agent 效果评估预留，可用于固定数据集上的质量、稳定性和行为评分。

它与普通单元测试分开，是因为评估测试通常更慢，而且可能需要独立的运行环境。当前阶段只保留项目边界，不实现真实评估。

## Web 管理端

### Nexora.Web

路径：`src/Nexora.Web`

Blazor Razor 类库，包含用户管理工作台的页面、样式和浏览器端分片上传代码。`Nexora.Api` 承载这些页面，与 API 共用站点和 Cookie。入口与功能见 [管理工作台说明](docs/web-workspace.md)。

当前提供媒体资源、上传进度、元数据、波形、分析任务和结果界面。跨用户的平台配置和管理员功能尚未实现。

## 其他目录

- `infrastructure/bicep/modules`：未来可复用的 Azure Bicep 模块。
- `infrastructure/bicep/environments`：未来不同环境的基础设施组合。
- `infrastructure/scripts`：环境、部署和运维辅助脚本。
- `docker`：未来的 Dockerfile 和容器配置。
- `docs`：架构和项目说明文档。
- `docs/adr`：架构决策记录。
- `.github/workflows`：预留的 GitHub Actions 目录；第一阶段不实现 CI/CD。

## 项目依赖关系

| 项目 | 允许依赖 |
| --- | --- |
| `Nexora.Domain` | 无 |
| `Nexora.Application` | `Nexora.Domain` |
| `Nexora.Infrastructure` | `Nexora.Application`、`Nexora.Domain` |
| `Nexora.Contracts` | 无 |
| `Nexora.Web` | Blazor、MudBlazor；通过同源 API 读取业务数据 |
| `Nexora.Api` | `Nexora.Application`、`Nexora.Infrastructure`、`Nexora.Web` |
| `Nexora.Worker` | `Nexora.Application`、`Nexora.Infrastructure`、`Nexora.Contracts` |
| `Nexora.Agent` | `Nexora.Application`、`Nexora.Contracts` |
| `Nexora.Domain.Tests` | `Nexora.Domain` |
| `Nexora.Application.Tests` | `Nexora.Application` |
| `Nexora.IntegrationTests` | `Nexora.Infrastructure`、`Nexora.Worker` |
| `Nexora.Agent.Tests` | `Nexora.Agent` |
| `Nexora.Agent.EvaluationTests` | `Nexora.Agent` |

核心原则：依赖只能从外层指向内层，业务核心不能依赖数据库、Web 框架或第三方服务等具体实现。

## 根目录关键文件

- `Nexora.sln`：聚合所有 .NET 项目，方便统一还原、构建和测试。
- `global.json`：固定仓库使用的 .NET SDK 版本。
- `.gitignore`：排除构建产物、本地配置、缓存和秘密文件。

## 环境要求

- .NET SDK 10
- Git
- PostgreSQL：运行媒体 API 与 Worker 时需要
- FFmpeg（包含 `ffprobe`）：运行媒体分析 Worker 时需要

检查本机环境：

```powershell
dotnet --version
git --version
```

## 还原、构建和测试

在仓库根目录执行：

```powershell
dotnet restore
dotnet build Nexora.sln
dotnet test Nexora.sln
```

## 启动 API 和管理工作台

```powershell
dotnet run --project src/Nexora.Api
```

在 Visual Studio 中只需将 `Nexora.Api` 设为启动项目。打开启动地址的 `/` 进入管理工作台；`Nexora.Web` 无须单独启动。

健康检查端点：

```text
GET /health
```

## 启动 Worker

```powershell
dotnet run --project src/Nexora.Worker
```

使用 `Ctrl+C` 请求 Worker 优雅停止。

## 媒体上传与后台处理

认证、数据库连接、本地存储、分片接口、Worker 启动方式及波形格式见 [媒体上传与分析说明](docs/media-pipeline.md)。管理界面说明见 [管理工作台说明](docs/web-workspace.md)。

## 开发规则

- 修改前阅读 `README.md` 和相关的 `docs` 文档。
- Domain 不得依赖 Infrastructure。
- Application 不得依赖 Infrastructure。
- Agent 不得直接依赖 Infrastructure。
- Controller 中不得加入业务逻辑。
- 不提交密码、令牌、连接字符串或其他 Secret。
- 每个任务只修改明确范围内的内容。
- 每次修改后运行 build 和相关测试。
- 不提前实现未来阶段的功能。
