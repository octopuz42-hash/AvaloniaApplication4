# UML-диаграммы проекта AvaloniaApplication4

Документ описывает фактическую структуру приложения «Paint — Классический», его запуск, основные зависимости и сценарии взаимодействия. Диаграммы написаны на Mermaid.

## 1. Роли файлов

| Файл | Назначение | Основные зависимости |
|---|---|---|
| `AvaloniaApplication4/Program.cs` | Точка входа, сборка и запуск Avalonia-приложения | `App`, Avalonia Desktop |
| `AvaloniaApplication4/App.axaml.cs` | Инициализация XAML и назначение главного окна | `MainWindow`, `MainViewModel`, Avalonia Application Lifetimes |
| `AvaloniaApplication4/App.axaml` | Тема, стили приложения и регистрация `ViewLocator` как шаблона данных | `ViewLocator`, Fluent Theme, ColorPicker styles |
| `AvaloniaApplication4/Views/MainWindow.axaml` | Интерфейс: меню, панель инструментов, настройки холста, фильтры, цвет и холст | `MainViewModel`, `PaintCanvas`, `DrawingTool`, `ImageFilterType`, `EnumToIntConverter` |
| `AvaloniaApplication4/Views/MainWindow.axaml.cs` | Обработчики масштабирования и сохранения PNG; конвертер перечисления для списка инструментов | `MainViewModel`, Avalonia Storage Provider, `EnumToIntConverter` |
| `AvaloniaApplication4/ViewModels/MainViewModel.cs` | Состояние холста, bitmap, команды создания/отмены/повтора, фильтры и экспорт PNG | CommunityToolkit.Mvvm, SkiaSharp, `DrawingTool`, `ImageFilterType` |
| `AvaloniaApplication4/Controls/PaintCanvas.cs` | Принимает события указателя, меняет bitmap и рисует его в интерфейсе | `MainViewModel`, `DrawingTool`, SkiaSharp, Avalonia |
| `AvaloniaApplication4/Models/DrawingTool.cs` | Перечисления `DrawingTool` и `ImageFilterType` | Используются View, ViewModel и Canvas |
| `AvaloniaApplication4/ViewLocator.cs` | По имени ViewModel пытается найти и создать соответствующий View | Avalonia Data Templates, `ViewModelBase` |
| `AvaloniaApplication4/ViewModels/ViewModelBase.cs` | Базовый тип ObservableObject для ViewModel | CommunityToolkit.Mvvm |
| `AvaloniaApplication4/AvaloniaApplication4.csproj` | Конфигурация приложения, целевая платформа и NuGet-пакеты | .NET 10, Avalonia 12, CommunityToolkit.Mvvm, SkiaSharp |
| `AvaloniaApplication4/Assets/avalonia-logo.ico` | Иконка главного окна | Подключается как Avalonia Resource и указывается в `MainWindow.axaml` |
| `AvaloniaApplication4/app.manifest` | Манифест приложения Windows | Подключается из `.csproj` |

## 2. Запуск приложения и связи компонентов

```mermaid
flowchart TD
	Entry[Program.Main] --> Builder[BuildAvaloniaApp: конфигурация Avalonia]
	Builder --> App[App]
	App --> AppXaml[App.axaml: тема, стили, DataTemplates]
	AppXaml --> Locator[ViewLocator]
	App --> Lifetime[Classic desktop lifetime]
	Lifetime --> Window[MainWindow]
	Lifetime -. задаёт DataContext .-> VM1[MainViewModel экземпляр из App]
	Window --> WindowXaml[MainWindow.axaml: интерфейс и bindings]
	Window --> WindowCode[MainWindow.axaml.cs: события окна]
	Window -. конструктор повторно задаёт DataContext .-> VM2[Другой MainViewModel экземпляр]
	WindowXaml --> VM2
	WindowXaml --> Canvas[PaintCanvas]
	Canvas --> VM2
	VM2 --> Bitmap[SKBitmap: изображение и история Undo/Redo]
	VM2 --> Enums[DrawingTool и ImageFilterType]
	WindowCode --> Storage[Диалог выбора файла Avalonia]
	Storage --> VM2
	VM2 --> Skia[SkiaSharp: рисование, фильтры и PNG]
	Project[csproj: .NET 10 и NuGet-пакеты] -. сборка .-> Entry
```

Пунктирная связь от `App` к первому `MainViewModel` отражает создание `DataContext` в `App.axaml.cs`. Затем конструктор `MainWindow` присваивает окну новый `MainViewModel`, поэтому фактически bindings интерфейса и `PaintCanvas` используют второй экземпляр.

## 3. Диаграмма классов и моделей

