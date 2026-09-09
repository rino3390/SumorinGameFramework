# Sumorin Game Framework

Unity MVP 架構共用框架，提供 DDD 核心、遊戲資料管理、模組安裝器等功能。

## 架構優點

- **模組化設計**：透過 VContainer DI 實現鬆耦合，模組可獨立開發與測試
- **領域驅動設計**：採用 DDD 概念，業務邏輯集中於 Controller，Model 為純資料，可用純 NUnit 測試
- **事件驅動架構**：透過 EventBus 實現跨模組通訊，避免直接依賴
- **視覺化資料管理**：GameManager 編輯器視窗讓企劃可直接編輯遊戲資料
- **快速擴展**：Module Installer 提供預製模組（屬性系統、Buff 系統等），一鍵安裝

## 安裝說明

### 依賴套件

本套件需要以下依賴，請先安裝：

| 套件 | 安裝方式 |
|------|----------|
| VContainer | OpenUPM |
| DoTween | Asset Store |
| R3 | UnityNuGet registry 裝核心 dll，Git URL 裝 Unity 橋接 |
| ObservableCollections | UnityNuGet registry |
| UniTask | OpenUPM |
| Odin Inspector | Asset Store（付費） |
| Unity Localization | Package Manager |
| NSubstitute（測試用） | UnityNuGet registry，原本隨 Zenject 附帶 |

### 套件來源配置

在 `ProjectSettings > Package Manager` 中加入兩個 scoped registry：

```json
{
    "name": "OpenUPM",
    "url": "https://package.openupm.com",
    "scopes": ["com.cysharp.unitask", "jp.hadashikick.vcontainer"]
},
{
    "name": "UnityNuGet",
    "url": "https://unitynuget-registry.openupm.com",
    "scopes": ["org.nuget"]
}
```

`Packages/manifest.json` 的依賴：

```json
"com.cysharp.r3": "https://github.com/Cysharp/R3.git?path=src/R3.Unity/Assets/R3.Unity#1.3.1",
"com.cysharp.unitask": "2.5.10",
"jp.hadashikick.vcontainer": "1.19.0",
"org.nuget.nsubstitute": "6.2.0",
"org.nuget.observablecollections.r3": "3.3.4",
"org.nuget.r3": "1.3.1"
```

