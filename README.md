# dida_test

滴答清单的 Sikoll 个人修改测试版。

> 作者：Sikoll  
> 声明：个人修改测试版，禁止商业化运用。  
> 反馈邮箱：3485446086@qq.com

## 下载安装包

- [Windows x64 安装包](dist/dida_test_sikoll_setup_x64.exe)
- 安装包为当前用户安装，不要求管理员权限。
- 安装程序会保留已有用户数据。

## 功能说明

- 本地任务数据，不依赖账号登录。
- 移除登录、会员与商业化入口。
- 本地 AI 对话与服务商配置。
- AI 页面、服务商设置、头像和菜单做了个人化调整。

## 源码目录

- `src/TickTickCustom.sln`：主解决方案。
- `src/TickTick`：WPF 主程序。
- `src/TickTickUtils`、`src/TickTickModels`、`src/TickTickDao`、`src/KotlinModels`：依赖项目。
- `installer/dida_test_sikoll.iss`：Inno Setup 安装脚本。
- `dist/`：已构建的 Windows 安装包。

## 构建依赖

源码基于 .NET Framework 4.6.2 / WPF，使用 .NET SDK 8 构建。

项目仍引用原安装目录中的部分二进制依赖。构建前需要把原版程序目录放在仓库外的工作区父目录，并命名为 `滴答清单`，或将各项目引用路径改为自己的原版依赖目录。

构建示例：

```powershell
dotnet build .\src\TickTick\TickTick.csproj -c Release
```

编译完成后需要向主程序注入原版 `.g.resources`，否则 WPF 会缺少 `app.xaml` 资源。相关工具位于 `src/tools/GResourcesFix`。

## 安装包构建

使用 Inno Setup 6 打开或编译：

```text
installer/dida_test_sikoll.iss
```

输出文件位于 `dist/dida_test_sikoll_setup_x64.exe`。

## 许可

仓库保留原有 `LICENSE`。第三方组件、资源与原始程序的授权归其各自权利人所有。