```mermaid
classDiagram
	class Program {
		+Main(args)
		+BuildAvaloniaApp()
	}
	class App {
		+Initialize()
		+OnFrameworkInitializationCompleted()
	}
	class MainWindow {
		+MainWindow()
		-CanvasScrollViewer_PointerWheelChanged(sender, e)
		-SaveButton_Click(sender, e)
	}
	class EnumToIntConverter {
		+Convert(value, targetType, parameter, culture)
		+ConvertBack(value, targetType, parameter, culture)
	}
	class MainViewModel {
		+CanvasWidth int
		+CanvasHeight int
		+Bitmap SKBitmap
		+ZoomScale double
		+CurrentTool DrawingTool
		+SelectedColor Color
		+BrushSize int
		+CreateNewCanvas()
		+SaveStateForUndo()
		+Undo()
		+Redo()
		+ApplyFilter(filterType)
		+SaveToFile(stream)
	}
	class ObservableObject {
		<<CommunityToolkit.Mvvm>>
	}
	class PaintCanvas {
		+Bitmap SKBitmap
		+OnPointerPressed(e)
		+OnPointerMoved(e)
		+OnPointerReleased(e)
		+Render(context)
		-CreatePaint(vm)
	}
	class DrawingTool {
		<<enumeration>>
		Pencil
		BucketFill
		Eraser
	}
	class ImageFilterType {
		<<enumeration>>
		None
		Invert
		Grayscale
		Sepia
		Blur
	}
	class ViewLocator {
		+Build(param)
		+Match(data)
	}
	class ViewModelBase {
		<<abstract>>
	}
	class MainView {
		<<ожидаемое имя View по соглашению ViewLocator>>
	}
	class SKBitmap {
		<<SkiaSharp>>
	}
	class StorageProvider {
		<<Avalonia>>
	}

	Program ..> App : запускает
	App ..> MainWindow : создаёт
	App ..> MainViewModel : назначает начальный DataContext
	MainWindow ..> MainViewModel : конструктор задаёт новый DataContext
	MainViewModel --|> ObservableObject : наследование
	MainWindow --> EnumToIntConverter : использует
	MainWindow --> PaintCanvas : содержит в XAML
	MainWindow ..> StorageProvider : сохраняет файл через
	MainWindow ..> MainViewModel : вызывает SaveToFile
	PaintCanvas --> MainViewModel : читает состояние и сохраняет Undo
	PaintCanvas --> SKBitmap : меняет и отображает
	MainViewModel --> SKBitmap : владеет изображением и историей
	MainViewModel --> DrawingTool : выбранный инструмент
	MainViewModel --> ImageFilterType : параметры фильтра
	ViewLocator ..> ViewModelBase : Match проверяет этот тип
	ViewLocator ..> MainView : ищет тип по имени
```

## 4. Сценарий рисования и сохранения

```mermaid
sequenceDiagram
	actor User as Пользователь
	participant View as MainWindow.axaml
	participant Code as MainWindow.axaml.cs
	participant Canvas as PaintCanvas
	participant VM as MainViewModel
	participant Skia as SkiaSharp / SKBitmap
	participant Picker as Avalonia StorageProvider

	User->>View: меняет инструмент, цвет или размер кисти
	View->>VM: Two-way bindings обновляют свойства
	User->>Canvas: нажимает/перемещает указатель
	Canvas->>VM: SaveStateForUndo()
	VM->>Skia: копирует bitmap в историю Undo
	Canvas->>Skia: рисует точку/линию или заливает холст
	Canvas-->>View: InvalidateVisual и Render(bitmap)
	User->>View: нажимает «Сохранить как...»
	View->>Code: SaveButton_Click
	Code->>Picker: SaveFilePickerAsync(PNG)
	Picker-->>Code: выбранный файл
	Code->>VM: SaveToFile(stream)
	VM->>Skia: кодирует bitmap в PNG
	Skia-->>Picker: записывает PNG в поток файла
```

## 5. Важные особенности текущего кода

- `MainViewModel` наследуется от `ObservableObject`, а не от `ViewModelBase`, поэтому `ViewLocator.Match` для него возвращает `false`. Если вызвать `Build` напрямую с `MainViewModel`, locator по соглашению будет искать тип `AvaloniaApplication4.Views.MainView`; такого класса в проекте нет, и результатом будет запасной `TextBlock` с сообщением `Not Found`. В основном запуске окно создаётся напрямую, поэтому этот locator не создаёт главный экран.
- `App.axaml.cs` создаёт один `MainViewModel`, а `MainWindow` в конструкторе заменяет его новым экземпляром. Сейчас первый экземпляр не используется после создания окна.
- Заливка `BucketFill` в `PaintCanvas` реализована как заливка всего bitmap одним цветом (`DrawColor`), а не как заполнение связанной области.
- `ViewModels/MainViewModel.cs` содержит два объявления `partial class MainViewModel`; они являются частями одного типа.
