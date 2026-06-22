# v1.0.0 - ベクトル場変形 for YMM4

YukkuriMovieMaker4 向けのベクトル場変形エフェクトプラグインの初回リリースです。
制御点ごとに放射場と渦場を生成し、その合成ベクトル場に沿ってピクセルの参照元座標を
中点法による数値積分で変位させる映像エフェクトプラグインです。
制御点は最大 32 個まで追加でき、各制御点のパラメータはすべてアニメーションに対応します。
制御点リストエディターによるグリッド表示・選択・追加・削除と、プレビュー画面上での
ドラッグ操作による直接編集・複数選択・トグル選択に対応します。8 言語対応 UI を備えます。

---

## 新機能

### 1. エフェクト定義（VectorFieldWarpEffect）

`VectorFieldWarpEffect` は `VideoEffectBase` を継承します。

`[VideoEffect]` 属性は以下のパラメーターで宣言されます。

- 表示名：`Texts.VectorFieldWarpEffectName`（ローカライズキー）
- カテゴリー：`VideoEffectCategories.Filtering`
- 検索タグ：`TagVectorField`・`TagVortex`・`TagAttraction`・`TagWarp`（「ベクトル場」・「渦」・「吸引」・「変形」）
- `IsAviUtlSupported = false` により AviUtl 向け EXO 出力は非対応です。
- `ResourceType = typeof(Texts)` でローカライズリソースを指定します。

`Label` プロパティは `$"{Texts.VectorFieldWarpEffectName} - {Points.Count}"` を返却します。
`Points` が変更されるたびに `OnPropertyChanged(nameof(Label))` が発火します。

公開プロパティは以下のとおりです。

- `Amount`（`Animation`、デフォルト `100`、内部範囲：`0`〜`100`）：
  `[AnimationSlider("F1", "%", 0, 100)]` でスライダー表示範囲 0〜100% として表示されます。
  `Texts.VectorFieldWarpEffectName` グループの Order 0 に属します。

- `MaxDisplacement`（`Animation`、デフォルト `200`、内部範囲：`0`〜`MaxDisplacementLimit`）：
  `[AnimationSlider("F1", "px", 0, 500)]` でスライダー表示範囲 0〜500px として表示されます。
  `Texts.VectorFieldWarpEffectName` グループの Order 1 に属します。

- `IntegrationSteps`（`int`、デフォルト `8`、内部範囲：`1`〜`MaxIntegrationSteps`）：
  `[TextBoxSlider("F0", "", 1, MaxIntegrationSteps)]`・`[Range(1, MaxIntegrationSteps)]`・
  `[DefaultValue(8)]` を持つフィールドプロパティです。セッターで `Math.Clamp` を適用します。
  `Texts.VectorFieldWarpEffectName` グループの Order 2 に属します。

- `Points`（`ImmutableList<VectorFieldPoint>`、デフォルト `[VectorFieldPoint.Create(0, 0)]`）：
  `[VectorFieldPointListEditor]` で制御点リストエディターとして表示されます。
  `null` を代入した場合は `ImmutableList<VectorFieldPoint>.Empty` に置き換えられます。
  `Texts.VectorFieldWarpEffectName` グループの Order 10 に属します。

`CreateExoVideoFilters` は空の `IEnumerable<string>` を返却します。

`CreateVideoEffect(IGraphicsDevicesAndContext devices)` は
`new VectorFieldWarpEffectProcessor(devices, this)` を返却します。

`GetAnimatables` は `Amount`・`MaxDisplacement` を yield した後、`Points` の各要素の
`X`・`Y`・`RadialStrength`・`VortexStrength`・`Radius` を順に yield します。

---

### 2. 制御点モデル（VectorFieldPoint）

`VectorFieldPoint` は `Animatable` を継承する `sealed` クラスです。

定数は以下のとおりです。

| 定数 | 値 | 説明 |
|---|---|---|
| `StrengthLimit` | `4096f` | 放射強度・渦強度の内部範囲の上限絶対値 |
| `RadiusLimit` | `4096f` | 影響半径の内部範囲の上限 |

プロパティは以下のとおりです。

- `IsSelected`（`bool`）：`[JsonIgnore]` が付与されており、シリアライズ対象外です。
  選択状態の管理にのみ使用されます。

- `IsEnabled`（`bool`、デフォルト `true`）：
  `[ToggleSlider(PropertyEditorSize = PropertyEditorSize.FullWidth)]` で全幅トグルとして表示されます。

