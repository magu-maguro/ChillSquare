# MaguroPanel

Unity UI の `Button` と `Panel`
を使って、階層型メニューを構築するためのシンプルなメニューシステムです。キーボード操作による選択・決定・戻る操作を
`MenuManager` が管理し、`MenuPanel` 同士を `MenuChildLink`
で接続してメニューの階層を作ります。

## 主な機能

-   RootPanel から子 Panel へ移動できる階層型メニュー
-   Back 操作で親 Panel へ戻り、RootPanel ではメニューを閉じる
-   Panel ごとに縦移動・横移動を設定可能
-   初期選択ボタンの指定
-   `Button.onClick` を利用した通常のボタンイベント
-   メニュー・Panel・選択ボタンの状態変化を通知する UnityEvent
-   Unity Input System の `InputActionAsset` を利用した入力設定

## 構成要素

### MenuManager

メニュー全体の状態と遷移を管理します。シーン内に配置し、RootPanel
と入力アセットを Inspector から設定します。

主な役割：

-   RootPanel の表示・メニューの開始と終了
-   現在の Panel の管理
-   子 Panel への遷移と親 Panel への復帰
-   キーボードによるボタン選択、決定、戻る操作
-   EventSystem の選択対象と、メニュー側で管理する選択状態の同期

`CurrentPanel` は現在表示中の Panel、`IsOpen`
はメニューが開いているかを表します。

### MenuPanel

メニュー内の各 Panel に付けるコンポーネントです。Panel 内の Button
を収集し、子 Panel へのリンクと初期選択ボタンを管理します。

Inspector の設定：

-   **Child Links**：この Panel から開く子 Panel と、その遷移に使う
    Button の組み合わせ
-   **Initial Selected Button**：Panel 表示時に最初に選択する
    Button。未設定または使用できない場合は、Panel 内で最初に使用可能な
    Button が選ばれます
-   **Navigation Direction**：ボタンの選択移動方向
    -   `Vertical`：上・下キーで移動
    -   `Horizontal`：左・右キーで移動

`MenuPanel` は Panel 内の Button の `onClick`
に共通処理を登録します。`MenuChildLink` に登録された Button なら子 Panel
へ遷移し、それ以外の Button は通常の `Button.onClick`
イベントを実行します。

### MenuChildLink

Button と遷移先の子 Panel
の組み合わせを表すシリアライズ可能なクラスです。`MenuPanel` の
`Child Links` リストから設定します。

-   **Button**：子 Panel を開くための Button
-   **Child Panel**：遷移先の `MenuPanel`

## セットアップ

### 1. UI を作成する

1.  Canvas の下に RootPanel を作成し、`MenuPanel` を追加します。
2.  RootPanel 内にメニュー用の Button を配置します。
3.  必要に応じて子 Panel を作成し、それぞれに `MenuPanel` を追加します。
4.  `MenuChildLink` を使って、親 Panel の Button と子 Panel
    を接続します。
5.  子 Panel の Button も同様に設定し、必要な階層を構築します。

**推奨：** 各 Panel は、ほかの Panel
の子ではなく、共通の親オブジェクトの下に兄弟として配置してください。`MenuPanel`
は `GetComponentsInChildren<Button>(true)` で子階層の Button
を収集するため、Panel 同士を入れ子にすると、親 Panel が子 Panel の
Button まで収集してしまう可能性があります。

### 2. MenuManager を設定する

シーン内の GameObject に `MenuManager` を追加し、次を設定します。

-   **Root Panel**：最初に開く `MenuPanel`
-   **Input Actions**：メニュー操作用の `.inputactions` アセット
-   **Action Map Name**：使用する Action Map 名。既定値は `Menu`
-   **Navigate Action Name**：移動用 Action 名。既定値は `Navigate`
-   **Submit Action Name**：決定用 Action 名。既定値は `Submit`
-   **Back Action Name**：戻る用 Action 名。既定値は `Back`

### 3. Input System を設定する

メニュー用の Action Map（既定名 `Menu`）に、次の Action を作成します。

  -----------------------------------------------------------------------
  Action                  推奨設定                バインド例
  ----------------------- ----------------------- -----------------------
  `Navigate`              Value / Vector2         方向キー、WASD などの
                                                  2D Vector Composite

  `Submit`                Button                  Enter、テンキー Enter

  `Back`                  Button                  Backspace、Escape
  -----------------------------------------------------------------------

