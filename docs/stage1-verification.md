# 第一阶段验证记录

验证日期：2026-10-03。

## 已验证

- API 与 Worker 使用根目录中被 Git 忽略的 `appsettings.Local.json` 连接同一 PostgreSQL 数据库和本地媒体目录。
- 现有 `users`、`assets`、`asset_files`、`media_metadata` 表满足当前实体映射，相关记录可正常写入和查询。
- 一次性账号完成注册、Cookie 登录和 `/api/auth/me` 查询。
- 生成的 2 秒 WAV 文件通过创建资产、开始上传、上传分片、查询进度和完成上传接口写入；原文件经受保护的内容接口读回，大小为 32078 字节。
- Worker 完成技术元数据提取，时长为 2.0 秒；波形接口返回 2000 组峰值数据。
- 第二个一次性账号访问第一个账号的资产、元数据、原文件和波形，均返回 404。
- 验证生成的账号、数据库记录和媒体文件已清理。
- `dotnet build Nexora.sln --no-restore -v:q` 成功，0 个警告、0 个错误。

## 表结构差异

当时 EF Core 模型映射了 `analysis_tasks` 和 `analysis_results`，但数据库没有这两张表。当前的上传、技术元数据和波形流程不依赖它们。旧版 `TestDbController` 随后的 Task 模块改造中已移除。

## 本地配置

真实连接字符串仅保存在根目录的 `appsettings.Local.json`，该文件被 Git 忽略。API 的 `DataProtection:KeyPath` 和 API/Worker 共用的 `MediaStorage:RootPath` 也在该文件中配置为本机可写路径。