- `X`（`Animation`、デフォルト `0`、内部範囲：`VerySmallValue`〜`VeryLargeValue`）：
  `[AnimationSlider("F1", "px", -500, 500)]` でスライダー表示範囲 -500〜500px です。

- `Y`（`Animation`、デフォルト `0`、内部範囲：`VerySmallValue`〜`VeryLargeValue`）：
  `[AnimationSlider("F1", "px", -500, 500)]` でスライダー表示範囲 -500〜500px です。

- `RadialStrength`（`Animation`、デフォルト `0`、内部範囲：`-StrengthLimit`〜`StrengthLimit`）：
  `[AnimationSlider("F1", "px", -200, 200)]` でスライダー表示範囲 -200〜200px です。

- `VortexStrength`（`Animation`、デフォルト `120`、内部範囲：`-StrengthLimit`〜`StrengthLimit`）：
  `[AnimationSlider("F1", "px", -200, 200)]` でスライダー表示範囲 -200〜200px です。

- `Radius`（`Animation`、デフォルト `200`、内部範囲：`1`〜`RadiusLimit`）：
  `[AnimationSlider("F1", "px", 1, 500)]` でスライダー表示範囲 1〜500px です。

`GetAnimatables` は `[X, Y, RadialStrength, VortexStrength, Radius]` を返却します。

`Create(double x, double y, double radialStrength = 0, double vortexStrength = 120, double radius = 200)`
は静的ファクトリーメソッドであり、`Values[0].Value` へ直接代入して初期値を設定した
`VectorFieldPoint` を返却します。

---

### 3. カスタムシェーダーエフェクト（VectorFieldWarpCustomEffect）

`VectorFieldWarpCustomEffect` は `D2D1CustomShaderEffectBase` を継承します。
コンストラクターは `Create<EffectImpl>(devices)` によって `EffectImpl` を生成します。

定数は以下のとおりです。

| 定数 | 値 | 説明 |
|---|---|---|
| `MaxPoints` | `32` | シェーダーへ渡せる制御点の上限数 |
| `MaxIntegrationSteps` | `16` | 積分回数の上限 |
| `MaxDisplacementLimit` | `2048f` | 最大変位の内部範囲の上限 |

公開プロパティは `GetIntValue`・`GetFloatValue`・`SetValue` を介して `EffectImpl` の
カスタムエフェクトプロパティへ転送します。`PointData` は書き込み専用です。

| プロパティ | 型 | 説明 |
|---|---|---|
| `PointCount` | `int` | 有効な制御点の数 |
| `IntegrationSteps` | `int` | 積分回数 |
| `Amount` | `float` | 変形の適用量（0〜1） |
| `MaxDisplacement` | `float` | 最大変位（px） |
| `PointData` | `byte[]`（書き込み専用） | 制御点データのバイト列 |

#### EffectImpl（内部 sealed クラス）

`[CustomEffect(1)]` 属性により入力画像を 1 枚受け取るカスタムエフェクトとして宣言されます。

定数バッファーのバイトサイズは以下のとおりです。

| 定数 | 値 |
|---|---|
| `HeaderByteSize` | `32` |
| `PointByteSize` | `32` |
| `ConstantBufferByteSize` | `32 + 32 × 32 = 1056` |

`ConstantBuffer` 構造体（`LayoutKind.Sequential`）のレイアウトは以下のとおりです。

| フィールド | 型 | オフセット |
|---|---|---|
| `PointCount` | `int` | 0 |
| `IntegrationSteps` | `int` | 4 |
| `Amount` | `float` | 8 |
| `MaxDisplacement` | `float` | 12 |
| `InputLeft` | `float` | 16 |
| `InputTop` | `float` | 20 |
| `InputWidth` | `float` | 24 |
| `InputHeight` | `float` | 28 |

プロパティセッターの値検証は以下のとおりです。

- `PointCount`：`Math.Clamp(value, 0, MaxPoints)`
- `IntegrationSteps`：`Math.Clamp(value, 1, MaxIntegrationSteps)`
- `Amount`：`float.IsFinite(value)` が false の場合は `0f`、それ以外は `Math.Clamp(value, 0f, 1f)`
- `MaxDisplacement`：`float.IsFinite(value)` が false の場合は `0f`、それ以外は `Math.Clamp(value, 0f, MaxDisplacementLimit)`
- `PointData`：`Array.Clear(pointData)` 後に `Math.Min(value.Length, pointData.Length)` バイトをコピー

