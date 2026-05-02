# チーム開発の進め方

このファイルは、Git にまだ慣れていない人向けの簡単な手順書です。

ゲームジャム中は、難しいことを増やさず、事故を減らすことを優先します。

## まず最初にやること

1. `Git` をインストールする
2. `Git LFS` をインストールする
3. 1回だけ `git lfs install` を実行する
4. このリポジトリを `clone` する
5. Unity Hub で `2022.3.62f3` を使って開く

## 作業を始めるときの流れ

毎回、作業を始める前にこの順番で進めます。

1. `git switch main`
2. `git pull origin main`
3. `git switch -c feature/作業内容`
4. Unity で作業する
5. `git status`
6. `git add .`
7. `git commit -m "feat: 何をしたか"`
8. `git push -u origin feature/作業内容`

## コマンドの意味

- `git switch main`
  最新版を取り込むために、まずメインのブランチへ戻ります。
- `git pull origin main`
  他のメンバーの変更を自分のPCに取り込みます。
- `git switch -c feature/作業内容`
  自分専用の作業ブランチを作ります。
- `git status`
  どのファイルが変わったか確認できます。
- `git add .`
  今回の変更をコミット対象に入れます。
- `git commit -m "feat: 何をしたか"`
  変更内容に名前をつけて保存します。
- `git push -u origin feature/作業内容`
  自分の変更を GitHub に送ります。

## ブランチ名の例

- `feature/player-move`
- `feature/title-ui`
- `feature/enemy-ai`
- `fix/jump-bug`
- `chore/project-setup`

分からなければ、`feature/自分の作業内容` でだいたい大丈夫です。

## コミットメッセージの例

- `feat: プレイヤー移動を追加`
- `feat: タイトル画面を作成`
- `fix: ジャンプ後に着地しないバグを修正`
- `chore: Input設定を更新`

## やってはいけないこと

- `main` ブランチでそのまま作業しない
- いきなり `main` に push しない
- 他の人が触っているシーンを同時に大きく編集しない
- `.meta` ファイルだけを単独で消さない
- Unity のファイルをエクスプローラー上で勝手に移動しない

## Unityで特に気をつけること

- シーン、Prefab、Material、`ProjectSettings` は競合しやすいです
- シーンを編集するときは、誰が触るか先に声をかけるのがおすすめです
- ファイルの移動やリネームは、できるだけ Unity エディタの中で行ってください
- コンフリクトを直したあとは、Unity で開いて壊れていないか確認してください

## どのファイルがGitに入るのか

Git に入れるもの:

- `Assets`
- `Packages`
- `ProjectSettings`

Git に入れないもの:

- `Library`
- `Temp`
- `Logs`
- `UserSettings`

これらは自動生成されるので、共有しなくて大丈夫です。

## 困ったとき

- `pull` したら壊れた
- コンフリクトが出て意味が分からない
- どのブランチで作業すればいいか分からない

こういうときは、無理に進めずチームに相談してください。

特に `scene` や `prefab` のコンフリクトは、触った人どうしで一緒に直す方が安全です。
