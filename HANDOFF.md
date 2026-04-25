# HANDOFF.md — 実装引き継ぎメモ

最終更新: **2026-04-16**
次回再開ポイント: **Phase 9.3（水量・地形の保存 / ロード）**

## 現在の状態

Phase 1〜8 および 9.1（制限時間モード）/ 9.2（複数ステージ）まで実装完了。

| Phase | 状態 | 備考 |
|-------|------|------|
| 1 プロジェクト基盤 | ✅ | .gitignore / 3 シーン登録 |
| 2 コア基盤 | ✅ | GameManager / SceneLoader / GameState |
| 3 入力 & プレイヤー | ✅ | Input System コード定義 / CharacterController |
| 4 地形編集 | ✅ | TerrainData ランタイムクローンで asset 永続化回避 |
| 5 水シミュレーション | ✅ | Rigidbody sphere プール (上限 800) |
| 6 ゴール判定 | ✅ | Trigger Collider、到達時即リサイクル |
| 7 UI | ✅ | HUD / Pause / Clear |
| 8.1 ライティング | ✅ | 暖色ライト + 砂色 Fog |
| 8.2 BGM/SE | 部分 | **音源ファイル未配置**。AudioManager の Inspector にドロップすれば動作 |
| 8.3 FPS 計測 | ✅ | F キーで切替 |
| 8.4 ビルド | ✅ | README に手順（実ビルドは未実施） |
| 8.5 README | ✅ | |
| 9.1 制限時間 | ✅ | デフォルト 120s、残り 10s で赤 |
| 9.2 複数ステージ | ✅ | StagePreset/StageDatabase + StageApplier。Title に動的ステージ選択 UI |
| 9.3 保存/ロード | ⏳ | **次回ここから** |
| 9.4 Undo | ⏳ | |

## 重要ドキュメント（先に目を通す順）

1. **`DESIGN.md`** — ゲーム仕様・操作・クリア条件
2. **`ARCHITECTURE.md`** — シーン構成・スクリプト役割
3. **`TASKS.md`** — フェーズ毎の進捗
4. **`README.md`** — プレイ方法・操作・ディレクトリ構成・エディタメニュー一覧

## プロジェクトの構造的ルール

### シーンセットアップ方式

**シーン（.unity）の内容は Editor スクリプトで自動構築する**。
各 Phase に対応する `Assets/Editor/PhaseXSetupTool.cs` があり:

- `[InitializeOnLoad]` + `EditorApplication.delayCall` で初回自動実行
- `EditorPrefs` フラグで二重実行を防止
- `[MenuItem("SandWater/Setup Phase X ...")]` で手動再実行可
- 各 Phase は直前の Phase の setup を明示的に呼び出して順序を保証

この方式により、ユーザーが Unity を起動するだけでシーン構築が完了する。
YAML を手書きしない。

### 実行時の自動生成オブジェクト

- `[GameManager]` — `GameManager.AutoBootstrap` が `RuntimeInitializeOnLoadMethod(BeforeSceneLoad)` で生成、DontDestroyOnLoad
- `[InputHandler]` — 同上、`InputHandler.AutoBootstrap`

これらはシーンに配置不要。Bootstrap 以外のシーンからも常に `Instance` で参照できる。

### Phase 追加時のテンプレート

新しい Phase の実装は以下の順で行う:

1. ランタイムスクリプトを `Assets/Scripts/<area>/*.cs` に追加（名前空間 `SandWater.<area>`）
2. 対応する `.cs.meta` を Python の `secrets.token_hex(16)` で生成した GUID 付きで作成
3. `Assets/Editor/PhaseXSetupTool.cs` を作成（前 Phase の `SetupXxx()` を呼び出す）
4. メニュー `SandWater/Setup Phase X ...` を登録
5. `EditorPrefs` フラグ `SandWater.PhaseXSetupDone` を使う
6. `TASKS.md` と `README.md` を更新

### 名前空間マップ

| フォルダ | 名前空間 |
|----------|----------|
| `Scripts/Core/` | `SandWater.Core` |
| `Scripts/Input/` | `SandWater.Inputs`（`UnityEngine.InputSystem` との衝突回避のため複数形） |
| `Scripts/Player/` | `SandWater.Player` |
| `Scripts/Terrain/` | `SandWater.TerrainSystem`（`UnityEngine.Terrain` との混乱回避） |
| `Scripts/Water/` | `SandWater.WaterSim` |
| `Scripts/UI/` | `SandWater.UI` |
| `Editor/` | `SandWater.EditorTools` |

### 命名の癖

- Editor セットアップツールの `AssignSerializedRef` / `AssignRef` ヘルパーで `[SerializeField]` にオブジェクトを自動アタッチ
- UI は **legacy uGUI**（`UnityEngine.UI.Text/Button/Image`）、TMP 非使用。フォントは `Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")`
- シーン GUID は事前生成した固定値（`Bootstrap.unity.meta` など）

## 既知の仕様・注意点

### JIS キーボード対応

`[` `]` は Unity Input System の物理キー判定なので JIS では動かない。
`InputHandler.cs` で `<Keyboard>/leftBracket` / `rightBracket` に加え `q` / `e` / `minus` / `equals` を追加バインド済。
DESIGN.md の操作表にも併記済。

### TerrainData アセットの永続化対策

