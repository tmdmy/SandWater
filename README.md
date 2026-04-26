# SandWater

Unity 6 (URP) で作る一人称視点の砂漠サンドボックスパズル。

**枯れた砂漠の村に水を取り戻す物語**。プレイヤーは砂を削って水路を作り、地中から噴き出す泉の水を村の井戸へ導く。井戸が満たされた瞬間、村の周りに緑が広がり花が咲き、枯れ木に葉が生える。

## 動作環境

- Unity **6000.4.2f1** 以降（URP テンプレート）
- 必須パッケージ（manifest.json で管理済）:
  - `com.unity.render-pipelines.universal`
  - `com.unity.inputsystem`
- AssetStore から手動再インポート必要（**.gitignore で除外**）:
  - **Terrain Tools Sample Asset Pack** — 砂テクスチャ
  - **Pandazole - Nature Environment Low poly Pack** — 植物・木・骨など装飾

## 初回セットアップ

### 1. プロジェクトを Unity Hub で開く
- Library 再生成に 5〜10 分かかる

### 2. Terrain Tools サンプルを再インポート
- Window → Package Management → Package Manager
- 上部「Packages: 」を **In Project** に切替
- **Terrain Tools** → **Samples** タブ → **Sample Asset Pack** → **Import**

### 3. Pandazole パックを再インポート
- Window → Asset Store →「Pandazole - Nature Environment Low poly Pack」を取得
- インポート後、**URP マテリアル変換**が必須:
  - Window → Rendering → Render Pipeline Converter
  - Source: `Built-in` / Target: `Universal Render Pipeline`
  - **Material Reference Converter** + **Material Shader Converter** にチェック
  - **Scan** → **Convert Assets**

### 4. シーン参照の自動復元
Pandazole / TerrainSampleAssets のプレハブ GUID は固定なので、再インポートだけで `[CustomDecorations]` などの参照が自動復元される。

## 起動

1. **Bootstrap** シーンを開く（自動で Title へ遷移）
2. ▶︎ Play
3. Title 画面で **Stage 1 / 2 / 3** を選択 → **Start**

## 操作方法

| 操作 | 入力 |
|------|------|
| 移動 | W / A / S / D |
| 視点移動 | マウス |
| ジャンプ | Space |
| 地形を削る（水路を堀る） | 左クリック（長押しで連続） |
| 地形を盛る | 右クリック（長押しで連続） |
| ブラシ半径 拡大 / 縮小 | マウスホイール |
| ブラシ強度 − / + | `[` `]` または `Q` / `E`（JIS 配列向け） |
| ポーズメニュー | Esc |
| 地形リセット | R |
| FPS 表示切替 | F |

**ESC ポーズメニュー** には **Resume / Retry / Stage Select / Quit** がある。Stage Select でいつでもタイトルに戻ってステージ変更可能。

## ゲーム内容

### 物語

砂漠の村で井戸が枯れ、村人は困っている。地下から噴き出す泉の水を、地形を削って井戸まで導こう。

### クリア条件

地面表面から斜め上 37° に噴き出す水を村の井戸（GoalZone）に到達させる。規定数 **10 個** の粒子が井戸に届くと、

- 井戸の水位がじわじわ上昇（ゴール進捗 0→100%）
- **しきい値到達で「村が一斉に蘇る」演出が発火**:
  - 隠してた緑の植物・花・葉・草が一斉に出現
  - 枯れ木・骨・棘 が一斉に消失
  - 緑のオアシスへの劇的な変化

### ステージ構成

| ステージ | 水源 X | ゴール X | 距離 | 制限時間 |
|----------|-------|---------|------|---------|
| Stage 1 - Easy Channel | -15 | +8 | 23 単位 | 120 秒 |
| Stage 2 - Longer Run | -25 | +8 | 33 単位 | 150 秒 |
| Stage 3 - High Plateau | -35 | +8 | 43 単位 | 180 秒 |

唯一の難易度差は **水源とゴールの距離**。地形・水の勢い・必要水量・ブラシは全ステージ共通。

## ディレクトリ構成

