# ARCHITECTURE.md — SandWater アーキテクチャ

## 1. シーン構成

### 1.1 Bootstrap シーン（`Bootstrap.unity`）
- ゲーム起動時に最初にロードされる
- グローバルマネージャー（`GameManager` 等）を生成して DontDestroyOnLoad
- タイトルシーンへ遷移

### 1.2 タイトルシーン（`Title.unity`）
- タイトル画面の UI
- Start ボタンでメインシーンへ遷移

### 1.3 メインシーン（`Main.unity`）
構成オブジェクト:
- `Terrain`（Terrain Tools で編集対象となる砂地）
- `Player`（FPS コントローラー + カメラ）
- `WaterSource`（水源：Particle System / 水スポーン）
- `GoalZone`（Trigger Collider、到達判定）
- `WaterSimulator`（水の流れ管理）
- `EnvironmentLighting`（URP ライティング・Skybox）
- `UIRoot`（HUD / ポーズ / クリア画面の Canvas）
- `GameManager`（Bootstrap から持ち越し）

## 2. スクリプト一覧

`Assets/Scripts/` 配下に機能単位のフォルダで整理する。

```
Assets/Scripts/
├── Core/
│   ├── GameManager.cs
│   ├── SceneLoader.cs
│   ├── GameState.cs
│   ├── SessionConfig.cs
│   ├── AudioManager.cs
│   ├── StagePreset.cs
│   ├── StageDatabase.cs
│   └── StageApplier.cs
├── Player/
│   ├── PlayerController.cs
│   └── PlayerCamera.cs
├── Terrain/
│   ├── TerrainEditor.cs
│   ├── TerrainBrush.cs
│   ├── TerrainHeightSampler.cs
│   └── TerrainResetter.cs
├── Water/
│   ├── WaterSource.cs
│   ├── WaterSimulator.cs
│   ├── WaterParticle.cs
│   └── GoalZone.cs
├── UI/
│   ├── HUDController.cs
│   ├── PauseMenu.cs
│   ├── ClearScreen.cs
│   └── BrushIndicator.cs
└── Input/
    └── InputHandler.cs
```

## 3. 各スクリプトの役割

### Core
| スクリプト | 役割 |
|-----------|------|
| `GameManager.cs` | シングルトン。ゲーム全体の状態（Playing / Paused / Cleared）を保持。各マネージャーへの参照を束ねる。 |
| `SceneLoader.cs` | シーン遷移を非同期でラップ。Bootstrap → Title → Main の流れを管理。 |
| `GameState.cs` | 現在のプレイ時間・到達水量・クリアフラグ・`SelectedStage` などランタイム状態を保持する POCO。 |
| `SessionConfig.cs` | 制限時間のシーン側フォールバック。`SetTimeLimit` で `GameState.TimeLimit` を上書きする。 |
| `StagePreset.cs` | ScriptableObject。ステージごとの地形初期高さ・水源/ゴール座標・制限時間・ブラシ初期値を保持。 |
| `StageDatabase.cs` | ScriptableObject。`StagePreset` のリスト。`Assets/Resources/StageDatabase.asset` から `Resources.Load` で取得。 |
| `StageApplier.cs` | Main シーン配置。`[DefaultExecutionOrder(100)]` で他コンポーネント Start より後に `SelectedStage` を各コンポーネントへ適用。 |

### Player
| スクリプト | 役割 |
|-----------|------|
| `PlayerController.cs` | WASD 移動・ジャンプ・重力。CharacterController を使用。 |
| `PlayerCamera.cs` | マウスルック・FOV 制御。`Cursor.lockState` 管理。 |

### Terrain
| スクリプト | 役割 |
|-----------|------|
| `TerrainEditor.cs` | 左右クリック入力を受けてカーソル下の Terrain ハイトマップに `TerrainPaintUtility` / `PaintContext` で編集を適用。 |
| `TerrainBrush.cs` | ブラシサイズ・強度・フォールオフを保持。マウスホイールと `[` `]` で調整。 |
| `TerrainHeightSampler.cs` | レイキャストで Terrain 上の座標を取得し、ハイトマップ座標への変換を担当。 |
| `TerrainResetter.cs` | 初期ハイトマップを保持し、R キーやリトライ時に復元する。 |

### Water
| スクリプト | 役割 |
|-----------|------|
| `WaterSource.cs` | 一定レートで水（ParticleSystem または水セル）を生成。 |
| `WaterSimulator.cs` | 全水要素の更新を束ねる。重力・地形高さサンプリングによる流下処理。MVP では Unity Particle + 物理コライダーに任せる選択肢も。 |
| `WaterParticle.cs` | 個別の水粒子の速度・寿命・地形との衝突処理。 |
| `GoalZone.cs` | Trigger Collider で水到達を検出。カウントがしきい値に達したら `GameManager` にクリア通知。 |

### UI
| スクリプト | 役割 |
|-----------|------|
| `HUDController.cs` | 経過時間・到達水量ゲージ・ブラシ情報を表示。 |
| `PauseMenu.cs` | Esc でトグル。Resume / Retry / Quit ボタンを処理。 |
| `ClearScreen.cs` | クリア演出と結果表示、リトライ導線。 |
| `BrushIndicator.cs` | Terrain 上にブラシの円（Decal または LineRenderer）を描画。 |

### Input
| スクリプト | 役割 |
|-----------|------|
| `InputHandler.cs` | Unity Input System をラップし、各システムが購読するイベント（Dig / Raise / BrushResize / Pause 等）を発火。 |

## 4. データフロー概略

```
InputHandler
   ├─► PlayerController / PlayerCamera
   ├─► TerrainEditor ──► Terrain ハイトマップ更新
   └─► PauseMenu

WaterSource ──► WaterSimulator ──► WaterParticle ──► GoalZone ──► GameManager(Clear)
                                         ▲
                                    Terrain (高さ参照)

GameManager ──► HUDController / ClearScreen / TerrainResetter
```

## 5. 依存アセット / パッケージ

- Universal Render Pipeline (URP)
- Terrain Tools Sample Asset Pack（ブラシテクスチャ・`TerrainPaintUtility`）
- Input System Package
- TextMeshPro（UI）
