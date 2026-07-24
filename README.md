# Nexora

Nexora 是一个智能媒体处理平台。

项目目前处于第一阶段：初始化工程骨架。本阶段只建立清晰的模块边界，使 API、Worker 和 React 管理端能够独立开发、构建和运行，暂不实现数据库、认证、消息队列、AI Provider、AI Agent 或媒体处理业务。

## 技术栈

- .NET 10
- C#
- ASP.NET Core Web API
- .NET Worker Service
- xUnit
- React
- TypeScript
- Vite

## 目录结构

```text
nexora/
├─ src/
│  ├─ Nexora.Api/
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
├─ web/
│  └─ nexora-admin/
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

它可以依赖 `Nexora.Application` 和 `Nexora.Domain`。当前阶段不添加 EF Core、数据库、Repository、Migration 或消息队列。

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

当前阶段只保留可启动、输出状态日志和优雅停止的最小能力。

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

从 `Nexora.Api` 入口验证多个模块组合后的行为，包括路由、Controller、中间件、依赖注入以及 HTTP 请求和响应。

### Nexora.Agent.Tests

路径：`tests/Nexora.Agent.Tests`

测试 Agent 模块中可确定、可重复的编排逻辑。它不负责评价模型回答质量，也不应调用真实付费模型。

### Nexora.Agent.EvaluationTests

路径：`tests/Nexora.Agent.EvaluationTests`

为未来的 Agent 效果评估预留，可用于固定数据集上的质量、稳定性和行为评分。

它与普通单元测试分开，是因为评估测试通常更慢，而且可能需要独立的运行环境。当前阶段只保留项目边界，不实现真实评估。

## Web 管理端

### nexora-admin

路径：`web/nexora-admin`

基于 React、TypeScript 和 Vite 的 Nexora 管理端。

未来用于提供任务管理、运行状态查看和平台配置等界面。当前阶段只提供可独立启动和构建的占位页面，不添加路由框架、API 调用、UI 框架或业务页面。

前端使用 npm 管理依赖，不加入 `.NET` Solution。

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
| `Nexora.Api` | `Nexora.Application`、`Nexora.Infrastructure`、`Nexora.Contracts` |
| `Nexora.Worker` | `Nexora.Application`、`Nexora.Infrastructure`、`Nexora.Contracts` |
| `Nexora.Agent` | `Nexora.Application`、`Nexora.Contracts` |
| `Nexora.Domain.Tests` | `Nexora.Domain` |
| `Nexora.Application.Tests` | `Nexora.Application` |
| `Nexora.IntegrationTests` | `Nexora.Api` |
| `Nexora.Agent.Tests` | `Nexora.Agent` |
| `Nexora.Agent.EvaluationTests` | `Nexora.Agent` |

核心原则：依赖只能从外层指向内层，业务核心不能依赖数据库、Web 框架或第三方服务等具体实现。

## 根目录关键文件

- `Nexora.sln`：聚合所有 .NET 项目，方便统一还原、构建和测试。
- `global.json`：固定仓库使用的 .NET SDK 版本。
- `Directory.Build.props`：集中设置所有 .NET 项目的公共编译选项。
- `Directory.Packages.props`：集中管理 NuGet 包版本。
- `.editorconfig`：统一编辑器和代码格式规则。
- `.gitignore`：排除构建产物、本地配置、缓存和秘密文件。
- `AGENTS.md`：定义自动化开发代理在仓库中的工作规则。

## 环境要求

- .NET SDK 10
- Node.js 22 或兼容版本
- npm
- Git

检查本机环境：

```powershell
dotnet --version
node --version
npm.cmd --version
git --version
```

如果 Windows PowerShell 的执行策略阻止 `npm.ps1`，可以使用 `npm.cmd`，无需修改系统执行策略。

## 还原、构建和测试

在仓库根目录执行：

```powershell
dotnet restore
dotnet build Nexora.sln
dotnet test Nexora.sln
```

## 启动 API

```powershell
dotnet run --project src/Nexora.Api
```

健康检查端点：

```text
GET /health
```

## 启动 Worker

```powershell
dotnet run --project src/Nexora.Worker
```

使用 `Ctrl+C` 请求 Worker 优雅停止。

## 启动 React 管理端

```powershell
cd web/nexora-admin
npm.cmd install
npm.cmd run dev
```

生成生产构建：

```powershell
npm.cmd run build
```

## 开发规则

- 修改前阅读 `README.md` 和 `docs/architecture.md`。
- Domain 不得依赖 Infrastructure。
- Application 不得依赖 Infrastructure。
- Agent 不得直接依赖 Infrastructure。
- Controller 中不得加入业务逻辑。
- 不提交密码、令牌、连接字符串或其他 Secret。
- 每个任务只修改明确范围内的内容。
- 每次修改后运行 build 和相关测试。
- 不提前实现未来阶段的功能。