```
Assets/
├── Editor/                         エディタ自動セットアップツール群（Phase 2〜13）
├── Scenes/                         Bootstrap / Title / Main
├── Scripts/
│   ├── Core/                       GameManager / GameState / SceneLoader / AudioManager
│   │                               StagePreset / StageDatabase / StageApplier / SessionConfig
│   ├── Input/                      InputHandler
│   ├── Player/                     PlayerController / PlayerCamera
│   ├── Terrain/                    TerrainBrush / TerrainEditor / TerrainHeightSampler
│   │                               TerrainResetter / BrushIndicator
│   ├── Water/                      WaterSimulator / WaterParticle / WaterSource (噴水化) / GoalZone
│   └── UI/                         HUDController / PauseMenu (Stage Select 付き)
│                                   ClearScreen / FailScreen / FpsCounter / TitleUIController
├── MarchingCubes/                  Phase 10: 滑らかな水面メッシュ生成
│   └── Scripts/                    FluidDensityField / MarchingCubesGenerator
│                                   MarchingCubesTables / FluidMeshRenderer
├── WitheredVillage/                Phase 12+13: 村テーマ + 蘇生機構
│   └── Scripts/                    WellWaterLevel / VillageRevival / VillageGroundAnchor
├── Settings/                       URP 設定 / MainTerrainData
├── Stages/                         Stage1〜3 (StagePreset asset)
├── Resources/                      StageDatabase
└── (要再インポート、.gitignore 済)
    ├── TerrainSampleAssets/        砂テクスチャ
    └── Pandazole_Ultimate_Pack/    Low Poly 装飾プレハブ
```

## エディタメニュー

### 自動構築（順次実行 / 手動再実行可）

| メニュー | 内容 |
|----------|------|
| `SandWater > Setup Phase 2 Scenes` | Title シーンに Start ボタン UI |
| `SandWater > Setup Phase 3 Main Scene` | Main シーンに Player + Ground |
| `SandWater > Setup Phase 4 Terrain` | 砂地 Terrain + 編集システム |
| `SandWater > Setup Phase 5 Water` | WaterSimulator + WaterSource |
| `SandWater > Setup Phase 6 Goal` | GoalZone |
| `SandWater > Setup Phase 7 UI` | HUD / Pause / Clear |
| `SandWater > Setup Phase 8 Polish` | ライティング・Fog、AudioManager |
| `SandWater > Setup Phase 9.1 Time Limit` | 制限時間 + FailScreen |
| `SandWater > Setup Phase 9.2 Stages` | StagePreset 3 種、Title ステージ選択 UI |
| `SandWater > Setup Phase 12 Withered Village` | 井戸ゴール + 家 + 枯れ装飾（プリミティブ） |
| `SandWater > Setup Phase 13 Asset Village` | Pandazole で装飾自動配置 |

### パッチ / リセット / 補助

| メニュー | 内容 |
|----------|------|
| `SandWater > Reset Main Terrain Data` | TerrainData を平らな初期高さに戻す |
| `SandWater > Strip Phase 12 Decorations (keep well + houses)` | 装飾だけ消去（井戸 + 家は残す） |
| `SandWater > Add Stage Select Button to Pause Menu` | 既存 PauseMenu に Stage Select 追加 |
| `SandWater > Retune for Easier Play` / `... Stage 2` / `... Stage 3` | 各ステージ値で Main 再適用 |

## アーキテクチャ要点

### Marching Cubes による水描画

- 800 個の水粒子から 3D 密度場を毎フレーム構築 → MC アルゴリズムでアイソサーフェス抽出 → URP/Lit 透過で描画
- 頂点法線は密度勾配から計算した滑らかなシェーディング
- パラメータ: Field Center (0,7,0), Size (40,14,40), Grid 48x28x48, Influence 1.0, Iso 0.15
- `[FluidMesh]` GameObject 配下で動作

### 水源（地面表面の泉）

- 地面表面 (Y=9) から斜め上 37° に勢いよく噴出
- `WaterSource`: spawn rate 80/秒、velocity (4, 3, 0)、velocitySpreadDegrees 3°
- Rigidbody 物理でリアルな放物線

### 村の蘇生機構

`VillageRevival.cs` が `GoalZone.Progress` を監視。しきい値（デフォルト 0.99）到達で 1 回だけ発火:

- **HiddenOnStart**: 蘇生で `SetActive(true)` ＝ 出現
- **HideOnRevival**: 蘇生で `SetActive(false)` ＝ 消失
- 任意でマテリアル swap（withered → alive）

### 地形追従

`VillageGroundAnchor.cs` が Start 時に各子オブジェクトの Y を `Terrain.SampleHeight` にスナップ。`[Village]` と `[CustomDecorations]` に同コンポーネントを付与することで、ステージ間で家やプレハブが浮かない / 埋まらない。

## カスタマイズ

### 装飾を手動配置する

`[CustomDecorations]` 配下が手動配置エリア（Phase 13 SetupTool は触らない）:

1. Hierarchy で `[CustomDecorations]` を選択（`[Village]` の子）
2. Project から Pandazole プレハブを Scene ビューにドラッグ
3. **W / E / R** キーで位置・回転・スケール調整
4. 蘇生時の挙動を設定:
   - **GoalZone** をクリック → Inspector の **VillageRevival**
   - **Hidden On Start** に追加 = 蘇生で出現
   - **Hide On Revival** に追加 = 蘇生で消失

Pandazole プレハブの分類例:
- 枯れ系（Hide On Revival 推奨）: `Bones_*` / `Thorns_*` / `TreeTrunk_*`
- 蘇生系（Hidden On Start 推奨）: `Bush_*` / `Cactus_*` / `Flower_*` / `Grass_*` / `Tree_*_Spring`

