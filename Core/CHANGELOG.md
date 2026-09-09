# Changelog

Sumorin Game Framework 的變更紀錄，涵蓋 Core 套件與框架模組。
版本號以 Core 套件（`Core/package.json`）為準，各模組另有自己的版本（`ModuleTemplates/modules.json`）。

## [0.5.0] - 2026-09-10

### 破壞性變更

- DI 由 Zenject 改為 VContainer，響應式由 UniRx 改為 R3，可觀察集合改用 ObservableCollections。
  既有專案要更換套件、asmdef 引用與 `using`，套件來源配置見 README。
- `DDDCoreInstaller` 與各模組 Installer 改為實作 VContainer 的 `IInstaller`。
  呼叫方式改為 `new DDDCoreInstaller().Install(builder)`。
  需要資產的模組由建構子傳入 DataSet，例如 `new BuffInstaller(buffDataSet)`。
- 值面型別由 `IReadOnlyReactiveProperty<T>` 改為 R3 的 `ReadOnlyReactiveProperty<T>`。
  讀現值改用 `CurrentValue`，`Value` 只有可寫的 `ReactiveProperty<T>` 才有。
- `ConfigSource` 要用 `Lifetime.Scoped` 的工廠註冊，不能用 `RegisterInstance`。
  VContainer 把同型別的 Singleton 重複註冊視為衝突，裝第二個模組時會拋 `Conflict implementation type`。
- `PooledViewProvider` 建構子改收 prefab、閒置實例的父物件與預載數量。
  不再由外部提供物件池。
- 存檔系統（2.0.0）的加密包裝改以工廠註冊，不再用裝飾器。
  參與者註冊的輔助方法改名為 `RegisterRepositoryParticipant` 與 `RegisterStateParticipant`，改掛在 `IContainerBuilder` 上。
- Buff 系統（5.0.0）的 `StackRecords` 改為 ObservableCollections 的 `ObservableList`。
  `ObserveReset` 的事件型別改為 `CollectionResetEvent<T>`。
- 測試基類 `ZenjectUnitTestFixture` 改為 `VContainerUnitTestFixture`，namespace 改為 `Sumorin.TestFramework`。
  註冊走 `Builder`，首次取用 `Container` 時才建置，之後的註冊不生效。
- NSubstitute 不再隨 Zenject 附帶，改由 UnityNuGet 的 `org.nuget.nsubstitute` 提供。
- 模組版本：FolderStructure 2.0.0、屬性系統 4.0.0、Buff 系統 5.0.0、存檔系統 2.0.0。

### 改進

- `PooledViewProvider` 建立時可預載指定數量的實例，歸還時收回池的父物件底下。
- 架構驗證器的「一事實恰一 Flow 訂閱」與「View 不得引用 QueryService」兩條規則改讀編譯後的 IL。
  靠型別推導的 `Subscribe(handler)`、藏在 lambda 裡的訂閱與型別別名都能命中，不再被原始碼掃描漏掉。

## [0.4.0] - 2026-09-07

### 新功能

- `LocalizedString` 的 drawer 支援 Odin 序列化的屬性。
  DataScript 以 `[OdinSerialize]` 宣告的 `LocalizedString` 屬性，與 Unity 序列化欄位用同一套表格選擇與各語言預覽。
  值尚未建立時直接顯示選擇器，不再出現 Odin 的 Null 框。

### 改進

- `LocalizedString` drawer 改以字串的 Id 引用，改字串 ID 不會讓其他資產的引用失效。
  舊資產以名稱引用的照常讀取，經 drawer 寫入後自動換成 Id。
- 同一個字串被多個欄位引用時，在其中一個欄位改字串 ID，其他欄位的下拉文字同步更新。
- 屬性系統（3.0.0）與 Buff 系統（4.0.0）的配置資產改為 Odin 序列化屬性直接實作 `I{X}Config`。
  序列化格式改變，升主版號。
- 屬性系統（3.0.0）：配置加上屬性種類，資源型屬性拒絕 Modifier。
  新增 `AdjustBaseValue` 直接增減基礎值，與 `GetMaxValue`／`GetMinValue` 上下限查詢。
- 屬性種類、Buff 生命週期與重複獲得時行為的下拉選單顯示中文名稱。

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
