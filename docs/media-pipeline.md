# 媒体上传与分析

当前实现提供 Cookie 登录、可续传的本地分片上传、后台技术元数据提取，以及音频峰值数据。视频画面内容理解和语音转写尚未包含在这条流程中。

## 运行前配置

- API 和 Worker 使用同一个 PostgreSQL 数据库及同一个本地媒体目录。
- `ConnectionStrings__NexoraDatabase` 应设置为实际可用的连接字符串。也可将根目录的 `appsettings.Local.example.json` 复制成被 Git 忽略的 `appsettings.Local.json`，填写实际连接信息；API 和 Worker 会读取同一份文件，环境变量可覆盖其中的值。
- Visual Studio 的“管理用户机密”也可为 API 和 Worker 分别设置 `ConnectionStrings:NexoraDatabase`；两个项目都有独立的 User Secrets ID。
- 可选：`MediaStorage__RootPath` 指定绝对目录。默认目录是当前运行账户的 `LocalApplicationData/Nexora/media`；API 和 Worker 若由不同账户运行，必须显式指定相同目录并赋予读写权限。
- 可选：`DataProtection__KeyPath` 指定 API 的 Cookie 密钥目录；运行账户需有读写权限，重启后应继续使用同一目录。自定义目录中的密钥默认不加密，正式环境需限制目录权限并配置密钥静态加密。
- Worker 需要 `ffprobe` 和 `ffmpeg` 位于 `PATH` 中；也可以分别设置 `MediaProcessing__FfprobePath` 和 `MediaProcessing__FfmpegPath`。
- 现有数据库表应与 `NexoraDbContext` 的映射一致。应用启动不会自动执行 EF Core 迁移。

若使用环境变量，在两个 PowerShell 终端中分别设置相同的值并启动：

```powershell
$env:ConnectionStrings__NexoraDatabase = 'Host=localhost;Port=5432;Database=nexora;Username=YOUR_USER;Password=YOUR_PASSWORD'
$env:MediaStorage__RootPath = 'D:\nexora-media'
dotnet run --project src/Nexora.Api
```

```powershell
$env:ConnectionStrings__NexoraDatabase = 'Host=localhost;Port=5432;Database=nexora;Username=YOUR_USER;Password=YOUR_PASSWORD'
$env:MediaStorage__RootPath = 'D:\nexora-media'
dotnet run --project src/Nexora.Worker
```

正式环境应通过环境变量或秘密管理服务提供数据库凭据，并使用 HTTPS。

## 登录与 CSRF

认证使用 ASP.NET Core Identity 的 `UserManager`、现有 `users` 表和 HttpOnly Cookie。

1. `GET /api/auth/csrf`：保存返回的 Cookie 与 JSON 中的 `token`。
2. 调用 `POST /api/auth/register` 或 `POST /api/auth/login`，在请求头加入 `X-CSRF-TOKEN: <token>`，并发送上一步的 Cookie。
3. 登录后再次调用 `GET /api/auth/csrf`，以后每个 `POST`、`PUT`、`DELETE` 请求都带上新的令牌与 Cookie。
4. `GET /api/auth/me` 查询当前用户；`POST /api/auth/logout` 退出。

注册请求包含 `email`、`password`（至少 12 字符，包含大小写字母、数字和符号）和可选的 `displayName`。其他媒体接口需要登录。浏览器管理端在不同开发端口运行时，建议通过同源代理转发 API 请求，使 Cookie 和 CSRF 令牌保持同源。

## 分片上传协议

先创建媒体资产：`POST /api/assets`。然后以资产 ID 为范围执行以下操作：

| 操作 | 接口 | 说明 |
| --- | --- | --- |
| 开始 | `POST /api/assets/{assetId}/uploads` | 请求包含 `fileRole`、`originalName`、`contentType`、`totalBytes`、`chunkSize`、可选 `sha256`。返回 `fileId`。 |
| 查询进度 | `GET /api/assets/{assetId}/uploads/{fileId}` | 返回已收到的零基分片编号；重启客户端后可继续上传缺失分片。 |
| 上传分片 | `PUT /api/assets/{assetId}/uploads/{fileId}/chunks/{index}` | 请求体为原始二进制，不能使用 JSON 或表单编码。重复发送相同分片是安全的。 |
| 完成 | `POST /api/assets/{assetId}/uploads/{fileId}/complete` | 检查分片、总大小和 SHA-256，合并文件并创建待处理元数据记录。 |
| 取消 | `DELETE /api/assets/{assetId}/uploads/{fileId}` | 删除临时分片并将文件记录软删除。 |

单个文件上限为 20 GiB，分片大小为 1–32 MiB。最后一个分片可以小于设定大小。服务端生成存储键；原始文件名只作展示信息，不用于文件系统路径。

完成后可通过 `GET /api/assets/{assetId}/files/{fileId}/content` 读取原文件。该接口支持 HTTP Range 请求。数据库保存文件位置、状态、大小和哈希；文件内容保存在本地媒体目录。

## 后台分析与结果

完成上传后，`MediaMetadata` 处于 `pending`。Worker 轮询待处理记录，调用 `ffprobe` 写入时长、分辨率、帧率、编码和原始探测 JSON。存在音轨时，Worker 用 `ffmpeg` 解码成单声道 PCM，再按时间桶计算最小与最大振幅。

- `GET /api/assets/{assetId}/files/{fileId}/metadata` 返回处理状态和技术元数据。
- `GET /api/assets/{assetId}/files/{fileId}/waveform` 返回音频峰值 JSON；没有音轨或尚未完成时返回 `404`。
- 处理失败后可调用 `POST /api/assets/{assetId}/files/{fileId}/metadata/retry` 重试。

波形文件保存在媒体目录中，与原文件使用相同的服务端存储键。返回格式为：

```json
{
  "schemaVersion": 1,
  "sampleRate": 8000,
  "samplesPerBucket": 400,
  "peaks": [[-0.45, 0.52], [-0.61, 0.58]]
}
```

`peaks` 中每一项表示一个时间桶内的最小和最大振幅，数值范围约为 `-1` 到 `1`。Worker 重启后会重新拾取长时间停留在 `processing` 的记录。当前实现按单个 Worker 实例运行；多实例部署需要数据库级任务领取机制。

Worker 每小时检查一次本地上传目录。超过 7 天没有活动的未完成上传会被软删除并清理分片；正常完成或主动取消上传时也会清理分片。
