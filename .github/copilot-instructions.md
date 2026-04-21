# Copilot Instructions for SilvaData

## Overview
This is a .NET MAUI application for managing and evaluating poultry batches (health, management, nutrition, etc.). The application focuses on high performance on mobile devices, intensive use of dynamic forms, synchronization, and evidence capture (images).

## Technology Stack
- .NET MAUI (.NET 10)
- MVVM pattern (CommunityToolkit.Mvvm)
- Dependency Injection (Microsoft.Extensions.DependencyInjection)
- Syncfusion MAUI components (ListView, Buttons, DataGrid, etc.)
- SQLite for local data storage
- LocalizationResourceManager.Maui for dynamic translations

## Build and Test Commands

### Prerequisites
Before building, ensure MAUI workloads are installed:
```bash
dotnet workload restore
# or manually install:
dotnet workload install maui-android maui-ios
```

### Build the project
```bash
dotnet build SilvaData.csproj
```

### Restore dependencies
```bash
dotnet restore SilvaData.csproj
```

### Build for specific platform
```bash
# For Android
dotnet build SilvaData.csproj -f net10.0-android

# For iOS
dotnet build SilvaData.csproj -f net10.0-ios
```

### Clean the project
```bash
dotnet clean SilvaData.csproj
```

## Project Structure
- **Pages/**: XAML views with minimal code-behind
- **ViewModels/**: Business logic, state, and commands (MVVM pattern)
- **Models/**: Entities and DTOs
- **Services/**: Data access, synchronization, upload, charts
- **Utils/**: Helper classes (navigation, formatting, messages, scroll)
- **Resources/**: Images, fonts, localization files
- **Data/**: Database related files
- **PageModels/**: Additional view models for specific page interactions

## Architecture Patterns

### MVVM Pattern
- All views use `x:DataType` binding for compile-time validation
- ViewModels expose commands (e.g., `SalvarCommand`, `EditarCommand`)
- Set `AutomationId` on interactive elements for testing and accessibility
- ViewModels inherit from `ObservableObject` or use `[ObservableProperty]` attributes

### Dependency Injection
- All registrations are done in `MauiProgram.cs`
- Views and ViewModels are registered as `Transient` or `Singleton` as appropriate
- Views constructed in XAML resolve ViewModels via `ServiceHelper.GetRequiredService<T>()` in code-behind
- Always inject services through constructors, never create instances directly

### Messaging Pattern (WeakReferenceMessenger)
Common messages used in the application:
- `UpdateScoreMessage`: Recalculates total score
- `VaiProProximoMessage`: Navigates to next field
- `ISIMacroFotoRequestedMessage`: Opens photo capture modal
- `RefreshLoteFormularioMessage`: Reloads form
- `SetFormularioEstadoMessage`: Adjusts initial state/edit mode/parameters
- `SetModeloISIMacroMessage`: Sets selected model
- `CloseFormularioMessage`: Signals form closure
- `SelecionouAvaliacaoQualitativaMessage`: Updates qualitative evaluation choice

**Important**: Always register messages in `RegisterMessages()` and unregister in `UnregisterMessages()` to avoid memory leaks.

### Navigation
- Use `NavigationUtils.ShowViewAsModalAsync<T>()` for modals
- Check `NavigationUtils.TemModalAberta` to avoid unnecessary reloads
- Clean navigation history after modal operations

## Coding Conventions

### Naming Conventions
- ViewModels: `<Name>ViewModel` (e.g., `LoteFormularioViewModel`)
- Edit views: `<Name>_Edit` to distinguish versions
- Messages: `<Action>Message` with minimal payload
- Async methods: Use `Async` suffix
- Logging: Use prefix in text (e.g., `[LoteFormularioView]`) for easy grepping

### Code Style
- Use Portuguese for business domain terms and UI labels (this is a Brazilian application)
- Use English for technical/framework terms
- Minimal comments unless explaining complex logic
- Match the style of existing comments in the file

### Performance Best Practices
1. **Lazy Loading**: Load data only when navigating to a page (`OnNavigatedTo`)
2. **Parallel Operations**: Use `Task.WhenAll()` for independent async operations
3. **Main Thread**: Wrap UI updates in `MainThread.InvokeOnMainThreadAsync`
4. **Debouncing**: Use `CancellationTokenSource` with 300ms delay for search filters
5. **List Virtualization**: Use `SfListView` with `CachingStrategy="RecycleTemplate"`
6. **Image Optimization**: Verify size and type before upload
7. **Background Loading**: Load images and heavy data in background threads

### UI State Management
Common flags in ViewModels:
- `IsBusy`: Shows shimmer/loading indicator
- `IsFormValid`: Form validation state
- `ReadOnly`: Edit permission state
- `AreImagesLoaded`: Image loading completion state

### Error Handling
- Use try/catch blocks with `Debug.WriteLine` for development telemetry
- Show user feedback via `PopUpOK.ShowAsync()`
- Log unhandled exceptions with stack trace

### Localization
- All visible strings must be translatable
- Add new keys to `Resources/Localization/Localization.resx`
- Use in XAML: `{localization:Translate KeyName}`
- Restore culture: `RestoreLatestCulture(true)`

## Adding New Features

### Adding a New View
1. Create `MyNewView.xaml` with code-behind
2. Register View and ViewModel in `MauiProgram.cs`:
   ```csharp
   builder.Services.AddTransient<MyNewView>();
   builder.Services.AddTransient<MyNewViewModel>();
   ```
3. Inject ViewModel in code-behind:
   ```csharp
   BindingContext = ServiceHelper.GetRequiredService<MyNewViewModel>();
   ```
4. Use messages for inter-component communication
5. Set `IsBusy` for long operations
6. Set `AutomationId` on interactive elements

### Adding New Packages
- Only add new libraries when absolutely necessary
- Prefer using existing libraries in the project
- Check compatibility with .NET 10 and MAUI
- Update all target frameworks if needed

## Common Patterns

### Dynamic Forms
- Parameters loaded from selected model
- Alternatives populated in specific ViewModel (`AvaliacaoAlternativasViewModel`)
- Support for runtime addition/removal of parameters

### Image Handling
- Up to 3 images per form
- Loaded asynchronously and displayed in ViewModel
- Type and size verification before upload

### Validation
- Required fields checked before saving (`CheckCamposObrigatorios`)
- Cross-field validation for complex forms (e.g., start/end dates)

### ListView Items with Commands (IMPORTANT PATTERN ??)

When creating list items that need to execute commands on tap:

**? WRONG - Don't bind to parent ViewModel from item context:**
```csharp
// ItemsSource with wrapper ViewModel
ItemsSource="{Binding MyItemsList}"

// DataTemplate with wrapper type
<DataTemplate x:DataType="viewModels:MyItemButton">
    <Border>
        <!-- ? This won't work reliably -->
        <Border.GestureRecognizers>
            <TapGestureRecognizer 
                Command="{Binding Source={RelativeSource AncestorType={x:Type viewModels:ParentViewModel}}, Path=ShowItemCommand}"
                CommandParameter="{Binding .}" />
        </Border.GestureRecognizers>
    </Border>
</DataTemplate>
```

**? CORRECT - Add command property to item ViewModel:**
```csharp
// 1. Item ViewModel with command property
public partial class MyItemButton : ObservableObject
{
    [ObservableProperty] private string name;
    [ObservableProperty] private int value;
    
    // ? Add command property
    public IRelayCommand? ShowItemCommand { get; set; }
}

// 2. Initialize command when creating items
public async Task LoadData()
{
    var tempList = new List<MyItemButton>();
    
    foreach (var item in sourceData)
    {
        tempList.Add(new MyItemButton
        {
            Name = item.Name,
            Value = item.Value,
            // ? Initialize command with parent method
            ShowItemCommand = new AsyncRelayCommand<MyItemButton>(ShowItem)
        });
    }
    
    MyItemsList = new ObservableCollection<MyItemButton>(tempList);
}

// 3. Parent ViewModel method
[RelayCommand(AllowConcurrentExecutions = false)]
public async Task ShowItem(MyItemButton? item)
{
    if (item == null) return;
    // Handle item action
}
```

**? XAML - Simple direct binding:**
```xaml
<DataTemplate x:DataType="viewModels:MyItemButton">
    <Border>
        <Border.Behaviors>
            <toolkit:TouchBehavior
                DefaultOpacity="1"
                DefaultScale="1"
                PressedAnimationDuration="100"
                PressedOpacity="0.7"
                PressedScale="0.95" />
        </Border.Behaviors>
        
        <Border.GestureRecognizers>
            <!-- ? Direct binding to item's command -->
            <TapGestureRecognizer 
                Command="{Binding ShowItemCommand}"
                CommandParameter="{Binding .}" />
        </Border.GestureRecognizers>
        
        <!-- Item content -->
    </Border>
</DataTemplate>
```

**Why this pattern:**
- ? Each item encapsulates its own behavior
- ? No need for complex RelativeSource bindings
- ? Better performance (no visual tree traversal)
- ? More maintainable and testable
- ? Follows MVVM principles correctly
- ? Consistent with project architecture

**TouchBehavior for visual feedback:**
Always use `CommunityToolkit.Maui.Behaviors.TouchBehavior` for tap animations:
- `DefaultScale="1"` and `PressedScale="0.95"`
- `DefaultOpacity="1"` and `PressedOpacity="0.7"`
- `PressedAnimationDuration="100"` for smooth feedback

## Testing Strategy
- Set `AutomationId` on all interactive elements
- Use descriptive IDs for UI testing
- Test critical paths: form submission, image upload, synchronization

## Known Issues and Warnings
- Suppress warnings: `XC0103`, `XA4211` (see project file)
- Enable preview features for .NET 10

## Important Notes
1. This is a mobile-first application - always consider mobile performance
2. Portuguese is the primary language for business terms and UI
3. Forms are dynamic and data-driven from models
4. Offline-first with synchronization support
5. Image capture and management is a critical feature
6. The app uses Syncfusion components extensively - prefer Syncfusion over standard MAUI controls where available

## Do Not
- Do not create instances of services directly - always use DI
- Do not block the UI thread with heavy operations
- Do not forget to unregister message handlers
- Do not add comments unless they add real value
- Do not remove or modify working code without a specific reason
- Do not change localization keys without updating all translation files
- Do not use RelativeSource bindings for list item commands - use the command property pattern instead