`TerrainData` はアセットファイル。`SetHeights` を実行すると **アセット自体が書き換わり、次回 Play にも残る**。
`TerrainResetter.Awake` で `Instantiate(terrain.terrainData)` によりクローンを生成し、`Terrain` と `TerrainCollider` に差し替え。これでランタイム編集は clone のみ。
アセット自体が汚れた場合は `SandWater > Reset Main Terrain Data` メニューで初期化。

### ステージシステム（9.2）の設計要点

- **TerrainData は全ステージ共通 1 個**（`Assets/Settings/MainTerrainData.asset`）。ステージごとの差は `StagePreset.initialTerrainHeight`（0〜1 正規化値）で表現。複数 TerrainData は持たない
- `StageDatabase.asset` は **`Assets/Resources/` 配下**（`Resources.Load<StageDatabase>("StageDatabase")` で参照）
- `StageApplier`（Main シーン）は `[DefaultExecutionOrder(100)]` で他コンポーネントより後に `Start` し、`TerrainResetter.SetInitialHeight` 等で値を上書き。TerrainResetter は Awake でランタイムクローンを作るため、そのクローン `_initialHeights` を StageApplier が書き換える
- Title は `StageDatabase` をロードしてボタンを動的生成。テンプレートは `StageButtonTemplate`（非アクティブ）を `Instantiate` で複製
- 選択結果は `GameState.SelectedStage` に保持（`DontDestroyOnLoad` の `GameManager` が保持）
- Main の `Retry` は `ResetForNewPlay` で `SelectedStage` を保持したまま再初期化

### 難易度調整メニュー

9.2 以降、RetuneTool はステージベース:

- `SandWater > Retune for Easier Play` — Stage 1 の値で Main を再適用
- `SandWater > Retune to Stage 2` / `... Stage 3` — 各ステージの値で再適用

内部で `Assets/Resources/StageDatabase.asset` を読んで該当 `StagePreset` のフィールドを使う。Stage パラメータを調整したい場合は `Assets/Stages/StageN.asset` を Inspector で編集してから Retune を実行。

### Unity 6 で非推奨

`Object.FindFirstObjectByType<T>()` → `Object.FindAnyObjectByType<T>()` に置換済。
追加スクリプトでは `FindAnyObjectByType` を使うこと。

### FPS カウンター切替キー

**F キー**（F3 だとノート PC の明度キーに奪われる）。

## Phase 9.3 着手の叩き台

水量・地形の保存 / ロード:

### 要件案

- プレイ中にいつでも「保存」（現在の地形ハイトマップ、経過時間、到達水量、選択ステージ ID を記録）
- 次回プレイ時、「続きから」で最後の保存状態をロード
- スロット複数あると嬉しいが MVP は単一スロット

### 実装方針案

- `SaveSystem`（static / シングルトン）
  - `PersistentDataPath/save.dat` に `BinaryFormatter` か `JsonUtility + Convert.ToBase64String(byte[])` で保存
  - 含める情報:
    - SelectedStage の `name`（`StageDatabase` 内での一致確認用）
    - `TerrainData.GetHeights(0,0,res,res)` の 2D 配列（1D 化して Base64 で圧縮してもよい）
    - `GameState.ElapsedTime` / `WaterReachedCount`
- Pause メニューに「Save」「Load」ボタンを追加（`PauseMenu` を拡張）
- Title に「Continue」ボタンを追加（保存がある場合のみ有効）
- ロード時:
  - `SelectedStage` を名前で検索して復元
  - `LoadMain` 後、`StageApplier` の `Start` で適用完了したあと、さらに `SaveSystem` がハイトマップと GameState を上書き
  - 実行順の都合から `StageApplier.Start` の更に後（`[DefaultExecutionOrder(200)]`）で `SaveLoader` が動くのが無難

### 注意

- 現状のセーブは **TerrainData 全解像度の高さ**（257×257 float）= 約 260KB をそのまま書くとそれなりのサイズ
- 保存中は Pause、ロード中は暗転や Loading UI があると親切
- 保存のトリガーを何にするか（手動ボタンのみ？オートセーブは要らない？）ユーザーに確認推奨

### 進めかた

1. `SaveData` POCO（`[Serializable]`）を定義
2. `SaveSystem.cs`（Save / Load / HasSave）をシングルトン or static で実装
3. `PauseMenu` に Save ボタン追加、`TitleUIController` に Continue ボタン追加
4. `StageApplier` の後段で動く `SaveLoader` を Main シーンに追加（ロードフラグが立っているときだけ動作）
5. `Phase9_3SetupTool` を作る
6. `TASKS.md` / `README.md` / `HANDOFF.md` を更新

## 再開時に Claude にやってもらうこと

このファイルと `DESIGN.md` / `ARCHITECTURE.md` / `TASKS.md` / `README.md` を読んでから:

- ユーザーが「Phase 9.3 をやりましょう」と言ったら、**Phase 9.3 着手の叩き台** セクションを起点に進める
- 既存の Phase 6〜9.2 で採用したパターン（SetupTool / EditorPrefs フラグ / SerializeField 自動ワイヤ）を踏襲
- Terrain / Water / UI のいずれかを触るなら、既存の同系スクリプトの書き方に合わせる

困ったら `SandWater > ...` 配下のメニュー一覧（`README.md` 参照）で現状を確認可能。
