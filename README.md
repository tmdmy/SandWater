# SandWater

Unity 6 (URP) で作る一人称視点の砂場シミュレーター。砂の地形を削って水路を作り、水源から流れる水をゴールまで導くとクリア。

## 動作環境

- Unity **6000.4.2f1** 以降（URP テンプレート）
- Package:
  - `com.unity.render-pipelines.universal`
  - `com.unity.inputsystem`
  - Terrain Tools Sample Asset Pack（Asset Store 提供）

## セットアップ

1. プロジェクトを Unity Hub から開く
2. 初回起動時に `SandWater/Setup Phase X` 系のエディタ自動セットアップが走り、3 シーン（Bootstrap / Title / Main）が構築される
3. 自動セットアップが途中で止まった場合は、メニュー `SandWater > Setup Phase 9.2 Stages` を手動実行すると Phase 2〜9.2 すべてが順に適用される
4. Bootstrap シーンを開いて Play ボタン
5. Title でプレイしたいステージを選んで **Start**

## 操作方法

| 操作 | 入力 |
|------|------|
| 移動 | W / A / S / D |
| 視点移動 | マウス |
| ジャンプ | Space |
| 地形を削る | 左クリック（長押しで連続） |
| 地形を盛る | 右クリック（長押しで連続） |
| ブラシ半径 拡大 / 縮小 | マウスホイール |
| ブラシ強度 − / + | `[` `]` または `Q` / `E`（JIS 配列向け） |
| ポーズ | Esc |
| 地形リセット | R |

## クリア条件

- 水源（青シリンダー）から流れる水がゴール（緑シリンダー、Trigger）に到達
- 到達粒子数が **規定数（デフォルト 10）** に達するとクリア画面が表示される

## ディレクトリ構成

```
Assets/
├── Editor/              エディタ自動セットアップツール群
├── Scenes/              Bootstrap / Title / Main
├── Scripts/
│   ├── Core/            GameManager / GameState / SceneLoader / AudioManager
│   ├── Input/           InputHandler
│   ├── Player/          PlayerController / PlayerCamera
│   ├── Terrain/         TerrainBrush / TerrainEditor / TerrainHeightSampler / TerrainResetter / BrushIndicator
│   ├── Water/           WaterSimulator / WaterParticle / WaterSource / GoalZone
│   └── UI/              HUDController / PauseMenu / ClearScreen / TitleUIController
├── Settings/            URP / TerrainData asset
└── TerrainSampleAssets/ Terrain Tools Sample Asset Pack
```

## エディタメニュー

| メニュー | 内容 |
|----------|------|
| `SandWater > Setup Phase 2 Scenes` | Title シーンに Start ボタン UI を構築 |
| `SandWater > Setup Phase 3 Main Scene` | Main シーンに Player + Ground を配置 |
| `SandWater > Setup Phase 4 Terrain` | 砂地 Terrain を生成、編集システム一式を配置 |
| `SandWater > Setup Phase 5 Water` | WaterSimulator + WaterSource を配置 |
| `SandWater > Setup Phase 6 Goal` | GoalZone を配置 |
| `SandWater > Setup Phase 7 UI` | HUD / Pause / Clear の Canvas を構築 |
| `SandWater > Setup Phase 8 Polish` | ライティング・Fog 調整、AudioManager 配置 |
| `SandWater > Setup Phase 9.1 Time Limit` | 制限時間・FailScreen の配置 |
| `SandWater > Setup Phase 9.2 Stages` | StagePreset 3 種・StageDatabase 生成、Title にステージ選択 UI、Main に StageApplier 配置 |
| `SandWater > Reset Main Terrain Data` | TerrainData アセットを初期高さに戻す |
| `SandWater > Retune for Easier Play` | Stage 1 の値で Main を再適用（地形高さ・水源・ゴール・ブラシ・制限時間） |
| `SandWater > Retune to Stage 2` / `... Stage 3` | 各ステージの値で Main を再適用 |

## オーディオ

**現状: 音源ファイル未割り当て（後回し）**。AudioManager の配線と呼び出しロジックは完成しており、クリップをアタッチすれば即座に鳴る状態。

`[AudioManager]` オブジェクトの Inspector で AudioClip スロットに任意のクリップをアタッチすると BGM / SE が鳴る:

- `Dig Clip` — 削るときの音
- `Raise Clip` — 盛るときの音
- `Clear Clip` — クリア音
- `Bgm Clip` — メインシーン BGM（自動ループ再生）

クリップ入手の候補:
- freesound.org / pixabay.com/sound-effects / kenney.nl（無料・商用可のライセンスを確認）
- Unity Asset Store の無料 SFX パック

ファイルを `Assets/Audio/` 配下に配置 → Project ウィンドウから Inspector のスロットにドラッグ&ドロップ → `Ctrl+S` で保存。

## パフォーマンス

- HUD 左上に FPS 表示、**F** キーで表示 / 非表示を切り替え
- 主な負荷要因:
  - 水粒子数（上限 `WaterSimulator.maxParticles` = 800）
  - 地形編集の `SetHeightsDelayLOD` 呼び出し頻度（毎フレーム、`SyncHeightmap` 実行）
  - 粒子 Rigidbody 物理コスト（`CollisionDetectionMode.ContinuousSpeculative`）

### プロファイル確認手順

1. **Window → Analysis → Profiler** を開く
2. Play モードで `Record` を有効化
3. Main シーンに遷移し、水を出しながら地形を編集
4. 確認ポイント:
   - CPU Usage: `PhysX` が急増していないか（粒子数過多の場合は `maxParticles` を下げる）
   - CPU Usage: `Rendering` が 16ms（60fps）を超えていないか
   - Memory: Heap が過度に増え続けないか（プール回収が正しく動いているか）

### 負荷を下げたいとき

- `WaterSimulator.maxParticles` を 400〜600 に下げる
- `WaterSimulator.particleLifetime` を 10 秒以下に短縮
- `WaterSource.spawnRatePerSecond` を 20 程度まで下げる
- Terrain 解像度を下げる（`MainTerrainData.heightmapResolution` を 129 など）

## ビルド（Windows スタンドアロン）

### 事前チェック

- Build Settings にシーンが `Bootstrap` → `Title` → `Main` の順で登録されていること（Phase 1 で設定済み）
- Console に赤いエラーがないこと
- Project Settings → Player:
  - `Product Name`: SandWater
  - `Company Name`: 任意
  - `Default Is Full Screen`: 用途に応じて

### 手順

1. **File → Build Profiles**（Unity 6 の新しいメニュー）または **File → Build Settings**
2. Platform で `Windows, Mac, Linux` を選択、Target Platform `Windows`、Architecture `x86_64`
3. `Build` ボタンをクリック
4. 出力先フォルダ（例: `Builds/Windows/`）を選択
5. 生成された `SandWater.exe` を実行して動作確認

### 確認項目

- Bootstrap → Title → Main への自動遷移
- Title で Start ボタンが押せる
- Main で地形編集、水流、ゴール到達、クリア画面
- Esc でポーズ、Retry / Quit
- Quit で終了

## ライセンス / アセット

- Terrain Sample Asset Pack: Unity Technologies（Asset Store）提供ライセンスに従う
- ソースコード: プロジェクト内のコードは内部プロジェクトとして扱う

## 今後の拡張候補（Phase 9）

- ~~制限時間モード~~（9.1 完了）
- ~~複数ステージ / 地形プリセット~~（9.2 完了：Title からステージ選択）
- 水量・地形の保存 / ロード
- 編集のアンドゥ機能