コンストラクターでは `constants.IntegrationSteps = 8` を初期値として設定します。

シェーダーリソースは `ShaderResourceUri.Get("VectorFieldWarp")` で参照します。

`UpdateConstants` は `drawInformation` が null の場合は即座に返却します。
`Span<byte> buffer = stackalloc byte[ConstantBufferByteSize]` を確保し、
`MemoryMarshal.Write(buffer, in constants)` でヘッダーを書き込んだ後、
`pointData.CopyTo(buffer[HeaderByteSize..])` で制御点データを後続バイトに書き込み、
`drawInformation.SetPixelShaderConstantBuffer(buffer)` を呼び出します。

`MapInputRectsToOutputRect` は `inputRects[0]` を `ClampInputRect` で丸め、
`InputLeft`・`InputTop`・`InputWidth`・`InputHeight` を更新した後、
`outputRect = Inflate(inputRect, GetMargin())`・`outputOpaqueSubRect = default` を設定します。

`MapOutputRectToInputRects` は `inputRects[0] = ClampInputRect(Inflate(outputRect, GetMargin() + 1))` を設定します。
出力矩形から入力矩形を逆算する際にマージンへ 1 を加算します。

`GetMargin` は `PointCount <= 0` または `Amount <= 0f` または `MaxDisplacement <= 0f` の場合に
`0` を返却します。それ以外は `(int)Math.Ceiling(Amount * MaxDisplacement)` を返却します。

`Inflate` は `margin <= 0` の場合は矩形をそのまま返却します。それ以外は
`Left - margin`・`Top - margin`・`Right + margin`・`Bottom + margin` を `Saturate` でクランプして返却します。

`Saturate` は `long` 値を `int.MinValue`〜`int.MaxValue` にクランプして `int` へキャストします。

`Properties` 列挙体のメンバーは `PointCount`・`IntegrationSteps`・`Amount`・`MaxDisplacement`・`PointData` です。

---

### 4. エフェクトプロセッサー（VectorFieldWarpEffectProcessor）

`VectorFieldWarpEffectProcessor` は `VideoEffectProcessorBase` を継承します。
コンストラクターは `IGraphicsDevicesAndContext devices` と `VectorFieldWarpEffect item` を受け取ります。

定数は以下のとおりです。

| 定数 | 値 | 説明 |
|---|---|---|
| `FloatsPerPoint` | `8` | 制御点 1 個あたりのフロート数 |
| `PositionLimit` | `65536f` | X・Y 座標の内部クランプ上限絶対値 |

フィールドは以下のとおりです。

- `item`：`VectorFieldWarpEffect`
- `pointData`：`float[MaxPoints × FloatsPerPoint]`（= `float[256]`）、シェーダーへ渡す制御点データのキャッシュ
- `effect`：`VectorFieldWarpCustomEffect?`
- `isFirst`：`bool = true`、初回フレームフラグ
- `pointCount`・`integrationSteps`：`int`、前フレームの値のキャッシュ
- `amount`・`maxDisplacement`：`float`、前フレームの値のキャッシュ

#### Update メソッド

`IsPassThroughEffect || effect is null` の場合は `effectDescription.DrawDescription` をそのまま返却します。

`Amount` は `GetValue(...) / 100d` を `Sanitize` で `0f`〜`1f` にクランプします。
`MaxDisplacement` は `GetValue(...)` を `Sanitize` で `0f`〜`MaxDisplacementLimit` にクランプします。
`IntegrationSteps` は `Math.Clamp(item.IntegrationSteps, 1, MaxIntegrationSteps)` で確定します。

ループは `Math.Min(points.Count, MaxPoints)` 回まで実行されます。各点の値は
`Sanitize` で以下の範囲にクランプされます。

| パラメーター | クランプ範囲 | フォールバック |
|---|---|---|
| `x` | `-PositionLimit`〜`PositionLimit` | `0f` |
| `y` | `-PositionLimit`〜`PositionLimit` | `0f` |
| `radialStrength` | `-StrengthLimit`〜`StrengthLimit` | `0f` |
| `vortexStrength` | `-StrengthLimit`〜`StrengthLimit` | `0f` |
| `radius` | `1f`〜`RadiusLimit` | `1f` |

