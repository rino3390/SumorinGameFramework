# Changelog

Sumorin Game Framework 的變更紀錄，涵蓋 Core 套件與框架模組。
版本號以 Core 套件（`Core/package.json`）為準，各模組另有自己的版本（`ModuleTemplates/modules.json`）。

## [0.3.0] - 2026-09-03

### 新功能

- 型別下拉：Inspector 欄位可用下拉選擇介面的實作，顯示名稱由型別自己宣告。
  實作類別標上 `[TypeDropdownName("顯示名稱")]`，欄位掛 `[TypeFilter("@SumorinEditorUtility.TypeDropdown<IEffect>()")]`。
  沒有標註的型別顯示型別名。
  不建立實例，實作可以沒有公開建構子，也可以在建構子做事。

## [0.2.0] - 2026-09-02

### 破壞性變更

- `IConfig`、`ConfigSource`、`ConfigManager` 由 `Sumorin.DDDCore` 移至 `Sumorin.SumorinUtility`。
  既有專案需更新 `using` 與 asmdef 引用。
  型別名與查找行為不變。

### 改進

- 配置與資源查找統一為 `ConfigManager` 一個入口，原規劃的 AssetProvider 廢除。
  Controller 取 `I{X}Config` 數值面，Presenter 與 View 直接注入取具體 SO。
  同一個 `Id` 查同一份字典，不再有「該讀哪個」的問題。
- FolderStructure 移除 `Script/Presenter/AssetProvider/` 資料夾（1.1.0）。
  ModuleInstaller 建立的資料夾與 asmdef 引用清單同步更新。
- 屬性系統（2.0.1）與 Buff 系統（3.0.1）跟隨 namespace 調整，行為不變。

## [0.1.0] - 開發中初版

- DDDCore：Entity、Repository、CommandResult、EventBus、配置存取。
- Presentation：ViewRegistry、IViewProvider（Transient / Pooled）、IBindableView。
- GameManager：資料編輯器視窗與 SODataBase、DataSet。
- ModuleInstaller：可選模組安裝與版本比對。
- SumorinUtility：GUID、RegexChecking 等通用工具。
- 框架模組：FolderStructure、屬性系統、Buff 系統、存檔系統（含加密）、GameSetting。
