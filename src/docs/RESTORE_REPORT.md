# WPF 定制版工程恢复报告(RESTORE_REPORT)

日期: 2026-11-14
工作副本: `C:\114514\dida\ticktick-wpf-custom\`(原始反编译资料 `analysis_work\decompiled\` 未改动)

## 1. 工程恢复情况
- 5 个工程复制到工作目录:KotlinModels、TickTickUtils、TickTickModels、TickTickDao、TickTick。
- 新建 `TickTickCustom.sln`(5 项目,ProjectReference 依赖)。
- 依赖图(实测编译顺序):KotlinModels → TickTickUtils → TickTickModels → TickTickDao → TickTick。
- **TickTickHandler 未从源码编译**:确认为 C++/CLI 混合模式程序集(gcroot/delegate*/数据段/`<Module>`、`-CppImplementationDetails-` 等目录),ILSpy 产物为不可编译的伪 C#。处理:直接引用原版二进制 `滴答清单\TickTickHandler.dll`(与 KotlinMultiplatformAll.dll 同策略)。

## 2. .NET 环境
- 本机原本无 SDK;已安装 **.NET SDK 8.0.428**(用户目录 `analysis_work\dotnet-sdk\`)。
- net462/net452 目标框架通过 NuGet 包 `Microsoft.NETFramework.ReferenceAssemblies 1.0.3` 提供引用程序集。
- WinRT(Windows.winmd)来自 Windows Kits 10.0.26100.0(本机已存在)。

## 3. 编译结果(全部 ✅)
| 程序集 | TFM | 结果 |
|---|---|---|
| KotlinModels | net452 | ✅ 0 警告 0 错误 |
| TickTickUtils | net452 (WPF) | ✅ 46 个无害警告 |
| TickTickModels | net462 | ✅ |
| TickTickDao | net462 | ✅ |
| TickTick(WPF 主程序,x64) | net462 | ✅(修复后) |

产物: `ticktick-wpf-custom\TickTick\bin\Debug\net462\TickTick.exe`(注入 .g.resources 后 23.1MB 容器)。

## 4. 修复清单
### 项目文件
1. 内部引用 HintPath → ProjectReference(Models/Dao/TickTick)。
2. 全部 HintPath 相对路径修正(反编译时以 decompiled\TickTick 为基准少一级目录):`..\..\..\滴答清单\` → `..\..\滴答清单\`;winmd 绝对路径改相对。
3. 各 csproj 注入 `Microsoft.NETFramework.ReferenceAssemblies` PackageReference。
4. TickTick.csproj:`GenerateResourceUsePreserializedResources=true` + `System.Resources.Extensions` 包(修 MSB3822/3823)。
5. `UseWindowsForms=true`(WinForms 引用);新增 Windows.winmd + System.Runtime.WindowsRuntime 引用(修 WinRT CS0246)。
6. System.ValueTuple:安装目录 DLL(4.0.2.0)与 Utils 编译所用 4.0.3.0 冲突(CS1705)→ 改 NuGet `System.ValueTuple 4.5.0`。
7. 新建 `tools\GResourcesFix`(Mono.Cecil):从原版 EXE(只读)提取 `TickTick.g.resources`(23,120,890 字节)注入自编译 EXE——WPF 的 BAML 必须位于 `.g.resources` 容器,ILSpy 导出为松散 EmbeddedResource 会导致 `App.InitializeComponent` 抛 `IOException`。

### 源码(9 处反编译缺陷,均为最小修改)
| 文件 | 问题 | 修复 |
|---|---|---|
| MarkDownEditor.cs(2 处) | `int ?? int` 无效表达式 | 删除多余 `?? (-1)` |
| WebView2EmbeddedBrowser.cs | 未赋值 `num` | 初始化 `= 0` |
| FocusWebSocket.cs | 未赋值 `num` | 初始化 `= 0` |
| TaskStickyWindow.cs | 循环变量与外层同名(CS0136)+ KVP 误调用 IsIntersect | 循环变量改名 stickyPair,使 IsIntersect 落到外层 WindowRect 变量 |
| CalendarTimelineDayView.cs | 构造函数二义性(CS0121) | 显式传第 4 参数 0.0 走 height 重载 |
| SystemToastManager.cs | 反编译展开的 add_Dismissed/remove_Dismissed(CS0571) | 还原为 `Dismissed +=` 事件订阅 |
| ColumnDao.cs | 嵌套作用域同名 serverId(含后续引用) | 内层改名 serverId2 并同步 3 处引用 |
| ProjectGroupBatchHandler.cs | 同上 | 同上 |
| TaskListViewModel.cs | 嵌套同名 item | 改名 item2 |

### 部署
- 原生与运行时依赖从原安装目录**只读复制**到输出目录:KotlinMultiplatformAll.dll、sqlite3.dll、WebView2Loader.dll、Hunspellx64.dll、SmartDateDLL.dll、VC 运行库、locales、TTF、RemindSound、AppIcons、EmojiJsonWithSkin.json、pinyin.txt 等。142 个引用 DLL 由 MSBuild 自动复制,无缺失。

## 5. 启动验证
- 第 1 次启动:进程存活但 ~18s 后崩溃。WER/.NET 日志定位:`System.IO.IOException` @ `ResourcePart.GetStreamCore` → `App.InitializeComponent` → 发现 `.g.resources` 缺失问题(见上)。
- 注入 .g.resources 后再次启动:**成功**。
  - 可见窗口:`滴答清单`(主窗口)+ `登录到滴答清单`(登录窗口)——符合预期,登录窗口即第三阶段剥离对象。
  - 进程持续存活、Responding=True、无新崩溃事件。

## 6. 尚未解决 / 已知事项
1. 启动时弹出登录窗口(待第三阶段剥离账号模块后消除)。
2. .g.resources 注入目前为手动后处理步骤,尚未挂到 csproj 的 PostBuild Target(建议下一步自动化,源=原版 EXE 只读)。
3. Debug 构建可用;Release/安装包打包未做。
4. 同步相关代码仍编译在内(第三阶段处理)。
5. 反编译残留的 46 个无害警告(未赋值字段提示类)未逐条清理。
6. 5 个 csproj 中 Dao 也带了一个多余的 TickTickHandler 二进制引用(无害)。

## 7. 第三阶段剥离方案(下一步)
按 `analysis_work\RECOMPILE_FEASIBILITY.md` 与 `docs/research/account-and-telemetry-audit.md`:
1. **启动链**:App.cs / MainWindowManager 启动分流——本地模式直接进主窗口,跳过 LoginDialog。
2. **遥测清零**:UserActCollectUtils 全部方法改为 no-op;移除 Bugsnag 引用与初始化。
3. **会员拦截清零**:ProChecker.ShowUpgradeDialog 等改为直接放行(不弹窗、不拦截);ProType/SettingsType.Premium 菜单项移除。
4. **ABTest 清零**:ABTestManager.TryGetABTestGroup 返回固定值/空,阻断网络实验拉取。
5. **同步旁路**:保留 Dal 本地读写,同步触发点(SyncServices 调用方)改为 no-op;实体 etag/syncStatus 字段保留(不重建数据库)。
6. **网络请求**:Communicator 调用点全部旁路(不删除类本身,防止牵连编译)。
7. **BAML 入口**:LoginDialog/Subscribe/Upgrade 等窗口类不再实例化即可,不删除 baml 资源(避免破坏资源索引)。
8. 每步后:全量重编译 + 启动冒烟 + 功能抽测(任务/清单/标签/提醒/日历)。

## 8. 安全声明
所有修改均在本地完成;反编译源码与定制版 EXE 不上传、不提交公开仓库、不发送第三方服务;原版安装目录只读;原始反编译资料与 local-tasks 备用项目未改动。

---

# 第三阶段：商业化模块剥离结果(2026-11-14)

## 剥离清单与落实方式
| 目标 | 文件 | 修改 |
|---|---|---|
| 登录/注册 | ticktick_WPF\App.cs | 启动门:`LoginUserId==""` 时不再弹 LoginDialog,改为创建本地用户 `localuser/Local` 并直接进主窗口 |
| 账号相关网络请求(启动链) | App.cs ShowMainWindow | 移除 `Communicator.PullAppConfig`、`AppConfigManager.PerformPullAppConfig/StartPolling`、`UpgradeAppService.SendUpdateFrom`、`ABTestManager.AssignGroupOnLaunch`、`UserActCollectUtils.StartPost` |
| 云同步 | ticktick_WPF.Util\SyncManager.cs | 静态构造不再 Init/订阅网络事件;`Sync()` 直接返回;`Init()`/`PullProjectTasks()` no-op;网络变化回调清空。SyncService 永不启动(无 WebSocket) |
| VIP/会员 | ticktick_WPF.Dal\UserDao.cs | `IsPro()/IsPro2()/IsUserValid()` 恒返回 true(所有原版会员功能全量开放) |
| 会员拦截弹窗 | ticktick_WPF.Util\ProChecker.cs | `ShowUpgradeDialog` 立即返回,不再弹任何升级窗 |
| AB 实验/推广 | ticktick_WPF.Service\ABTestManager.cs | `AssignGroupOnLaunch` no-op(不再请求实验服务器) |
| 遥测 | ticktick_WPF.Util\UserActCollectUtils.cs | 整类替换为**同签名 no-op 桩**(17 个方法,行为采集全部清零) |
| 崩溃上报 | ticktick_WPF\BugsnagHelper.cs | 静态构造不再配置 WPFClient;`SendException/SetUser` no-op(Bugsnag.dll 不再被调用) |

## 运行期修复(本地无账号环境暴露的问题)
1. `System.Memory` 版本冲突 → App.config 绑定重定向 4.0.2.0→4.0.1.2(NuGet 实际版本)。
2. `TagSerializer.NormalizeNames(null)`(无用户初始化时标签列表为 null)→ 增加 null 防护返回空列表。

## 验证结果
- 全量重编译 ✅(0 错误)
- `.g.resources` 注入 ✅
- 启动 ✅:仅出现「滴答清单」主窗口,**无登录窗口**
- 运行 15s+:进程存活、Responding=True、异常日志 **NO EXCEPTIONS**
- 临时异常落盘日志已保留在 docs\unhandled_exceptions.log 机制(后续可在稳定后移除)

## 遗留事项
1. 同步类(SyncManager/SyncService/Communicator)仍在编译内但已全部旁路;彻底移除需大面积删调用点,收益低风险高,暂不执行。
2. 登录/订阅/升级窗口类与 BAML 仍在(不再被实例化);BAML 入口未删除以避免资源索引破坏。
3. 尚未做长时运行与全功能人工回归(任务增删改、提醒触发、日历、看板、习惯、番茄钟)——建议下一轮执行功能验收清单。
4. 原始备份:`_original_backup\`(剥离前 7 个关键文件)。

---

# 会员入口清理与全功能解锁(2026-11-14 追加)

| 修改 | 文件 | 内容 |
|---|---|---|
| 设置菜单移除入口 | ticktick_WPF.ViewModels\SettingViewModel.cs | 删除 Account 与 Premium 两个菜单项(分隔符保留) |
| 入口兜底重定向 | ticktick_WPF.Views\SettingDialog.cs | ShowSettingDialog 收到 Premium/Account 一律改开 More 设置页 |
| 任务数量上限解除 | ticktick_WPF.Util\ProChecker.cs | CheckTaskLimit 恒 false(999 条/清单上限与总数上限均失效) |
| 番茄计时上限解除 | 同上 | CheckTimerLimit 恒 false |
| 全局配额解除 | ticktick_WPF.Util\Utils.cs | GetUserLimit 恒 long.MaxValue(附件日上传数等所有 LimitKind) |

加上此前 `UserDao.IsPro/IsPro2/IsUserValid → true` 与 `ProChecker.ShowUpgradeDialog` no-op,所有原版会员判断链(CheckPro/IsUserValid/ShowUpgradeDialog/各 Limit)全部放行,升级/订阅/账户 UI 无任何可达入口。

验证:重编译 ✅ → .g.resources 注入 ✅ → 启动 ✅ 主窗口正常、无登录窗、无异常日志。

---

# 追加:头像菜单/皇冠清理 + 本地 AI 自定义 API(2026-11-14)

## 头像菜单与皇冠(LeftMenuBar.cs)
1. `InitMenuOptions()`:移除「Premium(高级会员)」菜单项,头像菜单现在只有 设置/统计/退出。
2. `SetProCrown()`:头像旁的皇冠(Pro)与免费标识图标一律隐藏。
3. `OnActionSelected` "Premium" 分支 no-op(防御性)。
4. `ShowSettingDialog()`:不再拉取远程设置、不再检查登录,直接打开本地「更多设置」。
5. 主窗口 `UpgradeButton`(顶部升级入口)原为客户端升级提示,保留(与会员无关);其余会员跳转 `Utils.StartUpgrade` 调用点已不可达。

## 本地 AI 自定义 API(ticktick_WPF.Views.AI\LocalAIChatPanel.cs 新增)
- AI 标签页不再打开需要登录的 WebView AI 工作台,改为原生聊天面板(SwitchAI 已替换)。
- 顶部「AI API 设置」:Base URL / API Key / Model(OpenAI 兼容,默认 gpt-4o-mini),保存到 `ExtraSettings.AI_ApiBaseUrl/AI_ApiKey/AI_Model`。
- 对话:POST `{BaseURL}/chat/completions`(Bearer Key),带系统提示词 + 会话历史。
- **软件操作工具协议**(模型输出单行 JSON 即执行):
  - `add_task`(title/dueDate/priority/tags/projectId)→ `TaskService.AddTask` + `DataChangedNotifier.NotifyAddTaskToSelectView`
  - `count_tasks` → SQL 统计未完成数
  - `list_tasks`(limit)→ SQL 列出最近未完成任务
  执行结果自动回传模型生成自然语言答复。
- 编译 ✅,启动 ✅ 无异常。

## 验证
- 全量重编译 0 错误;注入 .g.resources 后启动,主窗口正常,无登录/会员/皇冠元素,异常日志 NO EXCEPTIONS。

---

# 追加 2:UI 残留清理 + AI 设置弹窗化(2026-11-14)
1. 窗口控制按钮旁的皇冠(MainWindow.UpgradeButton):所有可见分支改为 Collapsed,Click no-op——顶部皇冠彻底移除。
2. 头像恢复完整:重新显示免费头像标识(原本它盖住头像缺口),Pro 皇冠仍隐藏。
3. 左下角"..."菜单:移除「检查更新」「在网页上查看」两项,现在只剩 快捷键/帮助中心/反馈/更新日志。
4. AI 设置改为独立居中弹窗(520x260):Base URL / API Key / Model 三项 + 保存;面板左上角显示当前配置状态;避免内嵌面板无法输入的问题。
验证:重编译 ✅ 注入 ✅ 启动 ✅ NO EXCEPTIONS。

---

# 追加 3:AI 服务商抽屉 + 聊天界面重造(2026-11-14)

## 服务商管理(AIProviderStore.cs 新增 + LocalAIChatPanel 抽屉)
- 存储结构:多 Provider(id/name/base_url/api_key/model/temperature/max_tokens/note/is_official),JSON 持久化于 %APPDATA%\LocalTasks\ai_providers.json,启动自动加载。
- 内置预设卡片:Ollama / DeepSeek / SiliconFlow,点击自动填充 base_url 与默认模型。
- 功能:新增 / 删除 / 设为激活;「获取模型列表」调 /models 下拉选择;「测试连接」区分 200 / 401(Key 无效)/ 404(路径)/ 连接失败 / 地址格式错误。
- Deep link:`ticktick://v1/import-provider?name=&base_url=&api_key=&model=...`(新增 UriScheme.ProviderController),抽屉里也有「导入链接」输入框。
- 抽屉 UI:点击右上「服务商」在聊天区左侧滑出卡片列表 + 右侧编辑表单;未保存切换/关闭弹确认;双击面板不再触发主窗口最大化(双层防护)。

