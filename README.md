# PF_SourceCodeList <!-- omit in toc -->
対戦レースゲーム「Project-F」という作品で利用したプログラムについての解説を行っています。

ソースコードはすべてC#で記述されています。

## 実行環境
このリポジトリ内のC#プログラムは以下の環境で制作しました。
- Unity 2019.4
- Visual Studio 2021

# ソースコードリスト <!-- omit in toc -->
- [プレイヤー挙動](#プレイヤー挙動)
  - [PlayerMove.cs](#playermovecs)
  - [CustomPlayerMoveInspector.cs](#customplayermoveinspectorcs)
- [ボタン入力](#ボタン入力)
  - [GamePadManager.cs](#gamepadmanagercs)
- [データ管理](#データ管理)
  - [CharaData.cs](#charadatacs)
  - [StageData.cs](#stagedatacs)
- [エディタ拡張](#エディタ拡張)
  - [DebugRaceData.cs](#debugracedatacs)


<!-- プログラム解説 -->
# プレイヤー挙動
## PlayerMove.cs
### 解説 <!-- omit in toc -->
キャラクターが移動する際の挙動を計算しているプログラムです。
作品中では、車両ではなく人間を走らせるプログラムとして利用しています。

コード内に出てくるタイヤ、サスペンションなどの車両シミュレーションを人間の足、関節に置き換えて処理を行っています。
イメージしやすいように下記に一覧を記述しています。

|車両|人間|
|:--:|:--:|
|タイヤ|靴|
|タイヤの直径|足の長さ|
|サスペンション|足の関節|

実際のゲーム中では、外部のプログラムからアクセルやブレーキ等の入力を受け取った後、アシスト等でデータを調整し、パワーや旋回量の計算を行っています。

### 工夫した点 <!-- omit in toc -->
- 挙動でキャラクターが走っているリアリティを演出するために、車両シミュレーションを実装しました。地面と接している面の摩擦がどのようになっているのか、パワーをどのように地面に伝えるのかを表現するコードが簡潔になるように記述しました。
- ドライビングシミュレータに苦手意識を持っている方にも気軽に楽しめる作品に仕上げるために、モーメントによる挙動変化をマイルドに調整する等、リアリティと遊びやすさを両立するように心がけました。
- 4人対戦かつアクションゲームという高負荷がかかる中で、複雑な車両プログラムをスムーズに実行できるように、キャッシュの利用や数学関数等の負荷のかかる記述を減らすことで対策しました。

## CustomPlayerMoveInspector.cs
### 解説 <!-- omit in toc -->
PlayerMove.csのパラメータをUnityエディタのInspector内で編集しやすくするための機能をまとめたプログラムです。
このプログラムで実装している機能は以下の通りです。

|機能|内容|
|:--:|:--:|
|パワー設定|<img src="Docs/Images/PlayerMoveCtrl_Power.jpg" width="300">|
|ギア比設定|<img src="Docs/Images/PlayerMoveCtrl_Gear.jpg" width="300">|
|グリップ設定|<img src="Docs/Images/PlayerMoveCtrl_Grip.jpg" width="300">|

### 工夫した点 <!-- omit in toc -->
- トルクカーブのグラフやギア比から速度がどの程度出るのかを表示するようにしており、数値をどの程度変えると良いのかがわかりやすくなるように配慮しました。

# ボタン入力
## GamePadManager.cs
### 解説 <!-- omit in toc -->
パソコンに接続されているコントローラの種類に応じてボタン入力の対応先を自動で設定する機能をまとめたプログラムです。

### 工夫した点 <!-- omit in toc -->
- 開発エンジンとして利用していた Unity の入力システムでは、
ボタン入力の識別番号をコントローラの種類によって再設定する必要があり、外部の変換ドライバを利用するか複雑なボタン設定の画面を開いて手動で一つずつ設定する手間がかかっていました。

- その問題を解決するために制作したものがこのプログラムです。

- このプログラムを利用すればユーザはコントローラを接続するだけで、複雑な設定をすることなくゲームを遊べるようになります。

# データ管理
## CharaData.cs
<img src="Docs/Images/CharaData_inspector.jpg" width="300">

### 解説 <!-- omit in toc -->
キャラクターの3Dモデルや挙動データなどをキャラクターごとにまとめたデータ管理用のプログラムです。

## StageData.cs
<img src="Docs/Images/StageData_inspector.jpg" width="300">

### 解説 <!-- omit in toc -->
ステージの3Dモデルや再生するBGM、キャラクターの初期位置やギミックの配置等をまとめたデータ管理用のプログラムです。

### 工夫した点 <!-- omit in toc -->
- データ管理のプログラムでは、キャラクター挙動プログラムのように実行の速さよりも、拡張や修正のしやすさを優先して実装しました。


# エディタ拡張
## DebugRaceData.cs
<img src="Docs/Images/Debug_rule.jpg" width="300">

### 解説 <!-- omit in toc -->
制作時のデバッグ機能をまとめたプログラムです。パラメータ設定の自動化や、作業のショートカット設定をまとめることで作業時間の短縮を実現しています。

このプログラムで実装している機能は以下の通りです。

|機能|内容|
|:--:|:--:|
|ルール設定|<img src="Docs/Images/Debug_rule.jpg" width="300">|
|キャラクター設定|<img src="Docs/Images/Debug_charactor.jpg" width="300">|
|ステージ設定|<img src="Docs/Images/Debug_stage.jpg" width="300">|
|コントローラ設定|<img src="Docs/Images/Debug_controller.jpg" width="300">|
|シーン設定|<img src="Docs/Images/Debug_scene.jpg" width="300">|
|レース中デバッグ設定|<img src="Docs/Images/Debug_race.jpg" width="300">|
|ショートカット|<img src="Docs/Images/Debug_shortcutlist.jpg" width="300">|

### 工夫した点 <!-- omit in toc -->
- ショートカットリストやプリセット機能を実装することで、作業時間の短縮とヒューマンエラーの削減を実現しました。
