# Sumorin Game Framework

Unity MVP 架構共用框架，提供 DDD 核心、遊戲資料管理、模組安裝器等功能。

## 架構優點

- **模組化設計**：透過 Zenject DI 實現鬆耦合，模組可獨立開發與測試
- **領域驅動設計**：採用 DDD 概念，業務邏輯集中於 Entity，職責分明
- **事件驅動架構**：透過 EventBus 實現跨模組通訊，避免直接依賴
- **視覺化資料管理**：GameManager 編輯器視窗讓企劃可直接編輯遊戲資料
- **快速擴展**：Module Installer 提供預製模組（屬性系統、Buff 系統等），一鍵安裝

## 安裝說明

### 依賴套件

本套件需要以下依賴，請先安裝：

| 套件 | 安裝方式 |
|------|----------|
| Zenject | OpenUPM 或 Asset Store |
| DoTween | Asset Store |
| MessagePipe | OpenUPM 或 NuGet |
| UniRx | OpenUPM |
| UniTask | OpenUPM |
| Odin Inspector | Asset Store（付費） |
| Unity Localization | Package Manager |

### OpenUPM 配置範例

在 `ProjectSettings > Package Manager` 中加入：

```json
"name": "OpenUPM",
"url": "https://package.openupm.com",
"scopes": [
    "com.svermeulen.extenject",
    "com.cysharp.unitask",
    "com.cysharp.messagepipe",
    "com.cysharp.messagepipe.zenject",
    "com.neuecc.unirx"
]
```

![image-20260203204628877](img\2.png)

### 透過 Git URL

![image-20260203204301557](img\1.png)

在 Unity Package Manager 中選擇 `from git URL`，輸入：

```
https://github.com/rino3390/SumorinGameFramework.git?path=Core
```

## 功能模組

| 模組 | 說明 |
|------|------|
| DDDCore | Entity、Repository、EventBus 基礎架構 |
| GameManager | 遊戲資料管理編輯器視窗 |
| ModuleInstaller | 可選模組安裝器（屬性、Buff 等系統） |
| SumorinUtility | 通用工具方法 |

---

## DDDCore

提供領域驅動設計的基礎架構，包含 Entity、Repository 和 EventBus。

### Entity

所有領域實體的基底類別，提供唯一識別碼：

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

管理 Entity 的儲存庫，支援 CRUD 操作與條件查詢：

```csharp
// 定義 Repository
public class PlayerRepository : Repository<Player> { }

// 使用方式
public class PlayerService
{
    private readonly IRepository<Player> repository;

    public PlayerService(IRepository<Player> repository)
    {
        this.repository = repository;
    }

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

### EventBus

基於 MessagePipe 的事件系統，支援同步與非同步事件：

```csharp
// 定義事件
public struct PlayerLevelUpEvent : IEvent
{
    public string PlayerId { get; }
    public int NewLevel { get; }

    public PlayerLevelUpEvent(string playerId, int newLevel)
    {
        PlayerId = playerId;
        NewLevel = newLevel;
    }
}

// 發布事件
eventBus.Publish(new PlayerLevelUpEvent(player.Id, player.Level));

// 訂閱事件（記得在 Dispose 時取消訂閱）
subscription = eventBus.Subscribe<PlayerLevelUpEvent>(evt =>
{
    Debug.Log($"玩家 {evt.PlayerId} 升級到 {evt.NewLevel}");
});
```

---

## GameManager

視窗 `Tool > GameManager`。

提供視覺化的遊戲資料管理編輯器，讓企劃可在 Unity 編輯器中直接編輯 ScriptableObject 資料。

首次開啟會自動於`Data/GameManager`新增`Tab`的ScriptableObject，可用於自訂要顯示於管理視窗的頁籤。

![](img\4.png)

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
| GameSetting | 遊戲設定管理（搭配 GameSettingConfig 使用） |

### 模組依賴

部分模組有依賴關係，安裝時會自動檢查：

- Buff → Attribute（Buff 效果會修改屬性）

安裝器會顯示依賴狀態，未滿足的依賴需先安裝。

### GameSetting

將頁籤新增至`GameManager > Tab`中，開啟會自動生成`GameManager > GameSettingConfig`，可自行配置「遊戲設定」頁籤中要顯示的子頁籤。

![](img\3.png)