### ステージ設定を変える

`Assets/Stages/Stage1-3.asset` を Inspector で編集:

| フィールド | 説明 |
|-----------|------|
| `waterSourcePosition` | 水源の世界座標 |
| `waterInitialVelocity` | 噴出方向 + 勢い (magnitude が速度) |
| `waterSpawnRatePerSecond` | 1 秒あたりの粒子生成数 |
| `goalPosition` | 井戸の位置 |
| `goalBoxSize` | ゴール検出範囲 (Y を大きくすると深く掘っても判定) |
| `goalRequiredCount` | 必要水量 |
| `timeLimitSeconds` | 制限時間 |
| `brushInitialRadius` / `Strength` | ブラシの初期パラメータ |
| `initialTerrainHeight` | 開始時の地形高さ (0〜1 正規化) |

## オーディオ

**現状: 音源ファイル未割り当て** （AudioManager の配線・呼び出しロジックは完成）

`[AudioManager]` の Inspector で AudioClip スロットにアタッチすれば即動作:
- `Dig Clip` — 削るときの音
- `Raise Clip` — 盛るときの音
- `Clear Clip` — クリア音
- `Bgm Clip` — メインシーン BGM（自動ループ）

無料音源候補: [freesound.org](https://freesound.org/) / [pixabay.com/sound-effects](https://pixabay.com/sound-effects/) / [kenney.nl](https://kenney.nl/)

## パフォーマンス

- HUD 左上の FPS 表示。**F キー** で表示切替
- 主な負荷要因:
  - 水粒子（上限 `WaterSimulator.maxParticles` = 800）
  - Marching Cubes 計算（CPU 単一スレッド、48x28x48 グリッド）
  - 地形編集 `SetHeightsDelayLOD` 呼び出し
  - Rigidbody 物理コスト

### 負荷を下げる時

| 対象 | 設定場所 | 推奨値 |
|------|---------|--------|
| 水粒子数 | `WaterSimulator.maxParticles` | 400〜600 |
| 粒子寿命 | `WaterSimulator.particleLifetime` | 10 秒以下 |
| 水源スポーン | `WaterSource.spawnRatePerSecond` | 40〜60 |
| MC グリッド解像度 | `[FluidMesh]` → FluidMeshRenderer.gridResolution | (32, 16, 32) |
| 地形解像度 | `MainTerrainData.heightmapResolution` | 129 |

## ビルド（Windows スタンドアロン）

### 事前チェック

- Build Settings にシーンが `Bootstrap` → `Title` → `Main` の順で登録
- Console に赤いエラーが無いこと
- Player Settings:
  - Product Name: SandWater
  - Default Is Full Screen: 用途に応じて

### 手順

1. **File → Build Profiles** （または Build Settings）
2. Platform: `Windows, Mac, Linux` / Target: `Windows` / Arch: `x86_64`
3. `Build` → 出力先選択
4. 生成された `SandWater.exe` を実行

### 確認項目

- Bootstrap → Title → Main への遷移
- Title でステージ選択 → Start
- Main で地形編集、水流、ゴール到達、**村の蘇生演出**、クリア画面
- ESC で Pause、**Stage Select で Title 戻り**
- 各ステージプレイ可能

## ライセンス / アセット

- **Terrain Sample Asset Pack**: Unity Technologies — AssetStore EULA
- **Pandazole - Nature Environment Low poly Pack**: Pandazole — AssetStore EULA
- **MarchingCubes 実装**: 自前実装、Paul Bourke の標準 LUT 使用（public domain）
- **コード**: プロジェクト内コードは内部プロジェクトとして扱う

## 開発履歴

| Phase | 内容 |
|-------|------|
| 1〜8 | プロジェクト基盤・コア・入力・地形編集・水シミュ・ゴール・UI・仕上げ |
| 9.1 | 制限時間モード |
| 9.2 | 複数ステージ（StagePreset / StageDatabase / StageApplier） |
| 10 | **Marching Cubes** による滑らかな水面メッシュ描画 |
| 12 | **枯れた村テーマ** + 井戸ゴール (水位視覚化) + **蘇生演出** |
| 13 | **Pandazole AssetStore** で Low Poly 装飾化 + HideOnRevival |
| ＋ | **地面噴水スタイル** + ステージ統一 + **ESC Stage Select** + VillageGroundAnchor |

## 今後の拡張候補

- 音源ファイル配置（AudioManager 配線済）
- 保存 / ロード機能（旧 Phase 9.3）
- 編集の Undo（旧 Phase 9.4）
- 第 4 / 5 ステージ（高低差・障害物・複数ゴール）
- 環境演出（風で舞う砂、蘇生時の蝶のパーティクル、波紋）