`point.IsEnabled` が false、または `radialStrength == 0f && vortexStrength == 0f` の場合、
その点はシェーダーデータから除外されます（`pointCount` を加算しません）。

有効な点の `pointData` への書き込み順序は以下のとおりです（オフセットは `pointCount × 8`）。

| オフセット | 値 |
|---|---|
| +0 | `x` |
| +1 | `y` |
| +2 | `radialStrength` |
| +3 | `vortexStrength` |
| +4 | `radius` |
| +5 | `0f`（パディング） |
| +6 | `0f`（パディング） |
| +7 | `0f`（パディング） |

有効・無効を問わず全制御点に対して `CreateController` を呼び出してコントローラーを生成します。

シェーダーへの反映は差分更新方式です。`isFirst` が true、または前フレームから値が変化した場合のみ
`effect` の各プロパティを更新します。`PointData` は `isFirst || pointDataChanged` の場合のみ
`byte[]` に変換して転送します（`Buffer.BlockCopy` で `float[]` → `byte[]`）。

戻り値は `description with { Controllers = [.. description.Controllers, .. controllers] }` です。

#### CreateController メソッド

`ControllerPoint` を `Vector3(x, y, 0f)` で生成します。

- `Shape`：`VideoControllerPointShape.Circle`
- `IsSelected`：`point.IsSelected`
- ドラッグコールバック：未選択の場合は `SelectExclusively` を呼び出した後、選択中の全点の
  `X` と `Y` に `args.Delta.X` と `args.Delta.Y` を加算します。
- `OnDragStart`：Ctrl キーが押されている場合は `ToggleSelection`、押されていない場合は
  未選択の点のみ `SelectExclusively` します。

`SelectExclusively` は `item.Points` の全点を走査し、参照等値比較で `IsSelected` を設定します。

`ToggleSelection` は対象が未選択の場合は選択します。選択中の場合は他に選択中の点が
1 つ以上存在するときのみ選択を解除します（最終選択点は解除できません）。

#### SetPointData メソッド

`pointData[index]` の現在値と `value` が等しい場合は何もしません。
異なる場合は `pointData[index] = value` を書き込み、`changed = true` にします。

#### Sanitize メソッド（静的）

`double.IsFinite(value)` が false の場合は `fallback` を返却します。
それ以外は `(float)Math.Clamp(value, minimum, maximum)` を返却します。

#### CreateEffect / setInput / ClearEffectChain

`CreateEffect` は `VectorFieldWarpCustomEffect` を生成し、`IsEnabled` が false の場合は
破棄して `null` を返却します。有効な場合は `disposer` に収集し、`output` を取得して返却します。

`setInput` は `effect?.SetInput(0, input, true)` を呼び出します。

`ClearEffectChain` は `effect?.SetInput(0, null, true)` を呼び出し、`isFirst = true` に戻します。

---

### 5. 制御点アイテム ViewModel（VectorFieldPointItemViewModel）

`VectorFieldPointItemViewModel` は `Bindable` を継承し、`IDisposable` を実装します。

プロパティは以下のとおりです。

- `Model`：`VectorFieldPoint`（コンストラクターで受け取る）
- `Label`：`string`（`$"#{index + 1}"` 形式、コンストラクターで確定する読み取り専用）
- `IsEnabled`：`Model.IsEnabled` へ委譲
- `IsSelected`：`Model.IsSelected` へ委譲
- `SelectCommand`：`ICommand`（コンストラクターで受け取る）

イベントは以下のとおりです。

- `PositionChanged`：X または Y の座標が変化したときに発火します。

コンストラクターでは `SubscribeValues` を呼び出した後、
`Model.X.PropertyChanged`・`Model.Y.PropertyChanged`・`Model.PropertyChanged` を購読します。

`SubscribeValues` / `UnsubscribeValues` は `Model.X.Values` と `Model.Y.Values` の各要素の
`PropertyChanged` を購読・解除します。

`Animation_PropertyChanged` は `PropertyName` が `nameof(Animation.Values)` または
`nameof(Animation.AnimationType)` の場合のみ動作します。値コレクションの再購読を行い、
`PositionChanged` を発火します。

`Position_PropertyChanged` は `PositionChanged` を発火します。

`Model_PropertyChanged` は `IsEnabled` と `IsSelected` の変化を `OnPropertyChanged` へ転送します。

