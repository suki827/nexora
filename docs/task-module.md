# Task 模块

## 当前实现

一次 `AnalysisTask` 表示用户提交的一次完整分析流程。输入保存在 `task_inputs`，每次执行保存在 `analysis_task_attempts`，结构化结果保存在 `analysis_results`，生成的物理文件记录保存在 `generated_artifacts`。`GeneratedArtifact` 与 `AssetFile` 是不同资源。

用户 API 使用现有 Cookie 和 CSRF 机制：

| 操作 | 路由 |
| --- | --- |
| 创建任务 | `POST /api/tasks` |
| 分页列表 | `GET /api/tasks?page=1&pageSize=20` |
| 任务详情 | `GET /api/tasks/{id}` |
| 请求取消 | `POST /api/tasks/{id}/cancel` |
| 查询输入、执行、结果、产物 | `GET /api/tasks/{id}/inputs`、`attempts`、`results`、`artifacts` |

创建请求包含 `analysisType`、可选 `priority`（-100 到 100）、可选 `requestPayload`、可选 `idempotencyKey`，以及 `inputs`。目前唯一支持的分析类型是 `media_analysis`，且必须提供一个 `inputType=asset_file`、`inputRole=source` 的输入。创建时验证输入文件已就绪且归当前用户所有。任务自动进入 `queued`。

`Nexora.Worker` 中的 Task 执行器在源文件的媒体元数据状态为 `completed` 或 `failed` 时领取任务。完成时生成 `technical_metadata`、版本 `1.0` 的结构化结果；元数据失败时，Task 记录失败原因。领取采用数据库行锁与 `SKIP LOCKED`，同一任务不会被多个 Worker 同时领取。执行失败最多尝试三次；运行中的 Attempt 超过 10 分钟未更新会被恢复或标记失败。取消请求与领取、完成操作按任务行锁串行处理。

当前 `media_analysis` 只包含时长、分辨率、帧率、编码等技术元数据，不包含视频语义理解、语音转写或 AI 结果。Worker 需要启动，且上传文件的元数据处理完成后 Task 才会得到结果。

普通用户只能读取自己的 Task；Task 执行器负责写入 Attempt 和技术元数据 Result，这些写入不会从用户控制的 HTTP 请求直接调用。Artifact 的物理文件生成和下载接口仍需后续接入。

## 数据库关系与保护

- `users → analysis_tasks` 为 `RESTRICT`。
- `analysis_tasks → task_inputs` 和 `analysis_tasks → analysis_task_attempts` 为 `CASCADE`。
- `analysis_tasks → analysis_results` 和 `analysis_tasks → generated_artifacts` 为 `RESTRICT`，避免意外丢失历史结果或误认为物理文件已删除。
- `task_inputs → asset_files`、`task_inputs → generated_artifacts`、`generated_artifacts → analysis_results` 为 `RESTRICT`。
- `(owner_id, idempotency_key)` 在键非空时唯一；`(task_id, attempt_number)` 唯一；每个任务同一种结果最多一条 `is_current = true`；本地与云端存储键沿用 `asset_files` 的部分唯一索引模式。
- 数据库检查约束限制 Task、Attempt、Artifact 状态、输入对象二选一、非负时长与文件大小、置信度范围以及存储配置。

## 迁移状态

`AddTaskModule` 的 `Up` 仅创建上述五张表、外键、索引和检查约束；不会删除或重建已有 `users`、`assets`、`asset_files`、`media_metadata`。`Down` 主动拒绝自动回滚，因为回滚必须先决定数据库记录及物理产物的保留方式。

2026-10-03 已在当前 `nexora` 数据库执行经审核的 `docs/sql/baseline-existing-db.sql`，随后应用 `AddTaskModule`。只读核对确认五张 Task 表存在、旧版单数表不存在，迁移历史包含 `20260726053239_InitialCreate` 和 `20261003052432_AddTaskModule`。

基线脚本把**没有实际执行过**的旧 `InitialCreate` 标记为已应用，以跳过其旧表创建；这是现有数据库专用的迁移历史对齐方案。不要将此脚本用于新数据库。此历史迁移链仍不能直接用于从零创建完整数据库，新的空库需要单独的初始化方案。