`Navigate` は方向入力を `Vector2` として読み取ります。縦移動の Panel は
Y 成分、横移動の Panel は X 成分を使います。

### 4. EventSystem を設定する

シーンに EventSystem を用意し、マウスによる Button 操作を使う場合は
`InputSystemUIInputModule` を設定します。

**重要：** `MenuManager` が独自に `Submit` Action を処理して
`Button.onClick.Invoke()` を呼ぶため、`InputSystemUIInputModule` 側の
**Submit アクションは None に設定してください**。両方が Submit
を処理すると、1 回の Enter 入力で Button
のクリック処理が重複し、複数階層を進んでしまうことがあります。

`Navigate` も `MenuManager`
が独自に処理するため、メニュー内のキーボード移動は `MenuManager`
が担当します。マウス操作を残す場合、`InputSystemUIInputModule`
自体は無効化せずに使用できます。

また、メニュー内 Button の **Navigation** は `None`
に設定することを推奨します。選択移動は `MenuManager` が行います。

## メニューの操作

-   **OpenRoot()**：RootPanel
    を開き、メニュー操作を有効にします。通常は外部 UI の Button
    などから呼び出します。
-   **Navigate**：現在の Panel の `Navigation Direction`
    に応じて、使用可能な Button
    の選択を移動します。リストの端では反対側へ循環します。
-   **Submit**：選択中の Button の `onClick` を実行します。Child Link
    が設定されている場合は、子 Panel への遷移も行います。
-   **Back**：子 Panel では親 Panel に戻り、RootPanel
    ではメニューを閉じます。

子 Panel への遷移は `MenuChildLink` を通して行います。子 Panel を開く
Button の Inspector の `On Click()`
に、同じ遷移処理を重複して設定しないでください。

## UnityEvent

### MenuManager のイベント

-   **On Menu Opened**：メニューが閉じた状態から開いたときに呼ばれます。
-   **On Menu Closed**：RootPanel で Back
    操作を行い、メニューを閉じたときに呼ばれます。
-   **On Child Panel Opened**：子 Panel への遷移後に呼ばれ、遷移先の
    `MenuPanel` が渡されます。
-   **On Parent Panel Opened**：親 Panel
    への復帰後に呼ばれ、表示された親 `MenuPanel` が渡されます。

これらのイベントを利用して、ゲーム側の入力切り替え、カーソル表示、SE
再生などを連携できます。

### MenuPanel のイベント

-   **On Opened**：Panel が表示されたときに呼ばれます。
-   **On Closed**：Panel が非表示になるときに呼ばれます。
-   **On Selection Changed**：選択中の Button
    が変わったときに呼ばれ、選択された `Button` が渡されます。

## 注意事項・制約

-   `MenuManager` は `InputActionAsset` 内の指定された Action Map
    を有効化・無効化します。ほかのシステムと同じ Action Map
    を共有すると影響する場合があるため、メニュー専用の Action Map
    を用意してください。
-   `MenuManager` の `OnDisable()` ではメニュー用 Action Map
    を無効化します。
-   `MenuPanel` は `Awake()` で Button
    を収集し、クリックリスナーを登録します。初期化後に Button
    を動的に追加・削除する用途には、そのままでは対応していません。
-   Panel 内の Button は、`MenuPanel` の GameObject
    の子階層から取得されます。Panel 同士を入れ子にすると Button
    の収集範囲が重なる可能性があります。
-   選択可能な Button は、アクティブかつ `interactable`
    が有効なものです。
-   `OpenRoot()` は、メニューがすでに開いている場合にも RootPanel
    へ戻す用途で呼び出せます。
-   スクリプトには調査用の `Debug.Log`
    が含まれているため、配布前に必要に応じて削除してください。

## 想定する利用例

-   ゲームのタイトルメニュー
-   設定画面と、その配下にあるサブメニュー
-   一時停止メニュー
-   キーボード操作を中心とした階層型 UI

## 名前空間

すべてのクラスは `MaguroPanel` 名前空間に属します。

``` csharp
using MaguroPanel;
```