`Dispose` はすべての購読を解除し、`GC.SuppressFinalize` を呼び出します。
二重破棄は `disposedValue` フラグで防止します。

---

### 6. 制御点リストエディター ViewModel（VectorFieldPointListEditorViewModel）

`VectorFieldPointListEditorViewModel` は `Bindable` を継承し、`IDisposable` を実装します。

`Effect` プロパティは `(VectorFieldWarpEffect)ItemProperties[0].PropertyOwner` を返却します。

`SetEditorInfo` は空実装です。

公開プロパティは以下のとおりです。

| プロパティ | 型 | 説明 |
|---|---|---|
| `Columns` | `int` | グリッドの列数 |
| `Rows` | `int` | グリッドの行数 |
| `VerticalLines` | `object[]` | 列区切り線の数に対応するダミー配列 |
| `HorizontalLines` | `object[]` | 行区切り線の数に対応するダミー配列 |
| `Items` | `ImmutableList<VectorFieldPointItemViewModel?>` | グリッドセルに配置されたアイテム（null はセルの空きを表す） |
| `SelectedTarget` | `object?` | 選択中の制御点の `Model`（PropertiesEditor のターゲット） |
| `CanAddPoint` | `bool` | `Effect.Points.Count < MaxPoints` |

コマンドは以下のとおりです。

| コマンド | 実行条件 | 説明 |
|---|---|---|
| `AddPointCommand` | `CanAddPoint` | 制御点を追加します |
| `RemovePointCommand` | `selectedItem != null` | 選択中の制御点を削除します |
| `OnBeginEditPointCommand` | 常に実行可能 | `BeginEdit` を発火します |
| `OnEndEditPointCommand` | 常に実行可能 | `EndEdit` を発火します |

イベントは `BeginEdit`・`EndEdit` です。

#### AddPoint メソッド

`index = Effect.Points.Count` を取得し、追加位置の座標を以下の式で計算します。

```
x = (index % 5 - 2) * 100.0
y = (index / 5) * 100.0
```

`VectorFieldPoint.Create(x, y)` を生成して `points` に追加し、`BeginEdit`・`EndEdit` で
`CommitStructuralChange(points, points.Count - 1)` を挟みます（新点を選択状態にします）。

#### RemovePoint メソッド

選択中のアイテムの `allViewModels` 内インデックスを取得し、モデルを `points` から削除します。
`selectedIndex` は `points.Count == 0 ? -1 : Math.Min(index, points.Count - 1)` で決定します。
`BeginEdit`・`EndEdit` で `CommitStructuralChange(points, selectedIndex)` を挟みます。

#### CommitStructuralChange メソッド

`points` の全要素を `Clone` で JSON シリアライズ・デシリアライズしてディープコピーします。
コピー後の各要素に `IsSelected = (index == selectedIndex)` を設定し、
`ItemProperties[0].SetValue(clones)` で確定します。

`Clone` は `JsonConvert.DeserializeObject<VectorFieldPoint>(JsonConvert.SerializeObject(point))`
を呼び出し、失敗した場合は `VectorFieldPoint.Create(0, 0)` を返却します。

#### HandleSelect メソッド

`isMutatingSelection = true` に設定した後、`allViewModels` の全要素の `IsSelected` を
`ReferenceEquals(item, vm)` で設定します。finally ブロックで `isMutatingSelection = false` に
戻してから `UpdateSelection()` を呼び出します。

#### RebuildViewModels メソッド

`allViewModels.ToDictionary(x => x.Model)` で既存 ViewModel を Model をキーとして引きます。
新しいリストを構築する際、既存の ViewModel が再利用できる場合はそのまま使用し、
新規点に対してのみ `new VectorFieldPointItemViewModel(point, index, selectCommand)` を生成します。

削除された ViewModel に対して `PropertyChanged`・`PositionChanged` の購読を解除してから `Dispose` します。
追加された ViewModel に対して `PropertyChanged`・`PositionChanged` を購読します。

`RefreshGridLayout`・`EnsureSelectionAfterRebuild`・`UpdateSelection` を順に呼び出します。

#### RefreshGridLayout メソッド

`ComputeGridLayout` を呼び出し、`Columns`・`Rows` を更新します。
`VerticalLines`・`HorizontalLines` の長さが変化した場合のみ新しい `object[]` を生成します。
`Items` を `ImmutableList.CreateRange(layout.Cells)` で更新します。