## 聊天界面重造(LocalAIChatPanel)
- 气泡:助手居左 rgba(255,255,255,0.75),用户居右淡冰蓝半透,统一圆角 14、最大宽 520、间距 10。
- 滚动区透明背景,细滚动条 35% 透明度、悬停加深;加载态为助手气泡内 ●●● 打点动画。
- 底部输入:自动扩容(38→120px),Shift+Enter 换行,Enter 发送;半透无边框。
- 发送按钮:胶囊圆角,未配置 API 置灰并带悬浮提示,发送期间显示进行中状态。
- 输入框与发送键之间新增 📋(注入待办清单上下文)与 🧩(提示词预设:默认/翻译/总结/程序员)两个小按钮。
- 主窗口容器布局未改动,仅 AIBorder 内容替换。

---

# 追加 4:AI 控件去拼接化 + 边界安全(2026-11-14)
- 服务商入口右移改为右侧留 68px 安全间距,不再侵入右上角系统按钮区。
- 服务商 / 注入待办 / 提示词按钮统一胶囊圆角(14px),复用 PopupBackground/TextPrimary 主题资源,hover/pressed/disabled 透明度过渡与原生一致。
- 提示词下拉改为整体圆角卡片菜单(外层 4px 内距 + 选项 8px 圆角 hover 高亮,hover 时 AccentPrimary 底白字),并加边界检测:锚点下方工作区空间不足 140px 自动向上弹出。
- 业务逻辑、变量名、容器布局未动。

---

# 追加 5:双击最大化禁用 + 服务商按钮修复(2026-11-14)
根因:`MainWindow.HeaderGrid_MouseLeftButtonDown`(绑定在覆盖整个顶栏的 HeaderGrid Border 上)
1. 300ms 内双击 → `ToggleWindowState()` 触发全屏(即用户看到的双击全屏);
2. 对任意点击调用 `DragMove()`,把落在顶栏区域的「服务商」按钮点击吞掉,导致抽屉永远打不开。
修复:处理函数开头沿可视树检查 OriginalSource,命中 Button(服务商/系统按钮)直接 return 交给按钮自身;删除 300ms 双击 → ToggleWindowState 逻辑(拖动窗口能力保留)。