![image-20260203204628877](https://github.com/rino3390/SumorinGameFramework/blob/main/img/2.png)

### 透過 Git URL

![image-20260203204301557](https://github.com/rino3390/SumorinGameFramework/blob/main/img/1.png)

在 Unity Package Manager 中選擇 `from git URL`，輸入：

```
https://github.com/rino3390/SumorinGameFramework.git?path=Core
```

## 功能模組

| 模組 | 說明 |
|------|------|
| DDDCore | Entity、Repository、CommandResult、EventBus 基礎架構 |
| Presentation | View 管理框架（ViewRegistry、IViewProvider、IBindableView） |
| GameManager | 遊戲資料管理編輯器視窗 |
| ModuleInstaller | 可選模組安裝器（屬性、Buff、存檔等系統） |
| SumorinUtility | 通用工具方法、配置存取（ConfigManager）與型別下拉（TypeDropdown） |

---

## DDDCore

提供領域驅動設計的基礎架構，包含 Entity、Repository、CommandResult 和 EventBus。

### Entity

所有領域實體的基底類別，提供唯一 Id：

```csharp
public class Player : Entity
{
    public string Name { get; }
    public int Level { get; private set; }

    public Player(string id, string name) : base(id)
    {
        Name = name;
        Level = 1;
    }

    public void LevelUp() => Level++;
}
```

### Repository

管理 Entity 的儲存庫，支援 CRUD 操作與條件查詢。
沒有具名查詢需求就直接注入框架的 `IRepository<TEntity>`，不自訂 Repository：

```csharp
public class PlayerController
{
    [Inject] private IRepository<Player> repository;

    // 查詢：使用 Find/FindAll，不要先取集合再 LINQ
    public Player FindByName(string name)
    {
        return repository.Find(p => p.Name == name);
    }

    public IEnumerable<Player> FindByMinLevel(int minLevel)
    {
        return repository.FindAll(p => p.Level >= minLevel);
    }
}
```

需要具名查詢方法（如 `GetByOwner()`）時，才自訂介面繼承 `IRepository<TEntity>` 再加上該方法。
刪除用 `DeleteById` / `DeleteAll`。

### EventBus

自實作事件系統（DDDCore EventBus），支援同步與非同步事件。
由 `DDDCoreInstaller` 註冊，發布端注入 `IPublisher`、訂閱端注入 `ISubscriber`：

```csharp
// 定義事件：名詞 + 動詞完成式，代表已發生的事實
public class PlayerLevelUpped : IEvent
{
    public string PlayerId { get; }
    public int NewLevel { get; }

    public PlayerLevelUpped(string playerId, int newLevel)
    {
        PlayerId = playerId;
        NewLevel = newLevel;
    }
}

// Controller 注入 IPublisher 發布
publisher.Publish(new PlayerLevelUpped(player.Id, player.Level));

// Flow 注入 ISubscriber 訂閱，回傳 IDisposable，Dispose 時解除
subscription = subscriber.Subscribe<PlayerLevelUpped>(evt =>
{
    Debug.Log($"玩家 {evt.PlayerId} 升級到 {evt.NewLevel}");
});
```

---

## 配置存取（ConfigManager）

同一份 SO 資產，兩個取用面：Domain 拿數值介面，表現層拿具體 SO。
`IConfig`、`ConfigSource`、`ConfigManager` 位於 `Sumorin.SumorinUtility`。

```csharp
// Contract：數值介面，只含數值不含 Unity 型別
public interface IItemConfig : IConfig
{
    int Price { get; }
}

// DataScript：SO 以 Odin 序列化屬性直接實作介面，另有資源屬性
[DataEditorConfig("道具管理", "Data/Items", "道具")]
public class ItemData : SODataBase, IItemConfig
{
    [OdinSerialize] public int    Price { get; private set; }
    [OdinSerialize] public Sprite Icon  { get; private set; }
}
```

組裝：`DDDCoreInstaller` 註冊 `ConfigManager`，模組 Installer 各自貢獻 `ConfigSource`，遊戲側不自行組裝：

```csharp
new DDDCoreInstaller().Install(builder);
builder.Register(_ => new ConfigSource(itemDataSet.Datas), Lifetime.Scoped);   // 不能用 RegisterInstance，同型別 Singleton 重複註冊會衝突
```

`Lifetime.Scoped` 的重複註冊 VContainer 會收成集合，`ConfigManager` 解析 `IEnumerable<ConfigSource>` 就拿得到全部來源。
`RegisterInstance` 是 Singleton，第二個模組註冊時會拋出 `Conflict implementation type`。

```csharp
```

讀取：查找鍵是配置自己的 `Id`（`SODataBase` 提供），數值面與具體 SO 查同一份字典：

```csharp
// Controller 讀數值面，看不到 SO 型別
var config = configManager.Get<IItemConfig>(itemId);

// Presenter / View 讀具體 SO，資源欄位直接可用
var data = configManager.Get<ItemData>(itemId);
icon.sprite = data.Icon;
```

---

## 型別下拉（TypeDropdown）

Inspector 欄位用下拉選擇介面的實作，顯示名稱由型別自己宣告。
`TypeDropdownNameAttribute` 位於 `Sumorin.SumorinUtility`，欄位所在的組件不需要引用 Editor 組件。

```csharp
public interface IEffect { }

[TypeDropdownName("添加修改器")]
public class AddModifier : IEffect { }

public class Heal : IEffect { }   // 沒標註，顯示 Heal

[TypeFilter("@SumorinEditorUtility.TypeDropdown<IEffect>()")]
public List<IEffect> Effects = new();
```

顯示名稱只從 attribute 讀，不建立實例，實作可以沒有公開建構子。
欄位型別是介面，需要多型序列化。
放在 `SODataBase` 這類 Odin 序列化的物件上直接可用。
放在一般 `MonoBehaviour` 或 `ScriptableObject` 上要加 `[SerializeReference]`。

---

## GameManager

視窗 `Tool > GameManager`。

提供視覺化的遊戲資料管理編輯器，讓企劃可在 Unity 編輯器中直接編輯 ScriptableObject 資料。

首次開啟會自動於`Data/GameManager`新增`Tab`的ScriptableObject，可用於自訂要顯示於管理視窗的頁籤。

![](https://github.com/rino3390/SumorinGameFramework/blob/main/img/4.png)

### 建立資料類別

繼承 `SODataBase` 並加上 `DataEditorConfig` Attribute，即可自動產生 GameManager 頁籤：

```csharp
[DataEditorConfig("道具管理", "Data/Items", "道具")]
public class ItemData : SODataBase
{
    [LabelText("名稱")]
    public string Name;

    [LabelText("售價")]
    public int Price;

    [LabelText("說明")]
    [TextArea]
    public string Description;
}
```

Attribute 參數依序為：Tab 名稱、資料存放路徑、資料類型標籤。

### 建立資料集合

繼承 `DataSet<T>` 管理多筆資料，供運行時查詢使用：

```csharp
public class ItemDataSet : DataSet<ItemData> { }
```

### 進階：自訂編輯器頁籤

如需完全自訂的編輯器行為，可繼承 `CreateNewDataEditor<T>`：

```csharp
public class ItemDataEditor : CreateNewDataEditor<ItemData>
{
    protected override string DataRoot => "Data/Items";
    protected override string DataTypeLabel => "道具";

    // 可覆寫方法自訂行為
}
```

或繼承 `GameEditorMenuBase` 建立非資料管理的自訂頁籤：

```csharp
public class SettingsEditor : GameEditorMenuBase
{
    public override string TabName => "遊戲設定";

    protected override OdinMenuTree BuildMenuTree()
    {
        var tree = SetTree();
        tree.Add("一般設定", new GeneralSettings());
        tree.Add("音效設定", new AudioSettings());
        return tree;
    }
}
```

---

## ModuleInstaller

提供預製的遊戲模組，可透過編輯器視窗一鍵安裝。

### 開啟方式

`Tools > Sumorin > Module Installer`

### 可用模組

| 模組 | 說明 |
|------|------|
| FolderStructure | 標準專案資料夾結構 |
| Attribute | 屬性系統（HP、MP、攻擊力等） |
| Buff | Buff/Debuff 系統（支援堆疊、持續時間） |
| Save | 存檔系統（存檔槽、Repository 轉接層、存檔加密） |
| GameSetting | 遊戲設定管理（搭配 GameSettingConfig 使用） |

### 模組依賴

部分模組有依賴關係，安裝時會自動檢查：

- Buff → Attribute（Buff 效果會修改屬性）

安裝器會顯示依賴狀態，未滿足的依賴需先安裝。

### GameSetting

將頁籤新增至`GameManager > Tab`中，開啟會自動生成`GameManager > GameSettingConfig`，可自行配置「遊戲設定」頁籤中要顯示的子頁籤。

![](https://github.com/rino3390/SumorinGameFramework/blob/main/img/3.png)