#### ComputeGridLayout メソッド（静的）

`viewModels.Count == 0` の場合は `GridLayout(1, 1, new VectorFieldPointItemViewModel?[1])` を返却します。

各 ViewModel の `Model.X.Values.FirstOrDefault()?.Value ?? 0.0` と
`Model.Y.Values.FirstOrDefault()?.Value ?? 0.0` を配列として取得します。

バウンディングボックスの幅・高さから以下の式でクラスタリング許容誤差を計算します。

```
tolerance = Math.Max(Math.Max(bboxW, bboxH) * 0.1, 1e-3)
```

`ClusterCoordinates` で X 座標群と Y 座標群をそれぞれクラスタリングし、
列番号・行番号の割り当てを取得します。

セル配列 `VectorFieldPointItemViewModel?[rowCount × colCount]` を確保し、衝突のない点を配置します。
衝突した点は `pending` リストに積み、`FindNearestEmptyCell` で最近傍の空きセルを探索します。
空きセルが見つからない場合は行数を 1 増やした新しい配列に `Array.Copy` で移行した後、再探索します。

`GridLayout` は `readonly struct` で `Columns`・`Rows`・`Cells` を保持します。

#### FindNearestEmptyCell メソッド（静的）

`radius = 1` から `rowCount + colCount` まで拡大しながら、チェビシェフ距離 = `radius` の
外周リングを走査します（`Math.Abs(dr) != radius && Math.Abs(dc) != radius` の場合はスキップ）。
空きセルが見つかった場合はそのスロットインデックスを返却します。見つからない場合は `-1` を返却します。

#### ClusterCoordinates メソッド（静的）

値を昇順ソートし、隣接する値の差が `tolerance` を超える箇所でクラスター番号を 1 増やします。
ソート前のインデックスへのクラスター番号の割り当て配列と、クラスター総数を返却します。

#### UpdateSelection メソッド

`isMutatingSelection` または `disposedValue` が true の場合は即座に返却します。
`allViewModels.FirstOrDefault(x => x.IsSelected)` で `selectedItem` を更新し、
`SelectedTarget = selectedItem?.Model` を設定します。

#### EnsureSelectionAfterRebuild メソッド

選択中の ViewModel が存在しない場合かつ `allViewModels.Count > 0` の場合、
`isMutatingSelection = true` に設定してから `allViewModels[0].IsSelected = true` を実行します。

---

### 7. 制御点リストエディタービュー（VectorFieldPointListEditor / VectorFieldPointListEditorAttribute）

#### VectorFieldPointListEditor

`UserControl` を継承し、`IPropertyEditorControl2`・`IPropertyEditorControl` を実装します。

- `ItemProperties`：`ItemProperty[]?`（内部セッター）
- `BeginEdit`・`EndEdit`：イベント

`DataContextChanged` で旧 ViewModel の `BeginEdit`・`EndEdit` 購読を解除して `Dispose` し、
新 ViewModel の `BeginEdit`・`EndEdit` を購読します。

`SetEditorInfo` は `DataContext` が `VectorFieldPointListEditorViewModel` の場合に
`viewModel.SetEditorInfo(info)` を委譲します（ViewModel 側は空実装）。

#### XAML レイアウト

3 行構成の `Grid` です。

- Row 0（Auto）：制御点グリッド表示エリア
  - `DropShadowEffect`（Color=SystemColors.WindowText、Opacity=1、BlurRadius=1、ShadowDepth=0）
  - 縦区切り線用 `ItemsControl`（`UniformGrid` Columns バインド）：幅 1px のグリッドに
    StrokeDashArray=4,4 の破線矩形を配置します。
  - 横区切り線用 `ItemsControl`（`UniformGrid` Columns=1）：高さ 1px のグリッドに
    StrokeDashArray=4,4 の破線矩形を配置します。
  - アイテム用 `ItemsControl`（`UniformGrid` Columns バインド）：
    `VectorFieldPointItemViewModel` 向けの `DataTemplate` で `Viewbox`（StretchDirection.DownOnly）
    内に 48×48 の `Grid` を配置し、16×16 の `ReadOnlyToggleButton` を中央に表示します。
    - CornerRadius は `ActualWidth` にバインドして円形にします。
    - `IsEnabled = false` のとき `Opacity = 0.3`、`IsSelected = true` のとき `BorderThickness = 2` にします。
    - `Command={Binding SelectCommand}`・`CommandParameter={Binding}`・`ToolTip={Binding Label}` を設定します。

