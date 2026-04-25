# TASKS.md — SandWater 実装タスクリスト

最終更新: 2026-04-16（Phase 9.2 まで完了）

凡例: `[x]` 完了 / `[~]` 部分完了 / `[ ]` 未着手

## Phase 1: プロジェクト基盤

- [x] 1.1 Unity プロジェクト（URP テンプレート）を確認し、.gitignore を整備
- [x] 1.2 Package Manager から Input System と Terrain Tools Sample Asset Pack を導入（URP テンプレートに同梱済）
- [x] 1.3 `Assets/Scripts/` 配下のフォルダ構成（Core / Player / Terrain / Water / UI / Input）を作成
- [x] 1.4 `Assets/Scenes/` に `Bootstrap.unity` `Title.unity` `Main.unity` を作成し Build Settings に登録

## Phase 2: コア基盤

- [x] 2.1 `GameState.cs` を実装（経過時間・到達水量・クリアフラグ）
- [x] 2.2 `GameManager.cs` をシングルトンで実装（DontDestroyOnLoad、`RuntimeInitializeOnLoadMethod` で自動生成）
- [x] 2.3 `SceneLoader.cs` で Bootstrap → Title → Main の遷移を実装
- [x] 2.4 Title シーンに最小 UI（Start ボタン）を配置して Main へ遷移できることを確認

## Phase 3: 入力とプレイヤー

- [x] 3.1 `InputHandler.cs` を Input System の Actions で実装（コード内で InputAction を定義、asset 非依存）
- [x] 3.2 `PlayerController.cs`（CharacterController ベース、WASD + ジャンプ + 重力）
- [x] 3.3 `PlayerCamera.cs`（マウスルック、Cursor ロック）
- [x] 3.4 Main シーンで歩き回れることを確認

## Phase 4: 地形編集

- [x] 4.1 Main シーンに砂地 Terrain を配置（Sand_TerrainLayer）
- [x] 4.2 `TerrainHeightSampler.cs` でレイキャスト → ハイトマップ座標変換
- [x] 4.3 `TerrainBrush.cs`（サイズ・強度・フォールオフ、InputHandler イベント購読）
- [x] 4.4 `TerrainEditor.cs` で `TerrainData.SetHeightsDelayLOD` による削る／盛る
- [x] 4.5 `BrushIndicator.cs` で LineRenderer の円ブラシ範囲可視化
- [x] 4.6 `TerrainResetter.cs` で初期ハイトマップ保持 & R キー復元（実行時 `TerrainData` クローンで asset 永続化を防止）

## Phase 5: 水シミュレーション

- [x] 5.1 `WaterSource.cs` で一定レート粒子生成
- [x] 5.2 `WaterParticle.cs`（Rigidbody + SphereCollider、低摩擦 PhysicsMaterial）
- [x] 5.3 `WaterSimulator.cs` でプール管理（上限 800、寿命 15s、落下リサイクル）
- [x] 5.4 水の見た目（URP Lit 水色、半径 0.1）
- [x] 5.5 動作確認済み

## Phase 6: ゴール判定とクリア

- [x] 6.1 `GoalZone.cs` を Trigger Collider で実装、到達時に即リサイクル（重複カウント防止）
- [x] 6.2 しきい値到達で `GameManager.NotifyCleared()` 発火
- [x] 6.3 水源（青シリンダー）とゴール（緑シリンダー）のマーカーを配置

## Phase 7: UI

- [x] 7.1 `HUDController.cs` で時間・ゴール進捗（ゲージ含む）・ブラシ情報
- [x] 7.2 クロスヘアを画面中央に配置
- [x] 7.3 `PauseMenu.cs`（Esc トグル、Resume / Retry / Quit、Time.timeScale 制御）
- [x] 7.4 `ClearScreen.cs`（クリア演出、経過時間表示、Retry ボタン）
- [x] 7.5 Retry は `SceneLoader.LoadMain` で再読み込み → `GameState.ResetForNewPlay` + `TerrainResetter` 再クローン

## Phase 8: 仕上げ

- [x] 8.1 URP ライティング調整（暖色 Directional Light + Linear Fog 砂色）
- [~] 8.2 BGM / SE — **音源ファイルは後回し**。`AudioManager` の配線・呼び出しロジック完成済み、Inspector でクリップをアタッチすれば即動作
- [x] 8.3 パフォーマンス確認 — FPS 表示（**F キー**切替）+ README にプロファイル手順と負荷低減策を記載
- [x] 8.4 ビルド確認（Windows スタンドアロン）— README に事前チェック・手順・確認項目（実ビルドはユーザー作業）
- [x] 8.5 README にプレイ方法と操作を記載

## Phase 9: 拡張（任意）

- [x] 9.1 制限時間モード — `SessionConfig`（デフォルト 120s）+ `GamePhase.Failed` + `FailScreen` UI、HUD カウントダウン表示（残り 10 秒で赤）
- [x] 9.2 複数ステージ（地形プリセットの切替）— `StagePreset`/`StageDatabase`（Resources 配下）+ `StageApplier`（Main で Start 時適用）+ Title ステージ選択 UI、既定 3 ステージ（Easy/Longer/High Plateau）
- [ ] 9.3 水量・地形の保存 / ロード
- [ ] 9.4 編集のアンドゥ機能