- Row 1（高さ 26px）：追加・削除ボタン行
  - Column 0（`*`）：追加ボタン（`Texts.VectorFieldWarpAddPoint`、`AddPointCommand`）
  - Column 1（26px）：削除ボタン（マイナスアイコン、`Texts.VectorFieldWarpRemovePoint` ツールチップ、`RemovePointCommand`）

- Row 2（`*`）：`PropertiesEditor`（Margin=0,-22,0,0 でヘッダー部を上方にクリップして非表示）
  - `Target={Binding SelectedTarget}` にバインドして選択中の制御点のプロパティを表示します。
  - `BeginEdit`・`EndEdit` イベントを `OnBeginEditPointCommand`・`OnEndEditPointCommand` へ転送します。

#### VectorFieldPointListEditorAttribute

`PropertyEditorAttribute2` を継承する `sealed` クラスです。

コンストラクターで `PropertyEditorSize = PropertyEditorSize.FullWidth` を設定します。

`Create()` は `new VectorFieldPointListEditor()` を返却します。

`SetBindings` は `editor.ItemProperties = itemProperties` を設定した後、
`editor.DataContext = new VectorFieldPointListEditorViewModel(itemProperties)` を設定します。

`ClearBindings` は `editor.ItemProperties = null`・`editor.DataContext = null` を設定します。
ViewModel の破棄は `DataContextChanged` ハンドラーが担います。

---

### 8. ローカライズ（Texts）

`Texts` クラスは `[AutoGenLocalizer]` 属性を持つ `partial` クラスとして宣言されます。
`YukkuriMovieMaker.Generator` のソースジェネレーターが `Texts.csv` を処理し、
各ロケールのリソースファイルを自動生成します。

対応言語：日本語（`ja-jp`）・英語（`en-us`）・中国語簡体字（`zh-cn`）・中国語繁体字（`zh-tw`）・
韓国語（`ko-kr`）・スペイン語（`es-es`）・アラビア語（`ar-sa`）・インドネシア語（`id-id`）

ローカライズキーの一覧は以下のとおりです。

| キー | ja-jp |
|---|---|
| `VectorFieldWarpEffectName` | ベクトル場変形 |
| `VectorFieldWarpAmountName` | 適用量 |
| `VectorFieldWarpAmountDesc` | 変形の適用量を調整します。0%では入力画像を変更しません。 |
| `VectorFieldWarpMaxDisplacementName` | 最大変位 |
| `VectorFieldWarpMaxDisplacementDesc` | 1画素が移動できる距離の上限を指定します。 |
| `VectorFieldWarpIntegrationStepsName` | 積分回数 |
| `VectorFieldWarpIntegrationStepsDesc` | 流線を求める中点法の反復回数を指定します。 |
| `VectorFieldWarpPointsDesc` | 放射場と渦場を生成する制御点を追加または削除します。 |
| `VectorFieldWarpAddPoint` | 制御点を追加 |
| `VectorFieldWarpRemovePoint` | 選択中の制御点を削除 |
| `VectorFieldPointGroupName` | 制御点 |
| `VectorFieldPointEnabledName` | 有効 |
| `VectorFieldPointEnabledDesc` | この制御点が生成するベクトル場を有効にします。 |
| `VectorFieldPointXName` | X座標 |
| `VectorFieldPointXDesc` | 制御点のX座標を指定します。 |
| `VectorFieldPointYName` | Y座標 |
| `VectorFieldPointYDesc` | 制御点のY座標を指定します。 |
| `VectorFieldPointRadialStrengthName` | 放射強度 |
| `VectorFieldPointRadialStrengthDesc` | 正の値で外向きに広げ 負の値で中心へ引き寄せます。 |
| `VectorFieldPointVortexStrengthName` | 渦強度 |
| `VectorFieldPointVortexStrengthDesc` | 正負の符号で回転方向を切り替えて渦の強さを指定します。 |
| `VectorFieldPointRadiusName` | 影響半径 |
| `VectorFieldPointRadiusDesc` | 制御点付近の場が最も強くなる距離尺度を指定します。 |
| `TagVectorField` | ベクトル場 |
| `TagVortex` | 渦 |
| `TagAttraction` | 吸引 |
| `TagWarp` | 変形 |